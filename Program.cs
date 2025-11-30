using Microsoft.AspNetCore.Authentication.Cookies;
using UniversityFinder.Repositories;
using UniversityFinder.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ TLS Configuration
System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

// Add services to the container.
// ✅ SUPABASE-ONLY ARCHITECTURE: All data (including authentication) uses Supabase
// No SQLite, no EF Core Identity - everything goes through Supabase REST API and Auth

builder.Services.AddControllersWithViews();

// ✅ SUPABASE AUTHENTICATION: Configure cookie authentication for Supabase sessions
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// ✅ AUTHORIZATION: Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ✅ SUPABASE REST API: Register Supabase REST service
builder.Services.AddHttpClient<SupabaseService>();

// ✅ SUPABASE AUTHENTICATION: Register Supabase Auth service
builder.Services.AddSingleton<SupabaseAuthService>();

// LEGACY: RvuImportService removed - RVU import functionality deprecated
// builder.Services.AddHttpClient<RvuImportService>();

// Register HttpClient for API services
builder.Services.AddHttpClient<OpenAiService>();

// LEGACY: TeleportApiService moved to Services/Legacy - no longer used
// Teleport API is deprecated in favor of official Bulgarian sources (RVU + NSI)
// builder.Services.AddHttpClient<TeleportApiService>();
// builder.Services.AddScoped<ITeleportApiService, TeleportApiService>();

// LEGACY: Repositories use EF Core which has been removed
// TODO: Update repositories to use SupabaseService or remove them
// Register Repositories
builder.Services.AddScoped<IUniversityRepository, UniversityRepository>();
builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ICostOfLivingRepository, CostOfLivingRepository>();

// Register Services
builder.Services.AddScoped<OpenAiService>();
// LEGACY: DataSeeder removed - uses ApplicationDbContext which is no longer available
// builder.Services.AddScoped<DataSeeder>();

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ SUPABASE AUTHENTICATION: Use authentication middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ✅ SUPABASE-ONLY: No database initialization needed - all data is in Supabase
// Application data (Universities, Programs, etc.) is stored in Supabase via REST API
// Authentication is handled by Supabase Auth
// No EF Core migrations or SQLite database needed

app.Run();
