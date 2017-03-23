using System;
using System.Collections.Generic;
using System.Text;

using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    public class ClientGoalHandle<TResult, TGoalAction, TFeedbackAction>
    {
        public string Id { get; set; }
        public TGoalAction Goal { get; set; }
        public CommunicationState State { get; set; }
        public Action<ClientGoalHandle<TResult, TGoalAction, TFeedbackAction>> OnTransitionCallback { get; set; }
        public Action<TFeedbackAction> OnFeedbackCallback { get; set; }
        public bool Active { get; set; }
        public GoalStatus LatestGoalStatus { get; set; }
        public TResult Result { get; set; }

        private IActionClient<TResult, TGoalAction, TFeedbackAction> actionClient;


        public ClientGoalHandle(IActionClient<TResult, TGoalAction, TFeedbackAction> actionClient)
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
