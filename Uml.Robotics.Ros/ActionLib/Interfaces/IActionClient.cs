using System;
using System.Collections.Generic;
using System.Text;

using Messages;
using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    public interface IActionClient<TGoal, TResult, TFeedback>
        where TGoal : InnerActionMessage, new()
        where TResult : InnerActionMessage, new()
        where TFeedback : InnerActionMessage, new()
    {
        void PublishCancel(GoalID goalId);
        void PublishGoal(GoalActionMessage<TGoal> goal);
        void TransistionToState(IActionClient<TGoal, TResult, TFeedback> actionClient,
            ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle, CommunicationState state);
    }
}
