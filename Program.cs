using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Services;
using SIRH.EY.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using SIRH.EY.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Dataverse (conditional)
var dataverseConfig = builder.Configuration.GetSection("Dataverse");
var environmentUrl = dataverseConfig["EnvironmentUrl"];
var username = dataverseConfig["Username"];
var password = dataverseConfig["Password"];

if (!string.IsNullOrEmpty(environmentUrl) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
{
    builder.Services.AddScoped<IDataverseService>(provider =>
        new DataverseService(environmentUrl, username, password));
}

// Global auth policy — every request must be authenticated
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie paths
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    // Every request must be authenticated (applied globally via filter below)
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(Policies.ITAdminOnly,
        p => p.RequireRole(Roles.ITAdmin));

    options.AddPolicy(Policies.HrPrivileged,
        p => p.RequireRole(Roles.ITAdmin, Roles.RH));

    options.AddPolicy(Policies.ManagerOrAbove,
        p => p.RequireRole(Roles.ITAdmin, Roles.RH, Roles.Manager));
});

// Application services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IParametreService, ParametreService>();
builder.Services.AddScoped<IReferentielRhService, ReferentielRhService>();
builder.Services.AddScoped<IPlanDeveloppementService, PlanDeveloppementService>();
builder.Services.AddScoped<IPromotionReadinessService, PromotionReadinessService>();
builder.Services.AddScoped<IWorkforceImpactService, WorkforceImpactService>();
builder.Services.AddScoped<IUserContextService, UserContextService>();
// Authorization services
builder.Services.AddScoped<ITeamAccessService, TeamAccessService>();
builder.Services.AddScoped<IOwnershipService, OwnershipService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddHttpClient<FlowiseService>();
builder.Services.AddHttpClient();

var app = builder.Build();

// ── SEED ─────────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var userManager  = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager  = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var context      = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1. Roles
    string[] roles = { "ITAdmin", "RH", "Manager", "Collaborateur" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    // 2. ITAdmin user
    var adminEmail = "admin@ey.tn";
    var adminUser  = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            Nom = "Admin",
            Prenom = "IT"
        };
        await userManager.CreateAsync(adminUser, "Admin@123456");
        await userManager.AddToRoleAsync(adminUser, "ITAdmin");
    }

    // 3. RH system user
    var rhEmail = "rh@ey.tn";
    var rhUser  = await userManager.FindByEmailAsync(rhEmail);
    if (rhUser == null)
    {
        rhUser = new ApplicationUser
        {
            UserName = rhEmail,
            Email = rhEmail,
            EmailConfirmed = true,
            Nom = "RH",
            Prenom = "Système"
        };
        await userManager.CreateAsync(rhUser, "Rh@123456");
        await userManager.AddToRoleAsync(rhUser, "RH");
    }

    // 4. HR Master Data seed (idempotent)
    await SeedHrMasterData(context);

    // 5. Collaborateurs seed
    var collabData = new List<(string email, string nom, string prenom, string departement, string poste, string grade, int? managerId)>
    {
        ("hanine.hammami@ey.com",     "Hammami",    "Hanine",    "RH",         "HR Director",     "Manager", null),
        ("smiai.nour@ey.com",         "Nour",       "Smiäi",     "Tax",        "Data Analyst",    "Senior",  null),
        ("mariem.safri@ey.com",       "Safri",      "Mariem",    "Audit",      "Senior Auditor",  "Senior",  null),
        ("raed.amri@ey.com",          "Amri",       "Raed",      "Consulting", "Consultant",      "Junior",  null),
        ("ayoub.gombra@ey.com",       "Gombra",     "Ayoub",     "Tax",        "Consultant",      "Junior",  null),
        ("Ahmed.benyoussef@ey.com",   "Ben Youssef","Ahmed",     "Audit",      "Audit Manager",   "Manager", null),
        ("sofien.klaou@ey.com",       "Klaou",      "Sofien",    "Advisory",   "Senior Consultant","Senior", null),
        ("ibtissem.bessrour@ey.com",  "Besrour",    "ibtissem",  "Risk",       "Risk Manager",    "Manager", null),
    };

    foreach (var data in collabData)
    {
        var user = await userManager.FindByEmailAsync(data.email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = data.email,
                Email = data.email,
                EmailConfirmed = true,
                Nom = data.nom,
                Prenom = data.prenom
            };
            await userManager.CreateAsync(user, "Temp@123456");
            var identityRole = data.grade == "Manager" ? "Manager" : "Collaborateur";
            await userManager.AddToRoleAsync(user, identityRole);
        }

        if (!await context.Collaborateurs.AnyAsync(c => c.Email == data.email))
        {
            context.Collaborateurs.Add(new Collaborateur
            {
                Nom          = data.nom,
                Prenom       = data.prenom,
                Email        = data.email,
                Departement  = data.departement,
                Poste        = data.poste,
                Grade        = data.grade,
                DateEmbauche = DateTime.Today.AddYears(-3),
                Actif        = true,
                UserId       = user.Id
            });
        }
    }
    await context.SaveChangesAsync();

    // 6. Assign Audit manager relationships
    var auditManager = await context.Collaborateurs.FirstOrDefaultAsync(c => c.Poste == "Audit Manager");
    if (auditManager != null)
    {
        var equipeAudit = await context.Collaborateurs
            .Where(c => c.Departement == "Audit" && c.Poste != "Audit Manager")
            .ToListAsync();
        foreach (var c in equipeAudit)
            c.ManagerId = auditManager.Id;
        await context.SaveChangesAsync();
    }

    // 7. Demo data (competences, formations, etc.)
    await DemoDataSeeder.SeedAsync(context);
}

