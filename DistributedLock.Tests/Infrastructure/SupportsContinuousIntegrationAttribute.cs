using System;
using System.Collections.Generic;
using System.Text;

namespace Medallion.Threading.Tests
{
    /// <summary>
    /// Indicates that a test infrastructure component supports being run in a remote continuous integration environment
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class SupportsContinuousIntegrationAttribute : Attribute
    {
        public bool WindowsOnly { get; set; }
    }
}
