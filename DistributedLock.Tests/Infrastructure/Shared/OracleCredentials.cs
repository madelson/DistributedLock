using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
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
            ConfigureWallet(credentialDirectory);
            var (datasource, username, password) = GetCredentials(credentialDirectory);

            return new OracleConnectionStringBuilder
            {
                DataSource = datasource,
                UserID = username,
                Password = password,
                PersistSecurityInfo = true,
                // The free-tier autonomous database only allows 20 connections maximum (presumably across all clients) so this limit
                // should help keep us below this limit. Running up against the limit throws errors on Connection.Open()
                MaxPoolSize = 15,
            }.ConnectionString;
        }

        private static void ConfigureWallet(string credentialDirectory)
        {
            var walletDirectory = Directory.GetDirectories(credentialDirectory, "Wallet_*").Single();
            if (OracleConfiguration.TnsAdmin != walletDirectory)
            {
                // directory containing tnsnames.ora and sqlnet.ora
                OracleConfiguration.TnsAdmin = walletDirectory;
            }
            if (OracleConfiguration.WalletLocation != walletDirectory)
            {
                // directory containing cwallet.sso
                OracleConfiguration.WalletLocation = walletDirectory;
            }
        }

        private static (string DataSource, string Username, string Password) GetCredentials(string credentialDirectory)
        {
            var file = Path.Combine(credentialDirectory, "oracle.txt");
            if (!File.Exists(file)) { throw new InvalidOperationException($"Unable to find Oracle credentials file {file}"); }
            var lines = File.ReadAllLines(file);
            if (lines.Length != 3) { throw new FormatException($"{file} must contain exactly 2 lines of text"); }
            return (lines[0], lines[1], lines[2]);
        }
    }
}
