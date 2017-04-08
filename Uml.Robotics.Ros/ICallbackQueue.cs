using System;

namespace Uml.Robotics.Ros
{
    public interface ICallbackQueue
    {
        void AddCallback(CallbackInterface callback);
        void AddCallback(CallbackInterface callback, UInt64 owner_id);
        
        void CallAvailable(int timeout = ROS.WallDuration);

        void RemoveById(UInt64 owner_id);

        void Enable();
        void Disable();
        void Clear();
    }
}
