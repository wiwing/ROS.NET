using System;
using System.Collections.Generic;
using System.Text;

using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    public interface IActionClient<TResult, TGoalAction, TFeedbackAction>
    {
        void PublishCancel(GoalID goalId);
        void PublishGoal(TGoalAction goal);
        void TransistionToState(IActionClient<TResult, TGoalAction, TFeedbackAction> actionClient,
            ClientGoalHandle<TResult, TGoalAction, TFeedbackAction> goalHandle, CommunicationState state);
    }
}
