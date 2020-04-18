using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Medallion.Threading.Azure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Azure
{
    /// <summary>
    /// Demonstrates various behaviors of Azure blob storage that our implementation relies upon or takes into account
    /// </summary>
    public class AzureBehaviorTest
    {
        [Test]
        public void TestAttemptToLeaseBlobIfDoesNotExist()
        {
            var blobClient = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, Guid.NewGuid().ToString());
            
            Assert.Throws<RequestFailedException>(() => blobClient.GetBlobLeaseClient().Acquire(TimeSpan.FromMinutes(1)))
                .ErrorCode.ShouldEqual(AzureErrors.BlobNotFound);

            blobClient = new BlobClient(AzureCredentials.ConnectionString, "dne-container", Guid.NewGuid().ToString());
            Assert.Throws<RequestFailedException>(() => blobClient.GetBlobLeaseClient().Acquire(TimeSpan.FromMinutes(1)))
                .ErrorCode.ShouldEqual("ContainerNotFound");
        }

        [Test]
        public void TestUploadToWrongBlobType([Values("block", "page", "append")] string actualType)
        {
            var blobName = Guid.NewGuid().ToString();

            var createFunctions = new Dictionary<string, Action>
            {
                ["block"] = () => new BlockBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, blobName).Upload(Stream.Null),
                ["page"] = () => new PageBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, blobName).CreateIfNotExists(size: 0),
                ["append"] = () => new AppendBlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, blobName).CreateIfNotExists()
            };

            // right now append blobs aren't fully supported, so we can't fully test this
            if (actualType == "append")
            {
                Assert.Throws<RequestFailedException>(() => createFunctions[actualType]())
                    .ErrorCode.ShouldEqual("FeatureNotSupportedByEmulator");
                return;
            }
            else
            {
                createFunctions.Remove("append"); // to prevent it from failing below
            }

            var baseClient = new BlobBaseClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, blobName);

            Assert.IsFalse(baseClient.Exists());
            createFunctions[actualType]();
            Assert.IsTrue(baseClient.Exists());

            foreach (var kvp in createFunctions)
            {
                if (kvp.Key == actualType)
                {
                    Assert.DoesNotThrow(() => kvp.Value());
                }
                else
                {
                    Assert.Throws<RequestFailedException>(() => kvp.Value())
                        .ErrorCode.ShouldEqual("InvalidBlobType");
                }
            }
        }

        [Test]
        public void TestSlashEquivalence()
        {
            var name = Guid.NewGuid() + "/a";

            var blobClient1 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
            Assert.IsFalse(blobClient1.Exists());
            blobClient1.Upload(Stream.Null);
            Assert.IsTrue(blobClient1.Exists());

            var blobClient2 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name.Replace('/', '\\'));
            Assert.IsTrue(blobClient2.Exists());
        }

        [Test]
        public void TestThrowsIfLeaseAlreadyHeld()
        {
            var name = nameof(TestThrowsIfLeaseAlreadyHeld) + Guid.NewGuid();
            var client1 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
            client1.Upload(Stream.Null);
            var client2 = new BlobClient(AzureCredentials.ConnectionString, AzureCredentials.DefaultBlobContainerName, name);
            Assert.DoesNotThrow(() => client1.GetBlobLeaseClient().Acquire(TimeSpan.FromSeconds(15)));
            Assert.Throws<RequestFailedException>(() => client2.GetBlobLeaseClient().Acquire(TimeSpan.FromSeconds(15)))
                .ErrorCode.ShouldEqual("LeaseAlreadyPresent");
        }
    }
}
