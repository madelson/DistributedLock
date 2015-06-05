using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Tests
{
    internal static class TestHelper
    {
        public static T ShouldEqual<T>(this T @this, T that, string message = null)
        {
            Assert.AreEqual(actual: @this, expected: that, message: message);
            return @this;
        }

        public static TException AssertThrows<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex) 
            {
                Assert.Fail("Expected exception of type " + typeof(TException) + " but found " + ex);
            }

            Assert.Fail("Expected exception of type " + typeof(TException));

            return null; // never gets here
        }

        public static void AssertDoesNotThrow(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Assert.Fail("Failed with " + ex);
            }
        }
    }
}
