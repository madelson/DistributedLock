using Npgsql;
using System;
using System.IO;

namespace Medallion.Threading.Tests;

internal static class PostgresCredentials
{
    private static (string username, string password) GetCredentials(string baseDirectory)
    {
        var file = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "credentials", "postgres.txt"));
        if (!File.Exists(file)) { throw new InvalidOperationException($"Unable to find postgres credentials file {file}"); }
        var lines = File.ReadAllLines(file);
        if (lines.Length != 2) { throw new FormatException($"{file} must contain exactly 2 lines of text"); }
        return (lines[0], lines[1]);
    }

    public static string GetConnectionString(string baseDirectory)
    {
        var (username, password) = GetCredentials(baseDirectory);

        return new NpgsqlConnectionStringBuilder
        {
            Port = 5432,
            Host = "localhost",
            Database = "postgres",
            Username = username,
            Password = password,
            PersistSecurityInfo = true,
            ApplicationName = SqlServerCredentials.ApplicationName,
            // set a high pool size so that we don't empty the pool through things like lock abandonment tests
            MaxPoolSize = 500,
        }.ConnectionString;
    }
}
