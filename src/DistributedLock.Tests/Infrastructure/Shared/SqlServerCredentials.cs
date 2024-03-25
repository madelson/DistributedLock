namespace Medallion.Threading.Tests;

internal static class SqlServerCredentials
{
    public static readonly string ApplicationName = $"{typeof(SqlServerCredentials).Assembly.GetName().Name} ({TargetFramework.Current})";

    public static readonly string ConnectionString = new System.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = @"localhost", // localhost for SQL Developer, .\SQLEXPRESS for express
            InitialCatalog = "master",
            IntegratedSecurity = true,
            ApplicationName = ApplicationName,
            // set a high pool size so that we don't empty the pool through things like lock abandonment tests
            MaxPoolSize = 10000,
            TrustServerCertificate = true,
        }
        .ConnectionString;
}
