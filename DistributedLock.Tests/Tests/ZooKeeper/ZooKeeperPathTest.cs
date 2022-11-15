using Medallion.Threading.ZooKeeper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Medallion.Threading.Tests.ZooKeeper;

[Category("CI")]
public class ZooKeeperPathTest
{
    [Test]
    public void TestRejectsNull() => Assert.Throws<ArgumentNullException>(() => new ZooKeeperPath(null!));

    [TestCase("")]
    [TestCase("a")]
    [TestCase("/a/")]
    [TestCase("/x\0b")]
    [TestCase("/.")]
    [TestCase("/b/./a")]
    [TestCase("/x/..")]
    [TestCase("/../r")]
    public void TestRejectsInvalidPaths(string path) => Assert.Throws<FormatException>(() => new ZooKeeperPath(path));

    [Test]
    public void TestRejectsPathsWithControlCharacters()
    {
        // these can't be test cases because the control characters confuse the test explorer
        this.TestRejectsInvalidPaths("/\u0000a");
        this.TestRejectsInvalidPaths("/a/\u007f");
        this.TestRejectsInvalidPaths("/a\uf8ff/b");
        this.TestRejectsInvalidPaths("/a\uffff");
    }

    [TestCase("/a.js")]
    [TestCase("/...m")]
    [TestCase("/...")]
    [TestCase("/..foo")]
    [TestCase("/a..")]
    [TestCase("/.x")]
    public void TestAllowsDotInNonRelativePath(string path) => Assert.DoesNotThrow(() => new ZooKeeperPath(path));

    [TestCase("/", "abc", ExpectedResult = "/abc")]
    [TestCase("/xyz/foo", "bar", ExpectedResult = "/xyz/foo/bar")]
    [TestCase("/", "...", ExpectedResult = "/...")]
    [TestCase("/", "", ExpectedResult = "/EMPTYz4PhNX7vuL3xVChQ1m2AB9Yg5AULVxXcg_SpIdNs6c5H0NE8XYXysP+DGNKHfuwvY7kxvUdBeoGlODJ6+SfaPg==")]
    [TestCase("/", "/", ExpectedResult = "/_XIbwNE7SSUJciq0_Jytyos4P84h5HzFJfq8lf6cmKUh_qv1_0n6w3WNV1VCeLz+vdnEQFc2SB9JI1VD96hUnTw==")]
    [TestCase("/", "a/", ExpectedResult = "/a__H_H9kmjf3WHGaL30_9h_RNn+GipsxwkovtbOR9nXFUa9lT++YHfmM7FomA2RoilE23u5yCsImr8ImvgukZ3lQ==")]
    [TestCase("/bar", "\0", ExpectedResult = "/bar/_uCRNAomB1pOve0Vq+O+kytY9KC4Z_xSULCRuUNk1HSJwSoAqccNYC2Nw3kzrKTwySoQjNCVX1OXDhDjw42kQ7g==")]
    [TestCase("/a.a", ".", ExpectedResult = "/a.a/._C2EkHXwXvLsbrucJTRS3xFHv7Mf_y9klmKDxPTE8yevCoH5h8Ae69Y+_lP+ahpW91crnzgO78elOk2E6APJfIQ==")]
    [TestCase("/b..b/a", "..", ExpectedResult = "/b..b/a/.._Rh_NqllQfVidZsBWJatUTHt1u_RuBCeYLhNba59jtMe0u1rD7QNtp2QKxdN3Ohz1E1VnJ_yfFMevOUWeSrtxxw==")]
    [TestCase("/a/b", "/..", ExpectedResult = "/a/b/_..p0p_PzMVsjkHpwCrL63ktSqk0bLNJX9_X+7BtDMfHWr5usgqZ3n5VVI3FSGJ0YPNrps_Pf7f9yxeCzz+AiD0sw==")]
    [TestCase("/", "zookeeper", ExpectedResult = "/zookeeper_yX0zkYBzsEZ1VDADNUx54LUSt9VL9M9lOPoez38tnk4FrBtllVz+ksFllir7N2yla_wy22pIOnbpcfRkcVXBag==")]
    [TestCase("/", "zooKeeper", ExpectedResult = "/zooKeeper")]
    [TestCase("/a", "zookeeper", ExpectedResult = "/a/zookeeper")]
    public string TestGetChildNodePathWithSafeName(string path, string name)
    {
        var result = new ZooKeeperPath(path).GetChildNodePathWithSafeName(name);
        Assert.DoesNotThrow(() => new ZooKeeperPath(result.ToString()), "should pass path validation");
        return result.ToString();
    }

    [Test]
    public void TestGetChildNodePathWithSafeNameHandlesControlCharacters() =>
        this.TestGetChildNodePathWithSafeName("/", "\u0000\u007f\uf8ff\uffff").ShouldEqual("/____7A++E8vPxbYKJmhCX1bUTCwjqJqW1POHfCeBk62R9hqB0Fd_uTUBIpv9mssG7K68FHZ_7wJ70UNRKsVH8CrngA==");

    [Test]
    public void TestEquality()
    {
        var paths = new[] { null, "/", "/a", "/A" }.Select(p => p == null ? default : new ZooKeeperPath(p)).ToArray();
        for (var i = 0; i < paths.Length; ++i)
        {
            for (var j = 0; j < paths.Length; ++j)
            {
                if (i == j)
                {
                    Assert.IsTrue(paths[i] == paths[j]);
                    Assert.IsFalse(paths[i] != paths[j]);
                    Assert.IsTrue(paths[i].Equals(paths[j]));
                    Assert.IsTrue(Equals(paths[i], paths[j]));
                }
                else
                {
                    Assert.IsFalse(paths[i] == paths[j]);
                    Assert.IsTrue(paths[i] != paths[j]);
                    Assert.IsFalse(paths[i].Equals(paths[j]));
                    Assert.IsFalse(Equals(paths[i], paths[j]));
                    Assert.AreNotEqual(paths[i].GetHashCode(), paths[j].GetHashCode());
                }
            }
        }
    }

    [Test]
    public void TestExposesMinimalApi()
    {
        var publicMembers = typeof(ZooKeeperPath).GetMembers()
            .Where(m => m.DeclaringType == typeof(ZooKeeperPath));
        CollectionAssert.AreEquivalent(
            new[] { "ToString", "Equals", "Equals", "GetHashCode", "op_Equality", "op_Inequality", ".ctor" },
            publicMembers.Select(m => m.Name)
        );
    }
}
