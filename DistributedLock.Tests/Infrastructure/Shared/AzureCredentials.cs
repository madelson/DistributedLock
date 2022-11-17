namespace Medallion.Threading.Tests;

public static class AzureCredentials
{
    // based on https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator#connect-to-the-emulator-account-using-a-shortcut
    public const string ConnectionString = "UseDevelopmentStorage=true";

    public static string DefaultBlobContainerName { get; } = "distributed-lock-" + TargetFramework.Current.Replace('.', '-');
}
