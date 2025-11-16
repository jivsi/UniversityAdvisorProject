using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniversityAdvisor.Domain.Entities;
using UniversityAdvisor.Infrastructure.Data;
using UniversityAdvisor.Application.Interfaces;
using UniversityAdvisor.Infrastructure.Data.Repositories;
using UniversityAdvisor.Application.Mappings;
using UniversityAdvisor.Application.UseCases.Universities;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/university-advisor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(AutoMapperProfiles));

// Repository Registration (Clean Architecture)
builder.Services.AddScoped<IUniversityRepository, UniversityRepository>();
builder.Services.AddScoped<IRatingRepository, RatingRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();

// Use Cases Registration
builder.Services.AddScoped<SearchUniversitiesUseCase>();

// Legacy Services (to be migrated gradually)
builder.Services.AddScoped<UniversityAdvisor.Services.IUniversityService, UniversityAdvisor.Services.UniversityService>();
builder.Services.AddScoped<UniversityAdvisor.Services.IUniversityApiService, UniversityAdvisor.Services.UniversityApiService>();
builder.Services.AddScoped<UniversityAdvisor.Services.IAIChatService, UniversityAdvisor.Services.AIChatService>();

// HTTP Client Factory
builder.Services.AddHttpClient();

// MVC Configuration
builder.Services.AddControllersWithViews(options =>
{
    // Add global filters here if needed
});

// Data Protection
builder.Services.AddDataProtection();

// Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        // Apply pending migrations
        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying database migrations.");
            throw; // Fail fast if migrations fail
        }

        // Seed roles
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        var roles = new[] { "Admin", "User", "Moderator" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
                logger.LogInformation($"Role '{roleName}' created.");
            }
        }

        // Seed initial data (legacy service for now)
        var universityService = scope.ServiceProvider.GetRequiredService<UniversityAdvisor.Services.IUniversityService>();
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
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing database.");
    }
}

try
{
    Log.Information("Starting University Advisor application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
