using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests.Redis;

[SetUpFixture]
public class RedisSetUpFixture
{
    [OneTimeSetUp]
    public void OneTimeSetUp() { }

    [OneTimeTearDown]
    public void OneTimeTearDown() => RedisServer.DisposeAll();
}
