using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests.Tests
{
    [TestClass]
    public class DeadlockExceptionTest : TestBase
    {
        [TestMethod]
        public void TestDeadlockExceptionSerialization()
        {
            void ThrowDeadlockException() => throw new DeadlockException(nameof(TestDeadlockExceptionSerialization), new InvalidOperationException("foo"));

            DeadlockException deadlockException = null;
            try { ThrowDeadlockException(); }
            catch (DeadlockException ex) { deadlockException = ex; }

            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, deadlockException);

            stream.Position = 0;
            var deserialized = (DeadlockException)formatter.Deserialize(stream);
            deserialized.Message.ShouldEqual(deadlockException.Message);
            deserialized.StackTrace.ShouldEqual(deadlockException.StackTrace);
            deserialized.InnerException.Message.ShouldEqual(deadlockException.InnerException.Message);
        }
    }
}
