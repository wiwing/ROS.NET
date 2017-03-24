using System;
using System.Collections.Generic;
using System.Text;

using Messages;
using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    public class ClientGoalHandle<TGoal, TResult, TFeedback>
        where TGoal : InnerActionMessage, new()
        where TResult : InnerActionMessage, new()
        where TFeedback : InnerActionMessage, new()
    {
        public string Id { get; set; }
        public GoalActionMessage<TGoal> Goal { get; set; }
        public CommunicationState State { get; set; }
        public Action<ClientGoalHandle<TGoal, TResult, TFeedback>> OnTransitionCallback { get; set; }
        public Action<FeedbackActionMessage<TFeedback>> OnFeedbackCallback { get; set; }
        public bool Active { get; set; }
        public GoalStatus LatestGoalStatus { get; set; }
        public TResult Result { get; set; }

        private IActionClient<TResult, TResult, TFeedback> actionClient;


        public ClientGoalHandle(IActionClient<TResult, TResult, TFeedback> actionClient)
        {
            this.actionClient = actionClient;
        }


        public void Cancel()
        {
            throw new NotImplementedException();
        }


        public void Resend()
        {
            throw new NotImplementedException();
        }


        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}
