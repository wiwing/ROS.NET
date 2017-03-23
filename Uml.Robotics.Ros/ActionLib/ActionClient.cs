using System;
using System.Collections.Generic;
using System.Text;

using Messages;
using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    class ActionClient<TGoal, TResult, TFeedback, TGoalAction, TResultAction, TFeedbackAction>
        where TGoalAction : RosMessage, IActionGoal, new()
        where TResultAction : RosMessage, IActionResult, new()
        where TFeedbackAction : RosMessage, IActionFeedback, new()
    {
        public string Name { get; private set; }
        public int QueueSize { get; set; } = 50;

        private NodeHandle nodeHandle;
        private bool statusReceived;
        private List<TGoalAction> goals;
        private Dictionary<string, int> goalSubscriberCount;
        private Dictionary<string, int> cancelSubscriberCount;
        private Subscriber<GoalStatusArray> statusSubscriber;
        private Subscriber<TFeedbackAction> feedbackSubscriber;
        private Subscriber<TResultAction> resultSubscriber;
        private Publisher<TGoalAction> goalPublisher;
        private Publisher<GoalID> cancelPublisher;
        private int nextGoalId = 0; // Shared amon all clients


        public ActionClient(string name, NodeHandle parentNodeHandle)
        {
            this.Name = name;
            this.nodeHandle = new NodeHandle(parentNodeHandle, name);
            this.statusReceived = false;
            this.goals = new List<TGoalAction>();
            this.goalSubscriberCount = new Dictionary<string, int>();
            this.cancelSubscriberCount = new Dictionary<string, int>();

            statusSubscriber = nodeHandle.subscribe<GoalStatusArray>("status", (uint)QueueSize, OnStatusMessage);
            feedbackSubscriber = nodeHandle.subscribe<TFeedbackAction>("status", (uint)QueueSize, OnFeedbackMessage);
            resultSubscriber = nodeHandle.subscribe<TResultAction>("status", (uint)QueueSize, OnResultMessage);

            goalPublisher = nodeHandle.advertise<TGoalAction>("goal", QueueSize, OnGoalConnectCallback, OnGoalDisconnectCallback);
            cancelPublisher = nodeHandle.advertise<GoalID>("cancel", QueueSize, OnCancelConnectCallback,
                OnCancelDisconnectCallback);
        }


        public void SendGoal(TGoal goal, Action<ClientGoalHandle<TResult, TGoalAction, TFeedbackAction>> OnTransistionCallback)
        {
            var goalAction = new TGoalAction();

        }


        private void OnCancelConnectCallback(SingleSubscriberPublisher publisher)
        {
            int subscriberCount = 0;
            bool subscriberExists = cancelSubscriberCount.TryGetValue(publisher.subscriber_name, out subscriberCount);
            cancelSubscriberCount[publisher.subscriber_name] = (subscriberExists ? subscriberCount : 0) + 1;
        }


        private void OnCancelDisconnectCallback(SingleSubscriberPublisher publisher)
        {
            int subscriberCount = 0;
            bool subscriberExists = cancelSubscriberCount.TryGetValue(publisher.subscriber_name, out subscriberCount);
            if (!subscriberExists)
            {
                // This should never happen. Warning has been copied from official actionlib implementation
                ROS.Warn()("actionlib", $"goalDisconnectCallback: Trying to remove {publisher.subscriber_name} from " +
                    $"goalSubscribers, but it is not in the goalSubscribers list."
                );
            } else
            {
                ROS.Debug()("actionlib", $"goalDisconnectCallback: Removing {publisher.subscriber_name} from goalSubscribers, " +
                    $"(remaining with same name: {subscriberCount - 1})"
                );
                if (subscriberCount <= 1)
                {
                    cancelSubscriberCount.Remove(publisher.subscriber_name);
                }
                else
                {
                    cancelSubscriberCount[publisher.subscriber_name] = subscriberCount - 1;
                }
            }
        }


        private void OnFeedbackMessage(TFeedbackAction feedback)
        {
            throw new NotImplementedException();
        }


        private void OnGoalConnectCallback(SingleSubscriberPublisher publisher)
        {
            int subscriberCount = 0;
            bool keyExists = goalSubscriberCount.TryGetValue(publisher.subscriber_name, out subscriberCount);
            goalSubscriberCount[publisher.subscriber_name] = (keyExists ? subscriberCount : 0) + 1;
            ROS.Debug()("actionlib", $"goalConnectCallback: Adding {publisher.subscriber_name} to goalSubscribers");
        }


        private void OnGoalDisconnectCallback(SingleSubscriberPublisher publisher)
        {
            int subscriberCount = 0;
            bool keyExists = goalSubscriberCount.TryGetValue(publisher.subscriber_name, out subscriberCount);
            if (!keyExists)
            {
                // This should never happen. Warning has been copied from official actionlib implementation
                ROS.Warn()("actionlib", $"goalDisconnectCallback: Trying to remove {publisher.subscriber_name} from " +
                    $"goalSubscribers, but it is not in the goalSubscribers list."
                );
            } else
            {
                ROS.Debug()("actionlib", $"goalDisconnectCallback: Removing {publisher.subscriber_name} from goalSubscribers, " +
                    $"(remaining with same name: {subscriberCount - 1})"
                );
                if (subscriberCount <= 1)
                {
                    goalSubscriberCount.Remove(publisher.subscriber_name);
                } else
                {
                    goalSubscriberCount[publisher.subscriber_name] = subscriberCount - 1;
                }
            }
        }


        private void OnResultMessage(TResultAction result)
        {
            throw new NotImplementedException();
        }


        private void OnStatusMessage(GoalStatusArray statusArray)
        {
            throw new NotImplementedException();
        }
    }


    public enum CommunicationState
    {
        WAITING_FOR_GOAL_ACK,
        PENDING,
        ACTIVE,
        WAITING_FOR_RESULT,
        WAITING_FOR_CANCEL_ACK,
        RECALLING,
        PREEMPTING,
        DONE
    }
}
