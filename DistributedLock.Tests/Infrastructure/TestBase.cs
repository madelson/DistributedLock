using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class TestBase
    {
        [OneTimeSetUp]
        public void TestInitialize()
        {
            TestHelper.CurrentTestType = this.GetType();
        }

        [OneTimeTearDown]
        public void TestCleanup()
        {
            TestHelper.CurrentTestType = null;
        }
    }
}