// ── MIDDLEWARE ────────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// Area route must be registered before the default route
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=AdminHome}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ── HR MASTER DATA SEED ───────────────────────────────────────────────────────
static async Task SeedHrMasterData(ApplicationDbContext context)
{
    // Only seed once
    if (await context.Departments.AnyAsync()) return;

    // Departments
    var departments = new[]
    {
        new Department { Name = "Assurance",              Code = "ASS",  IsActive = true },
        new Department { Name = "Consulting",             Code = "CON",  IsActive = true },
        new Department { Name = "Strategy & Transactions",Code = "SaT",  IsActive = true },
        new Department { Name = "Tax",                    Code = "TAX",  IsActive = true },
        new Department { Name = "Talent Team",            Code = "TT",   IsActive = true },
        new Department { Name = "Service IT",             Code = "IT",   IsActive = true },
        new Department { Name = "Finances et Contrôle",  Code = "FIN",  IsActive = true },
        new Department { Name = "Facilities",             Code = "FAC",  IsActive = true },
        new Department { Name = "MBD",                   Code = "MBD",  IsActive = true },
        new Department { Name = "Risk Management",        Code = "RISK", IsActive = true },
    };
    context.Departments.AddRange(departments);
    await context.SaveChangesAsync();

    // Sub-departments (Assurance example — extend as needed)
    var deptAss = departments.First(d => d.Code == "ASS");
    var deptCon = departments.First(d => d.Code == "CON");
    var deptTax = departments.First(d => d.Code == "TAX");
    var deptTT  = departments.First(d => d.Code == "TT");

    var subDepts = new[]
    {
        new SubDepartment { Name = "Audit Financier",      DepartmentId = deptAss.Id, IsActive = true },
        new SubDepartment { Name = "Risk Advisory",        DepartmentId = deptAss.Id, IsActive = true },
        new SubDepartment { Name = "Business Consulting",  DepartmentId = deptCon.Id, IsActive = true },
        new SubDepartment { Name = "Digital & Technology", DepartmentId = deptCon.Id, IsActive = true },
        new SubDepartment { Name = "Global Compliance",    DepartmentId = deptTax.Id, IsActive = true },
        new SubDepartment { Name = "People Advisory",      DepartmentId = deptTax.Id, IsActive = true },
        new SubDepartment { Name = "Recrutement",          DepartmentId = deptTT.Id,  IsActive = true },
        new SubDepartment { Name = "Formation & Dev",      DepartmentId = deptTT.Id,  IsActive = true },
    };
    context.SubDepartments.AddRange(subDepts);
    await context.SaveChangesAsync();

    // Positions
    var positions = new[]
    {
        new Position { Name = "Consultant",              IsActive = true },
        new Position { Name = "Senior Consultant",       IsActive = true },
        new Position { Name = "Manager",                 IsActive = true },
        new Position { Name = "Senior Manager",          IsActive = true },
        new Position { Name = "Director",                IsActive = true },
        new Position { Name = "Partner",                 IsActive = true },
        new Position { Name = "Analyste",                IsActive = true },
        new Position { Name = "Auditeur",                IsActive = true },
        new Position { Name = "Auditeur Senior",         IsActive = true },
        new Position { Name = "Audit Manager",           IsActive = true },
        new Position { Name = "Data Analyst",            IsActive = true },
        new Position { Name = "HR Director",             IsActive = true },
        new Position { Name = "Risk Manager",            IsActive = true },
        new Position { Name = "Tax Manager",             IsActive = true },
    };
    context.Positions.AddRange(positions);
    await context.SaveChangesAsync();

    // Grades
    var grades = new[]
    {
        new GradeEntity { Name = "Junior",         Level = 1, IsActive = true },
        new GradeEntity { Name = "Senior",         Level = 2, IsActive = true },
        new GradeEntity { Name = "Manager",        Level = 3, IsActive = true },
        new GradeEntity { Name = "Senior Manager", Level = 4, IsActive = true },
        new GradeEntity { Name = "Director",       Level = 5, IsActive = true },
        new GradeEntity { Name = "Partner",        Level = 6, IsActive = true },
    };
    context.Grades.AddRange(grades);
    await context.SaveChangesAsync();

    // Business Units
    var businessUnits = new[]
    {
        new BusinessUnitEntity { Name = "Assurance",              Code = "ASS", IsActive = true },
        new BusinessUnitEntity { Name = "Consulting",             Code = "CON", IsActive = true },
        new BusinessUnitEntity { Name = "Strategy & Transactions",Code = "SaT", IsActive = true },
        new BusinessUnitEntity { Name = "Tax",                    Code = "TAX", IsActive = true },
        new BusinessUnitEntity { Name = "CBS",                    Code = "CBS", IsActive = true },
    };
    context.BusinessUnits.AddRange(businessUnits);
    await context.SaveChangesAsync();

    // Locations
    var locations = new[]
    {
        new LocationEntity { Name = "Tunis — Lac 1",  City = "Tunis",  Country = "Tunisie",  IsActive = true },
        new LocationEntity { Name = "Tunis — Lac 2",  City = "Tunis",  Country = "Tunisie",  IsActive = true },
        new LocationEntity { Name = "Sfax",           City = "Sfax",   Country = "Tunisie",  IsActive = true },
        new LocationEntity { Name = "Remote",         City = null,     Country = null,        IsActive = true },
        new LocationEntity { Name = "Hybride",        City = null,     Country = null,        IsActive = true },
    };
    context.Locations.AddRange(locations);
    await context.SaveChangesAsync();

    // System Parameters
    var parameters = new[]
    {
        new SystemParameter { Key = "HRIS.Version",          Value = "1.0.0",  Category = "Système",    Description = "Version de l'application",       IsEditable = false },
        new SystemParameter { Key = "HR.MaxFormationsParAn",  Value = "5",      Category = "Formation",   Description = "Nombre maximum de formations par an par collaborateur" },
        new SystemParameter { Key = "HR.SeuilCompetenceCible",Value = "3",      Category = "Compétences", Description = "Niveau cible minimum pour une compétence validée" },
        new SystemParameter { Key = "HR.AncienneteMin",       Value = "2",      Category = "RH",          Description = "Ancienneté minimum (années) pour évaluation succession" },
        new SystemParameter { Key = "SMTP.Host",             Value = "",        Category = "Email",       Description = "Serveur SMTP sortant" },
        new SystemParameter { Key = "SMTP.Port",             Value = "587",     Category = "Email",       Description = "Port SMTP" },
    };
    context.SystemParameters.AddRange(parameters);
    await context.SaveChangesAsync();
}
