using System;
using System.Collections.Generic;
using System.Text;

using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib.Interfaces
{
    interface IActionServer<TGoal, TResult, TFeedback>
    {
        void PublishResult(GoalStatus goalStatus, TResult result);
        void PublishFeedback(GoalStatus goalStatus, TFeedback feedback);
        void PublishStatus();
    }
}
