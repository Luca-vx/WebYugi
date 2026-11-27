using Npgsql;

namespace WebYugi.Infrastructure;

/// <summary>Resolves PostgreSQL connection settings from Railway/Neon or local configuration.</summary>
public static class DatabaseConnection
{
    public static string Resolve(IConfiguration configuration)
    {
        var value = configuration["DATABASE_URL"]
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                "Configure DATABASE_URL or ConnectionStrings:DefaultConnection with the PostgreSQL connection details.");

        // Railway integrations commonly expose PostgreSQL URLs (postgres://user:pass@host/db).
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == "postgres" || uri.Scheme == "postgresql"))
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = uri.IsDefaultPort ? 5432 : uri.Port,
                Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
                Username = Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[0]),
                Password = uri.UserInfo.Contains(':')
                    ? Uri.UnescapeDataString(uri.UserInfo.Split(':', 2)[1])
                    : string.Empty,
                SslMode = SslMode.Require
            };

            if (int.TryParse(query["connect_timeout"], out var timeout) && timeout > 0)
                builder.Timeout = timeout;
            if (bool.TryParse(query["pooling"], out var pooling))
                builder.Pooling = pooling;

            return builder.ConnectionString;
        }

        // Neon requires TLS. Preserve any parameters supplied in a standard Npgsql string.
        var connection = new NpgsqlConnectionStringBuilder(value);
        if (connection.SslMode == SslMode.Disable)
            connection.SslMode = SslMode.Require;
        return connection.ConnectionString;
    }
}
