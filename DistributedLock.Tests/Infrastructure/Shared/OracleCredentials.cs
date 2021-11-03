using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    internal static class OracleCredentials
    {
        private static string GetPassword(string baseDirectory)
        {
            var file = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "credentials", "oracle.txt"));
            if (!File.Exists(file)) { throw new InvalidOperationException($"Unable to find mysql credentials file {file}"); }
            var lines = File.ReadAllLines(file);
            if (lines.Length != 1) { throw new FormatException($"{file} must contain exactly 1 line of text"); }
            return lines[0];
        }

        public static string GetConnectionString(string baseDirectory)
        {
            var password = GetPassword(baseDirectory);

            return new OracleConnectionStringBuilder
            {
                DataSource = "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=XE)))",
                UserID = "SYSTEM",
                Password = password,
                PersistSecurityInfo = true,
                // set a high pool size so that we don't empty the pool through things like lock abandonment tests
                MaxPoolSize = 500,
            }.ConnectionString;
        }
    }
}
