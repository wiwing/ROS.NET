using Messages.actionlib_msgs;
using System;
using System.Collections.Generic;
using System.Text;

using Uml.Robotics.Ros;

namespace Uml.Robotics.Ros.ActionLib
{
    public interface IActionClient<TGoal, TResult, TFeedback>
        where TGoal : InnerActionMessage, new()
        where TResult : InnerActionMessage, new()
        where TFeedback : InnerActionMessage, new()
    {
        Publisher<GoalActionMessage<TGoal>> GoalPublisher { get; }
        Publisher<GoalID> CancelPublisher { get; }
        void TransitionToState(ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle, CommunicationState state);
    }

    public class ActionFailedExeption
    : Exception
    {
        public static string GetGoalStatusString(GoalStatus goalStatus)
        {
            if (goalStatus == null)
                return "null";

            if (Enum.IsDefined(typeof(GoalStatus), goalStatus.status))
            {
                byte status = goalStatus.status;
                return status.ToString("g");
            }

            return $"INVALID GOAL STATUS {goalStatus.status}";
        }

        public ActionFailedExeption(string actionName, Messages.actionlib_msgs.GoalStatus goalStatus)
            : base($"The action '{actionName}' failed with final goal status '{GetGoalStatusString(goalStatus)}': {goalStatus?.text}")
        {
            this.ActionName = actionName;
            this.FinalGoalStatus = (goalStatus)?.status ?? GoalStatus.LOST;
            this.StatusText = goalStatus?.text;
        }
        public string ActionName { get; }
        public byte FinalGoalStatus { get; }
        public string StatusText { get; }
    }
}
