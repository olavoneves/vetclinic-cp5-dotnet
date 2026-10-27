namespace VetClinic.FunctionalTests.Infrastructure;

public static class TestSettings
{
    private const string DefaultPassword = "VetClinic@Strong#2026"; // mesmo valor do .env.example

    /// <summary>
    /// Connection string do SQL Server do container. A senha vem (nesta ordem) da env
    /// MSSQL_SA_PASSWORD, do arquivo .env na raiz do repositório ou do valor do .env.example.
    /// </summary>
    public static string BuildConnectionString()
    {
        var password = Environment.GetEnvironmentVariable("MSSQL_SA_PASSWORD")
                       ?? ReadPasswordFromDotEnv()
                       ?? DefaultPassword;

        return $"Server=localhost,14333;Database=VetClinicDb;User Id=sa;Password={password};TrustServerCertificate=True";
    }

    private static string? ReadPasswordFromDotEnv()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "VetClinic.sln")))
            dir = dir.Parent;

        var envFile = dir is null ? null : Path.Combine(dir.FullName, ".env");
        if (envFile is null || !File.Exists(envFile))
            return null;

        return File.ReadAllLines(envFile)
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("MSSQL_SA_PASSWORD=", StringComparison.Ordinal))
            .Select(l => l["MSSQL_SA_PASSWORD=".Length..].Trim().Trim('"'))
            .FirstOrDefault();
    }
}
