using System;

namespace Uml.Robotics.Ros
{
    public interface ICallbackQueue
    {
        void AddCallback(CallbackInterface callback);
        void AddCallback(CallbackInterface callback, UInt64 owner_id);
        bool CallAvailable();
        bool CallAvailable(int timeOut);

        void RemoveById(UInt64 owner_id);

        void Enable();
        void Disable();
        void Clear();
    }
}
