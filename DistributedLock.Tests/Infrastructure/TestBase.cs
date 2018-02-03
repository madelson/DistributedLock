using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    public abstract class TestBase
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestHelper.CurrentTestType = this.GetType();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            TestHelper.CurrentTestType = null;
        }
    }
}
