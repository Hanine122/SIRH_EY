using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using SIRH.EY.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Dataverse (inchangé)
var dataverseConfig = builder.Configuration.GetSection("Dataverse");
var environmentUrl = dataverseConfig["EnvironmentUrl"];
var username = dataverseConfig["Username"];
var password = dataverseConfig["Password"];

if (!string.IsNullOrEmpty(environmentUrl) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
{
    builder.Services.AddScoped<IDataverseService>(provider =>
        new DataverseService(environmentUrl, username, password));
}

// Auth globale
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

// Login page
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IParametreService, ParametreService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

// ==================== SEED UNIQUE (tout regrouper ici) ====================
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // 1. Création des rôles
    string[] roles = { "RH", "Manager", "Collaborateur" };
    foreach (var role in roles)
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));

    // 2. Création de l'utilisateur RH
    var rhEmail = "rh@ey.tn";
    var rhUser = await userManager.FindByEmailAsync(rhEmail);
    if (rhUser == null)
    {
        rhUser = new ApplicationUser { UserName = rhEmail, Email = rhEmail, EmailConfirmed = true, Nom = "RH", Prenom = "Système" };
        await userManager.CreateAsync(rhUser, "Rh@123456");
        await userManager.AddToRoleAsync(rhUser, "RH");
    }

    // 3. Données des collaborateurs
    var collabData = new List<(string email, string nom, string prenom, string departement, string poste, string grade, int? managerId)>
    {
        ("hanine.hammami@ey.com", "Hammami", "Hanine", "RH", "HR Director", "Manager", null),
        ("smiai.nour@ey.com", "Nour", "Smiäi", "Tax", "Data Analyst", "Senior", null),
        ("mariem.safri@ey.com", "Safri", "Mariem", "Audit", "Senior Auditor", "Senior", null),
        ("raed.amri@ey.com", "Amri", "Raed", "Consulting", "Consultant", "Junior", null),
        ("ayoub.gombra@ey.com", "Gombra", "Ayoub", "Tax", "Consultant", "Junior", null),
        ("Ahmed.benyoussef@ey.com", "Ben Youssef", "Ahmed", "Audit", "Audit Manager", "Manager", null),
        ("sofien.klaou@ey.com", "Klaou", "Sofien", "Advisory", "Senior Consultant", "Senior", null),
        ("ibtissem.bessrour@ey.com", "Besrour", "ibtissem", "Risk", "Risk Manager", "Manager", null),
    };

    // 4. Création des comptes Identity + Collaborateurs
    foreach (var data in collabData)
    {
        var user = await userManager.FindByEmailAsync(data.email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = data.email, Email = data.email, EmailConfirmed = true, Nom = data.nom, Prenom = data.prenom };
            await userManager.CreateAsync(user, "Temp@123456");
            var role = data.grade == "Manager" ? "Manager" : "Collaborateur";
            await userManager.AddToRoleAsync(user, role);
        }

        var collabExist = await context.Collaborateurs.FirstOrDefaultAsync(c => c.Email == data.email);
        if (collabExist == null)
        {
            var collab = new Collaborateur
            {
                Nom = data.nom,
                Prenom = data.prenom,
                Email = data.email,
                Departement = data.departement,
                Poste = data.poste,
                Grade = data.grade,
                ManagerId = data.managerId,
                DateEmbauche = DateTime.Today.AddYears(-3),
                Actif = true,
                UserId = user.Id
            };
            context.Collaborateurs.Add(collab);
        }
    }
    await context.SaveChangesAsync();

    // 5. Mise à jour des ManagerId (Audit Manager)
    var auditManager = await context.Collaborateurs.FirstOrDefaultAsync(c => c.Poste == "Audit Manager");
    if (auditManager != null)
    {
        var equipeAudit = await context.Collaborateurs.Where(c => c.Departement == "Audit" && c.Poste != "Audit Manager").ToListAsync();
        foreach (var c in equipeAudit)
            c.ManagerId = auditManager.Id;
        await context.SaveChangesAsync();
    }

    // 6. Appel du DemoDataSeeder (compétences, formations, etc.)
    await DemoDataSeeder.SeedAsync(context);
}

// ==================== MIDDLEWARE ====================
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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();