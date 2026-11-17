using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebYugi.Infrastructure;
using WebYugi.Model;

namespace WebYugi.Services;

/// <summary>Converts legacy plaintext credentials in bounded batches after the schema migration is applied.</summary>
public sealed class LegacyPasswordMigrationService(
    IServiceScopeFactory scopeFactory,
    ILogger<LegacyPasswordMigrationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var migrated = await MigrateBatchAsync(stoppingToken);
                if (migrated == 0) return;
                await Task.Delay(TimeSpan.FromMilliseconds(20), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                // Do not include credentials or database values in the log.
                logger.LogError("Could not migrate legacy password records ({ErrorType}); will retry.", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task<int> MigrateBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Users>>();
        var legacyUsers = await db.Users.AsNoTracking().Where(user => !user.SenhaHashed)
            .OrderBy(user => user.Id).Take(50).ToListAsync(cancellationToken);
        var migrated = 0;
        foreach (var user in legacyUsers)
        {
            var oldPassword = user.Senha;
            var hash = passwordHasher.HashPassword(user, oldPassword);
            migrated += await db.Users.Where(candidate => candidate.Id == user.Id && !candidate.SenhaHashed && candidate.Senha == oldPassword)
                .ExecuteUpdateAsync(update => update.SetProperty(candidate => candidate.Senha, hash)
                    .SetProperty(candidate => candidate.SenhaHashed, true), cancellationToken);
        }
        if (migrated > 0) logger.LogInformation("Migrated {Count} legacy password record(s) to password hashes.", migrated);
        return migrated;
    }
}
