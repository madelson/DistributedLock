using Oracle.ManagedDataAccess.Client;
using System;
using System.IO;
using System.Linq;

namespace Medallion.Threading.Tests;

/// <summary>
/// For Oracle, we need both a password and a "wallet" directory.
/// 
/// See https://www.oracle.com/topics/technologies/dotnet/tech-info-autonomousdatabase.html for setup instructions.
/// See also https://github.com/oracle/dotnet-db-samples/issues/225
/// 
/// If the tests haven't been run for some time, it might be necessary to start the autonomous database at https://cloud.oracle.com/,
/// since it will stop after being idle for some time.
/// </summary>
internal static class OracleCredentials
{
    public static string GetConnectionString(string baseDirectory)
    {
        var credentialDirectory = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "credentials"));
        var (username, password) = GetCredentials(credentialDirectory);

        return new OracleConnectionStringBuilder
        {
            DataSource = "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))",
            UserID = username,
            Password = password,
            PersistSecurityInfo = true,
        }.ConnectionString;
    }

    private static (string Username, string Password) GetCredentials(string credentialDirectory)
    {
        var file = Path.Combine(credentialDirectory, "oracle.txt");
        if (!File.Exists(file)) { throw new InvalidOperationException($"Unable to find Oracle credentials file {file}"); }
        var lines = File.ReadAllLines(file);
        if (lines.Length != 2) { throw new FormatException($"{file} must contain exactly 2 lines of text"); }
        return (lines[0], lines[1]);
    }
}
