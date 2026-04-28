using Microsoft.EntityFrameworkCore;
using SIRH.EY.Data;
using SIRH.EY.Services;
// using Rotativa.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Lire les paramètres Dataverse...
var dataverseConfig = builder.Configuration.GetSection("Dataverse");
var environmentUrl = dataverseConfig["EnvironmentUrl"];
var username = dataverseConfig["Username"];
var password = dataverseConfig["Password"];

if (!string.IsNullOrEmpty(environmentUrl) && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
{
    builder.Services.AddScoped<IDataverseService>(provider =>
        new DataverseService(environmentUrl, username, password));
}

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ===== AJOUTS DÉPLACÉS ICI (avant Build) =====
builder.Services.AddMemoryCache();
builder.Services.AddScoped<IParametreService, ParametreService>();

var app = builder.Build();
// RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

// ===== Le seeder doit être exécuté après Build, mais avant Run =====
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DemoDataSeeder.SeedAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
// app.UseRotativa();
app.UseAuthorization();

app.UseDefaultFiles();   // Cherche index.html par défaut
app.UseStaticFiles();    // Sert les fichiers CSS/JS/images depuis wwwroot

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapFallbackToFile("index.html");

app.Run();