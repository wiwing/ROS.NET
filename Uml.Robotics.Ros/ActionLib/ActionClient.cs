using System;
using System.Collections.Generic;
using System.Text;

using Messages;
using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    class ActionClient<TGoal, TResult, TFeedback>
        where TGoal : RosMessage, IActionGoal, new()
        where TResult : RosMessage, IActionResult, new()
        where TFeedback : RosMessage, IActionFeedback, new()
    {
        public string Name { get; private set; }
        public int QueueSize { get; set; } = 50;
        private NodeHandle nodeHandle;
        private bool statusReceived;
        List<TGoal> goals;
        Dictionary<string, int> goalSubscribers;
        Dictionary<string, int> cancelSubscribers;
        Subscriber<GoalStatusArray> statusSubscriber;
        Subscriber<TFeedback> feedbackSubscriber;
        Subscriber<TResult> resultSubscriber;
        Publisher<TGoal> goalPublisher;
        Publisher<GoalID> cancelPublisher;


        public ActionClient(string name, NodeHandle parentNodeHandle)
        {
            this.Name = name;
            this.nodeHandle = new NodeHandle(parentNodeHandle, name);
            this.statusReceived = false;
            this.goals = new List<TGoal>();
            this.goalSubscribers = new Dictionary<string, int>();
            this.cancelSubscribers = new Dictionary<string, int>();

            statusSubscriber = nodeHandle.subscribe<GoalStatusArray>("status", (uint)QueueSize, OnStatusMessage);
            feedbackSubscriber = nodeHandle.subscribe<TFeedback>("status", (uint)QueueSize, OnFeedbackMessage);
            resultSubscriber = nodeHandle.subscribe<TResult>("status", (uint)QueueSize, OnResultMessage);

            goalPublisher = nodeHandle.advertise<TGoal>("goal", QueueSize, OnGoalConnectCallback, OnGoalDisconnectCallback);
            cancelPublisher = nodeHandle.advertise<GoalID>("cancel", QueueSize, OnCancelConnectCallback,
                OnCancelDisconnectCallback);
        }


        private void OnCancelConnectCallback(SingleSubscriberPublisher publisher)
        {
            throw new NotImplementedException();
        }


        private void OnCancelDisconnectCallback(SingleSubscriberPublisher publisher)
        {
            throw new NotImplementedException();
        }


        private void OnFeedbackMessage(TFeedback feedback)
        {
            throw new NotImplementedException();
        }


        private void OnGoalConnectCallback(SingleSubscriberPublisher publisher)
        {
            throw new NotImplementedException();
        }


        private void OnGoalDisconnectCallback(SingleSubscriberPublisher publisher)
        {
            throw new NotImplementedException();
        }


        private void OnResultMessage(TResult result)
        {
            throw new NotImplementedException();
        }


        private void OnStatusMessage(GoalStatusArray statusArray)
        {
            throw new NotImplementedException();
        }
    }
}
