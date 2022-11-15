using Medallion.Threading.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Medallion.Threading.Tests.FileSystem;

[SupportsContinuousIntegration]
public sealed class TestingFileDistributedLockProvider : TestingLockProvider<TestingLockFileSynchronizationStrategy>
{
    public override IDistributedLock CreateLockWithExactName(string name) => 
        new FileDistributedLock(new FileInfo(name));

    public override string GetSafeName(string name) =>
        new FileDistributedLock(new DirectoryInfo(Path.Combine(Path.GetTempPath(), this.GetType().Name)), name).Name;
}
