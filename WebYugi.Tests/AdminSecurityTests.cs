using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using WebYugi.Controllers;
using WebYugi.DTOs;
using WebYugi.Infrastructure;
using WebYugi.Model;
using WebYugi.Services;
using Xunit;

namespace WebYugi.Tests;

public sealed class AdminSecurityTests
{
    [Fact]
    public async Task AdminPolicy_RequiresCurrentDatabaseAdminStatus()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IAuthorizationHandler, AdminOnlyHandler>();
        services.AddAuthorizationCore(options => options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().AddRequirements(new AdminOnlyRequirement())));
        await using var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<AppDbContext>();
        db.Users.Add(new Users("Admin", "admin@example.com", "") { IsAdmin = true, NormalizedEmail = "admin@example.com" });
        await db.SaveChangesAsync();
        var principal = Principal(1);
        Assert.True((await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, "AdminOnly")).Succeeded);
        db.Users.Single().IsAdmin = false;
        await db.SaveChangesAsync();
        Assert.False((await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(principal, null, "AdminOnly")).Succeeded);
    }

    [Fact]
    public async Task AdminPolicy_AllowsAdminEvenWhenPasswordChangeFlagIsSet()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddScoped<IAuthorizationHandler, AdminOnlyHandler>();
        services.AddAuthorizationCore(options => options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().AddRequirements(new AdminOnlyRequirement())));
        await using var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<AppDbContext>();
        db.Users.AddRange(
            new Users("Normal", "normal@example.com", "") { NormalizedEmail = "normal@example.com" },
            new Users("Needs Password", "change@example.com", "") { IsAdmin = true, MustChangePassword = true, NormalizedEmail = "change@example.com" });
        await db.SaveChangesAsync();
        var auth = provider.GetRequiredService<IAuthorizationService>();
        Assert.False((await auth.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, "AdminOnly")).Succeeded);
        Assert.False((await auth.AuthorizeAsync(Principal(1), null, "AdminOnly")).Succeeded);
        Assert.True((await auth.AuthorizeAsync(Principal(2), null, "AdminOnly")).Succeeded);
    }

    [Fact]
    public async Task Seed_CreatesHashedAdminOnceAndDoesNotReplacePassword()
    {
        var (provider, seeder) = CreateSeeder("AdminSeed-" + Guid.NewGuid(), new Dictionary<string, string?>());
        await using (provider)
        {
            await seeder.StartAsync(default);
            var db = provider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync();
            Assert.True(user.IsAdmin);
            Assert.False(user.MustChangePassword);
            Assert.True(user.SenhaHashed);
            Assert.NotEqual("admin", user.Senha);
            var originalHash = user.Senha;
            await seeder.StartAsync(default);
            Assert.Equal(originalHash, (await db.Users.SingleAsync()).Senha);
            Assert.Single(await db.Users.ToListAsync());
        }
    }

    [Fact]
    public async Task Seed_RefusesToPromoteAnExistingOrdinaryAccount()
    {
        var (provider, seeder) = CreateSeeder("AdminConflict-" + Guid.NewGuid(), new Dictionary<string, string?>());
        await using (provider)
        {
            var db = provider.GetRequiredService<AppDbContext>();
            db.Users.Add(new Users("Existing", "admin@gmail.com", "existing") { NormalizedEmail = "admin@gmail.com" });
            await db.SaveChangesAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.StartAsync(default));
            Assert.False((await db.Users.SingleAsync()).IsAdmin);
        }
    }

    [Fact]
    public void PublicRegistrationAndAdminUpdateContractsDoNotAcceptAdminFlag()
    {
        Assert.Null(typeof(RegisterRequest).GetProperty("IsAdmin"));
        Assert.Null(typeof(AdminUsersController.UpdateUserRequest).GetProperty("IsAdmin"));
    }

    [Fact]
    public void CatalogMutationActionsRequireAdminPolicy()
    {
        foreach (var method in new[] { "Create", "Update", "Delete" })
        {
            var action = typeof(CardsController).GetMethod(method);
            Assert.NotNull(action);
            var authorization = action!.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
            Assert.Equal("AdminOnly", authorization.Policy);
        }
    }

    private static ClaimsPrincipal Principal(int id) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test"));

    private static (ServiceProvider Provider, InitialAdminSeeder Seeder) CreateSeeder(string databaseName, IDictionary<string, string?> settings)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IPasswordHasher<Users>, PasswordHasher<Users>>();
        var provider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var environment = new TestEnvironment { EnvironmentName = "Development", ApplicationName = "WebYugi.Tests", ContentRootPath = Path.GetTempPath(), ContentRootFileProvider = new NullFileProvider() };
        var seeder = new InitialAdminSeeder(provider, configuration, environment, NullLogger<InitialAdminSeeder>.Instance);
        return (provider, seeder);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "WebYugi.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
