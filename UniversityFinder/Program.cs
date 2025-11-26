using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniversityFinder.Data;
using UniversityFinder.Repositories;
using UniversityFinder.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ TLS Configuration for Hipolabs API
System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

// Add services to the container.
// ✅ SUPABASE REST API: Application data (Universities, Programs, etc.) uses Supabase REST API (PostgREST)
// This eliminates direct PostgreSQL TCP connections and works on restricted networks (school WiFi)
// No more "No such host is known" errors - all data operations use HTTP REST API
// Identity uses local SQLite database for authentication (cookie-based login only)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=Identity.db")); // Local SQLite for Identity only - application data uses Supabase REST
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => 
{
    options.SignIn.RequireConfirmedAccount = false; // Disable email confirmation for easier development
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// ✅ SUPABASE REST API: Register Supabase REST service
builder.Services.AddHttpClient<SupabaseService>();

// Register HttpClient for API services
builder.Services.AddHttpClient<HeiApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddHttpClient<OpenAiService>();
builder.Services.AddHttpClient<HipolabsApiService>();
builder.Services.AddHttpClient<TeleportApiService>();

// Register Repositories
builder.Services.AddScoped<IUniversityRepository, UniversityRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICostOfLivingRepository, CostOfLivingRepository>();

// Register Services
builder.Services.AddScoped<HeiApiService>();
builder.Services.AddScoped<OpenAiService>();
builder.Services.AddScoped<DataSeeder>();
builder.Services.AddScoped<IHipolabsApiService, HipolabsApiService>();
builder.Services.AddScoped<ITeleportApiService, TeleportApiService>();

// Add Memory Cache for API response caching
builder.Services.AddMemoryCache();

// Register Application Services
builder.Services.AddScoped<IUniversitySearchService, UniversitySearchService>();
builder.Services.AddScoped<IUserFavoriteService, UserFavoriteService>();
builder.Services.AddScoped<IUserSearchHistoryService, UserSearchHistoryService>();
builder.Services.AddScoped<ISubjectNormalizationService, SubjectNormalizationService>();
builder.Services.AddScoped<ISubjectInferenceService, SubjectInferenceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ✅ SUPABASE REST API: Initialize Identity database only (application data uses Supabase REST)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Apply migrations for Identity database only (local SQLite)
        context.Database.Migrate();
        
        // Note: Application data (Universities, Programs, etc.) is stored in Supabase via REST API
        // No EF Core migrations needed for application data - tables must be created in Supabase dashboard
        
        // ✅ HARDENED: Check and reset any stale sync statuses on startup
        // This prevents sync from being stuck after application restart
        try
        {
            var heiApiService = services.GetRequiredService<HeiApiService>();
            await heiApiService.CheckAndResetStaleSyncStatusesAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Warning: Failed to check stale sync statuses on startup. This is non-critical.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing Identity database.");
    }
}

app.Run();
