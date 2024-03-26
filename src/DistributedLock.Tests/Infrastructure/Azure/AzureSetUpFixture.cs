using Azure.Storage.Blobs;
using Medallion.Shell;
using NUnit.Framework;
using System.Diagnostics;

namespace Medallion.Threading.Tests.Azure;

[SetUpFixture]
public class AzureSetUpFixture
{
    // https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio%2Cblob-storage
    private const string EmulatorProcessName = "azurite";

    private bool _startedEmulator;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var existingProcesses = Process.GetProcessesByName(EmulatorProcessName);
        if (existingProcesses.Any())
        {
            Console.WriteLine($"Emulator already running (PID={existingProcesses[0].Id})");
            foreach (var process in existingProcesses) { process.Dispose(); }
        }
        else
        {
            var emulatorExePaths = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio"), $"{EmulatorProcessName}.exe", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .ToArray();
            if (!emulatorExePaths.Any())
            {
                throw new FileNotFoundException($"Could not locate {EmulatorProcessName}. This is required to run Azure tests. See https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite");
            }

            // Note: we used to hang on to this command to kill it later; we no longer do that because this process seems to naturally exit
            // by the end of the test and instead the emulator is running in another process. Therefore, we do a name-based lookup in teardown instead.
            var command = Command.Run(
                    emulatorExePaths[0],
                    // After updating to Azure.Storage.Blobs 12.19.1 I started getting an error saying that I had to update Azurite or pass this flag.
                    // AFAIK the only way to update Azurite is to update VS, which did not fix the issue. Therefore, I am passing this flag instead.
                    ["start", "--skipApiVersionCheck"], 
                    o => o.StartInfo(i => i.RedirectStandardInput = false)
                        .WorkingDirectory(Path.GetDirectoryName(this.GetType().Assembly.Location)!))
                .RedirectTo(Console.Out)
                .RedirectStandardErrorTo(Console.Error);
            Console.WriteLine($"Launched {EmulatorProcessName}");
            this._startedEmulator = true;
        }

        new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName).CreateIfNotExists();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        new BlobContainerClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName).DeleteIfExists();

        if (this._startedEmulator)
        {
            foreach (var process in Process.GetProcessesByName(EmulatorProcessName))
            {
                process.Kill();
                process.WaitForExit();
            }
        }
    }
}
