using System;
using System.Collections.Generic;
using System.Text;

namespace Uml.Robotics.Ros
{
    public interface ICallbackQueue
    {
        void AddCallback(CallbackInterface callback);

        void AddCallback(CallbackInterface callback, UInt64 owner_id);

        void RemoveById(UInt64 owner_id);
    }
}
