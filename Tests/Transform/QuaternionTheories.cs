using Xunit;
using Uml.Robotics.Ros;
using tf = Uml.Robotics.Ros.Transforms;
using System.Collections.Generic;
using Xunit.Extensions;
using System;

namespace Uml.Robotics.Ros.Tests
{

    public class QuaternionTheories
    {
        [Theory, MemberData(nameof(QuaternionData))]

        public void Should_QuaternionToRPYToQuaternion(tf.Quaternion quaternion)        
        {
            var rpy = quaternion.getRPY();
            var resultQuaternion = tf.Quaternion.FromRPY(rpy);

            Console.WriteLine("Input quaternion:");
            Console.WriteLine(quaternion.ToString());
            Console.WriteLine("Roll, Pitch, Yaw:");
            Console.WriteLine(rpy.ToString());
            Console.WriteLine("Output quaternion:");
            Console.WriteLine(resultQuaternion.ToString());

            var tolerance = 1E-05;
            Assert.InRange(Math.Abs(quaternion.x-resultQuaternion.x), 0.0, tolerance);
            Assert.InRange(Math.Abs(quaternion.y-resultQuaternion.y), 0.0, tolerance);
            Assert.InRange(Math.Abs(quaternion.z-resultQuaternion.z), 0.0, tolerance);
            Assert.InRange(Math.Abs(quaternion.w-resultQuaternion.w), 0.0, tolerance);  
        }


        public static IEnumerable<object[]> QuaternionData
        {
            get
            {                
                return new[]
                {
                    new object[] { new tf.Quaternion(0, 0, 0, 1)},
                    new object[] { new tf.Quaternion(2, 2, 2, 1)},
                    new object[] { new tf.Quaternion(3, 2, 4, 1)},
                    new object[] { new tf.Quaternion(0, Math.Sqrt(2.0)/2.0, 0, Math.Sqrt(2.0)/2.0)}
                };
            }
        }
    }
}