using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using WebYugi.Infrastructure;
using WebYugi.Services;
using WebYugi.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var databaseConnectionString = DatabaseConnection.Resolve(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(databaseConnectionString)
);

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 6 * 1024 * 1024;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddScoped<ICardImageStorage, LocalCardImageStorage>();
builder.Services.AddSingleton<ICardCopyLimitPolicy, DefaultCardCopyLimitPolicy>();
builder.Services.AddScoped<IPasswordHasher<Users>, PasswordHasher<Users>>();
builder.Services.AddSingleton<IPasswordResetEmailSender, PasswordResetEmailSender>();
builder.Services.AddHostedService<LegacyPasswordMigrationService>();
builder.Services.AddHostedService<InitialAdminSeeder>();
builder.Services.AddScoped<IAuthorizationHandler, AdminOnlyHandler>();
builder.Services.AddAuthorization(options => options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().AddRequirements(new AdminOnlyRequirement())));
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 210_000);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/users/login";
        options.Cookie.Name = "WebYugiAuth";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnValidatePrincipal = async context =>
        {
            var versionClaim = context.Principal?.FindFirst("pwd_ver")?.Value;
            var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(versionClaim, out var ticketVersion) || !int.TryParse(userIdClaim, out var userId))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }
            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var currentVersion = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .SingleOrDefaultAsync(db.Users.Where(user => user.Id == userId).Select(user => (int?)user.PasswordVersion), context.HttpContext.RequestAborted);
            if (currentVersion is null || currentVersion.Value != ticketVersion)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
        // The React SPA consumes API status codes and handles navigation itself.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

// Development can run on the HTTP-only launch profile; production always redirects to HTTPS.
if (!app.Environment.IsDevelopment() || app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
    app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!app.Environment.IsDevelopment())
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    await next();
});

if (Directory.Exists(app.Environment.WebRootPath))
    app.UseStaticFiles();

var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(Path.Combine(uploadsRoot, "cartas"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

app.Run();
