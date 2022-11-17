namespace Medallion.Threading.Tests;

internal static class SqlServerCredentials
{
    public static readonly string ApplicationName = $"{typeof(SqlServerCredentials).Assembly.GetName().Name} ({TargetFramework.Current})";

    public static readonly string ConnectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = @".\SQLEXPRESS",
            InitialCatalog = "master",
            IntegratedSecurity = true,
            ApplicationName = ApplicationName,
            // set a high pool size so that we don't empty the pool through things like lock abandonment tests
            MaxPoolSize = 10000,
        }
        .ConnectionString;
}
