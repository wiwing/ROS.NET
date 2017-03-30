using System;
using System.Collections.Generic;
using System.Text;

namespace Uml.Robotics.Ros
{
    public struct TimeData
    {
        public uint sec;
        public uint nsec;

        public TimeData(uint s, uint ns)
        {
            sec = s;
            nsec = ns;
        }

        public bool Equals(TimeData timer)
        {
            return (sec == timer.sec && nsec == timer.nsec);
        }
    }
}
