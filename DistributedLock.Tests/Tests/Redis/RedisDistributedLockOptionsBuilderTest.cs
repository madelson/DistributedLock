using Medallion.Threading.Redis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Medallion.Threading.Tests.Redis
{
    public class RedisDistributedLockOptionsBuilderTest
    {
        [Test]
        public void TestValidatesExpiry()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.Expiry(TimeSpan.FromSeconds(-2))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.Expiry(Timeout.InfiniteTimeSpan)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.Expiry(RedisDistributedLockOptionsBuilder.MinimumExpiry.TimeSpan - TimeSpan.FromTicks(1))));
            Assert.DoesNotThrow(() => GetOptions(o => o.Expiry(RedisDistributedLockOptionsBuilder.MinimumExpiry.TimeSpan)));
        }

        [Test]
        public void TestValidatesMinValidityTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.MinValidityTime(TimeSpan.FromSeconds(-2))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.MinValidityTime(TimeSpan.Zero)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.MinValidityTime(Timeout.InfiniteTimeSpan)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.MinValidityTime(RedisDistributedLockOptionsBuilder.DefaultExpiry.TimeSpan)));
            Assert.DoesNotThrow(() => GetOptions(
                o => o.MinValidityTime(RedisDistributedLockOptionsBuilder.DefaultExpiry.TimeSpan).Expiry(RedisDistributedLockOptionsBuilder.DefaultExpiry.TimeSpan + TimeSpan.FromMilliseconds(1))
            ));
        }

        [Test]
        public void TestValidatesExtensionCadence()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.ExtensionCadence(TimeSpan.FromSeconds(-2))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.ExtensionCadence(Timeout.InfiniteTimeSpan)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.MinValidityTime(TimeSpan.FromSeconds(1)).ExtensionCadence(TimeSpan.FromSeconds(1))));
        }

        [Test]
        public void TestValidatesBusyWaitSleepTime()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(1))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.MaxValue, TimeSpan.FromSeconds(1))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan)));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(-1))));
            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.FromSeconds(1), TimeSpan.MaxValue)));

            Assert.Throws<ArgumentOutOfRangeException>(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(1))));

            Assert.DoesNotThrow(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.Zero, TimeSpan.Zero)));
            Assert.DoesNotThrow(() => GetOptions(o => o.BusyWaitSleepTime(TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(4))));
        }

        [Test]
        public void TestDefaults()
        {
            var defaultOptions = RedisDistributedLockOptionsBuilder.GetOptions(null);
            defaultOptions.expiry.ShouldEqual(RedisDistributedLockOptionsBuilder.DefaultExpiry);
            defaultOptions.minValidityTime.ShouldEqual(TimeSpan.FromSeconds(27));
            defaultOptions.extensionCadence.ShouldEqual(TimeSpan.FromSeconds(9));
            defaultOptions.minBusyWaitSleepTime.ShouldEqual(TimeSpan.FromMilliseconds(10));
            defaultOptions.maxBusyWaitSleepTime.ShouldEqual(TimeSpan.FromMilliseconds(800));
        }

        private static void GetOptions(Action<RedisDistributedLockOptionsBuilder> options) =>
            RedisDistributedLockOptionsBuilder.GetOptions(options);
    }
}
