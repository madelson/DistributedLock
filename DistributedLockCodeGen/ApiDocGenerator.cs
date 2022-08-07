using Medallion.Shell;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DistributedLockCodeGen
{
    internal class ApiDocGenerator
    {
        private const string DotNetPath = @"C:\Program Files\dotnet\dotnet.exe";
        private const string DocsFramework = "netstandard2.1";
        private const string DocsFolder = "docs";
        private const string ApiFolder = "api";

        private static readonly Shell Shell = new(
            options => options.ThrowOnError()
                .Command(c => c.RedirectStandardErrorTo(Console.Error).RedirectTo(Console.Out))
        );

        
#if RELEASE
        [Test]
#endif
        public async Task GenerateApiDocs()
        {
            var result = await Shell.Run(DotNetPath, new[] { "tool", "install", "DefaultDocumentation.Console", "-g" }, options: o => o.ThrowOnError(false)).Task;
            Assert.That(result.ExitCode, Is.EqualTo(0).Or.EqualTo(1)); // 1 == already installed

            var tempDirectory = Path.Combine(Path.GetTempPath(), $"DistributedLockApiDocGen_{DateTime.UtcNow.Ticks}");
            try
            {
                Directory.CreateDirectory(tempDirectory);
                await GenerateApiDocsAsync(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        private static async Task GenerateApiDocsAsync(string tempDirectory)
        {
            var publishDirectory = Path.Combine(CodeGenHelpers.SolutionDirectory, DocsFolder, ApiFolder);
            DeleteDirectoryIfExists(publishDirectory);

            var linksPath = Path.Combine(tempDirectory, "links");
            Directory.CreateDirectory(linksPath);

            var contentPath = Path.Combine(tempDirectory, "output");
            Directory.CreateDirectory(contentPath);

            var projectDirectories = Directory.GetDirectories(CodeGenHelpers.SolutionDirectory, "DistributedLock.*")
                .Where(d => !d.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase) && !d.EndsWith("DistributedLock", StringComparison.OrdinalIgnoreCase));

            for (var i = 0; i < 2; ++i)
            {
                foreach (var projectDirectory in projectDirectories)
                {
                    await GenerateApiDocsAsync(projectDirectory);
                }
            }

            Directory.Move(contentPath, publishDirectory);

            async Task GenerateApiDocsAsync(string projectDirectory)
            {
                var projectName = Path.GetFileName(projectDirectory);
                var binDirectory = Path.Combine(projectDirectory, "bin", "Release", DocsFramework);

                var config = new
                {
                    AssemblyFilePath = Path.Combine(binDirectory, projectName + ".dll"),
                    DocumentationFilePath = Path.Combine(binDirectory, projectName + ".xml"),
                    ProjectDirectoryPath = projectDirectory,
                    OutputDirectoryPath = Path.Combine(contentPath, projectName),
                    AssemblyPageName = "README",
                    GeneratedAccessModifiers = "Public,Protected,ProtectedInternal",
                    GeneratedPages = "Assembly,Namespaces,Types,Members",
                    LinksOutputFilePath = Path.Combine(linksPath, projectName + ".txt"),
                    LinksBaseUrl = $"https://github.com/madelson/DistributedLock/tree/default-documentation/{DocsFolder}/{ApiFolder}/{projectName}/",
                    ExternLinksFilePaths = new string[] { Path.Combine(linksPath, "*.txt") },
                    FileNameFactory = "NameAndMd5Mix",
                };

                var configFilePath = Path.Combine(tempDirectory, projectName + "_config.json");
                File.WriteAllText(configFilePath, JsonSerializer.Serialize(config));

                DeleteDirectoryIfExists(config.OutputDirectoryPath);
                await Shell.Run("defaultdocumentation", "-j", configFilePath).Task;
            }
        }

        private static void DeleteDirectoryIfExists(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
