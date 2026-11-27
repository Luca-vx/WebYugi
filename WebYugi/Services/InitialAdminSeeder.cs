using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebYugi.Infrastructure;
using WebYugi.Model;

namespace WebYugi.Services;

public sealed class InitialAdminSeeder(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment, ILogger<InitialAdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var name = (configuration["ADMIN_INITIAL_NAME"] ?? "admin").Trim();
        var email = (configuration["ADMIN_INITIAL_EMAIL"] ?? "admin@gmail.com").Trim().ToLowerInvariant();
        var password = configuration["ADMIN_INITIAL_PASSWORD"];
        var usesDefaultPassword = string.IsNullOrEmpty(password);
        if (usesDefaultPassword) password = "Ad!2345678";
        if (name.Length is < 3 or > 100 || email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            throw new InvalidOperationException("ADMIN_INITIAL_NAME ou ADMIN_INITIAL_EMAIL contém valor inválido.");
        if (!environment.IsDevelopment() && (usesDefaultPassword || !IsStrongPassword(password!)))
            throw new InvalidOperationException("Configure ADMIN_INITIAL_PASSWORD com uma senha forte antes de iniciar em produção.");

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational() && !await db.Database.CanConnectAsync(cancellationToken))
            throw new InvalidOperationException("Banco indisponível durante a criação do administrador inicial.");

        var normalizedEmail = email;
        var existing = await db.Users.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail || user.Email.ToLower() == email, cancellationToken);
        if (existing is not null)
        {
            if (!existing.IsAdmin)
                throw new InvalidOperationException($"Já existe uma conta comum com o e-mail {email}. Resolva manualmente antes de inicializar o administrador.");
            return;
        }
        var admin = new Users(name.Trim(), email, "")
        {
            NormalizedName = name.Trim().ToUpperInvariant(),
            NormalizedEmail = email,
            IsAdmin = true,
            SenhaHashed = true
        };
        admin.Senha = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Users>>().HashPassword(admin, password!);
        db.Users.Add(admin);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // Concurrent startup may have inserted the same account; never overwrite it.
            db.ChangeTracker.Clear();
            if (!await db.Users.AnyAsync(user => user.NormalizedEmail == email && user.IsAdmin, cancellationToken)) throw;
        }
        logger.LogInformation("Administrador inicial verificado/criado.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static bool IsStrongPassword(string value) => value.Length >= 12 && value.Length <= 128 &&
        value.Any(char.IsUpper) && value.Any(char.IsLower) && value.Any(char.IsDigit) && value.Any(c => !char.IsLetterOrDigit(c)) &&
        !Regex.IsMatch(value, "admin|password|senha|123456", RegexOptions.IgnoreCase);
}
