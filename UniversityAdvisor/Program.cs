using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Data;
using UniversityAdvisor.Models;
using UniversityAdvisor.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30); // 30 second timeout
    }));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUniversityService, UniversityService>();
builder.Services.AddScoped<IUniversityApiService, UniversityApiService>();
builder.Services.AddScoped<IAIChatService, AIChatService>();
builder.Services.AddHttpClient();

builder.Services.AddControllersWithViews();

var app = builder.Build();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Note: Run migrations with: dotnet ef migrations add InitialPostgresMigration && dotnet ef database update
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Test database connection
        if (await db.Database.CanConnectAsync())
        {
            // Migrations should be run manually, not via EnsureCreated
            // db.Database.Migrate(); // Uncomment if you want automatic migrations in production

            // Seed Admin role
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var universityService = scope.ServiceProvider.GetRequiredService<IUniversityService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var countriesToSeed = new[] { "Bulgaria", "Germany", "France", "Italy", "Spain", "Greece", "Romania" };
            
            try
            {
                var imported = await universityService.ImportIfEmptyAsync(countriesToSeed);
                if (imported > 0)
                {
                    logger.LogInformation($"Successfully imported {imported} universities from external API.");
                }
                else
                {
                    logger.LogInformation("Universities already exist in database, skipping import.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error importing universities from external API.");
            }
        }
        else
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Cannot connect to database. Please ensure PostgreSQL is running and migrations are applied.");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing database. Please check your PostgreSQL connection string and ensure the database server is running.");
    }
}

app.Run();
