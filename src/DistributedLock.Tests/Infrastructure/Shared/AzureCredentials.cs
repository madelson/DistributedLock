namespace Medallion.Threading.Tests;

public static class AzureCredentials
{
    // based on https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#connect-to-the-emulator-account-using-a-shortcut
    public const string ConnectionString = "UseDevelopmentStorage=true";

    public static string DefaultBlobContainerName { get; } = "distributed-lock-" + TargetFramework.Current.Replace('.', '-');

    // Unlike container names, table names must be alphanumeric and begin with a letter.
    // https://learn.microsoft.com/en-us/rest/api/storageservices/understanding-the-table-service-data-model#table-names
    public static string DefaultTableName { get; } = "distributedlock" + TargetFramework.Current.Replace(".", string.Empty);
}
