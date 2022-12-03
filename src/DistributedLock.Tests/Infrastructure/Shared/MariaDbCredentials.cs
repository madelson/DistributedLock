using MySqlConnector;
using System;
using System.IO;

namespace Medallion.Threading.Tests;

internal static class MariaDbCredentials
{
    // MARIADB SETUP NOTE
    //
    // In order to enable application name tracking, we must enable the performance schema. Add the following to 
    // C:\Program Files\MariaDB 10.6\data\my.ini in the [mysqld] section
    //
    // ;from https://mariadb.com/kb/en/performance-schema-overview/#activating-the-performance-schema
    // performance_schema=ON

    private static (string username, string password) GetCredentials(string baseDirectory)
    {
        var file = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "credentials", "mariadb.txt"));
        if (!File.Exists(file)) { throw new InvalidOperationException($"Unable to find MariaDB credentials file {file}"); }
        var lines = File.ReadAllLines(file);
        if (lines.Length != 2) { throw new FormatException($"{file} must contain exactly 2 lines of text"); }
        return (lines[0], lines[1]);
    }

    public static string GetConnectionString(string baseDirectory)
    {
        var (username, password) = GetCredentials(baseDirectory);

        return new MySqlConnectionStringBuilder
        {
            Port = 3307,
            Server = "localhost",
            Database = "mysql",
            UserID = username,
            Password = password,
            PersistSecurityInfo = true,
            // set a high pool size so that we don't empty the pool through things like lock abandonment tests
            MaximumPoolSize = 500,
        }.ConnectionString;
    }
}
