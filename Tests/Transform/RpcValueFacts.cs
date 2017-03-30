using System;
using System.Collections.Generic;
using System.Text;
using Uml.Robotics.XmlRpc;
using Xunit;

namespace UnitTests
{
    public class RpcValueFacts
    {
        [Fact]
        public void CheckSimpleArrayValuesRoundTrip()
        {
            var now = DateTime.UtcNow;
            var v = new XmlRpcValue();
            v[0].Set(-456);
            v[1].Set(true);
            v[2].Set(123.0);
            v[3].Set(new byte[] { 0,9,8,7,6,5,4,3,2,1 });
            v[4].Set(now);
            v[5].Set("Test String");

            var serialized = v.ToXml();

            var w = new XmlRpcValue();
            w.FromXml(serialized);

            Assert.Equal(XmlRpcType.Int, w[0].Type);
            Assert.Equal(v[0].GetInt(), w[0].GetInt());
            Assert.Equal(-456, w[0].GetInt());

            Assert.Equal(XmlRpcType.Boolean, w[1].Type);
            Assert.Equal(v[1].GetBool(), w[1].GetBool());
            Assert.Equal(true, w[1].GetBool());

            Assert.Equal(XmlRpcType.Double, w[2].Type);
            Assert.Equal(v[2].GetDouble(), w[2].GetDouble(), 2);
            Assert.Equal(123.0, w[2].GetDouble(), 2);

            Assert.Equal(XmlRpcType.Base64, w[3].Type);
            Assert.Equal(v[3].GetBinary(), w[3].GetBinary());
            Assert.Equal(new byte[] { 0, 9, 8, 7, 6, 5, 4, 3, 2, 1 }, w[3].GetBinary());

            Assert.Equal(XmlRpcType.DateTime, w[4].Type);
            Assert.True(Math.Abs((v[4].GetDateTime() - w[4].GetDateTime()).TotalSeconds) < 1);
            Assert.True(Math.Abs((now - w[4].GetDateTime()).TotalSeconds) < 1);

            Assert.Equal(XmlRpcType.String, w[5].Type);
            Assert.Equal(v[5].GetString(), w[5].GetString());
            Assert.Equal("Test String", w[5].GetString());
        }
    }
}
