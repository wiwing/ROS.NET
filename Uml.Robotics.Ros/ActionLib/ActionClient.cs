using System;
using System.Collections.Generic;
using System.Text;

using Messages;
using Messages.actionlib_msgs;

namespace Uml.Robotics.Ros.ActionLib
{
    class ActionClient<TGoal, TResult, TFeedback>
        where TGoal : InnerActionMessage, new()
        where TResult : InnerActionMessage, new()
        where TFeedback : InnerActionMessage, new()
    {
        public string Name { get; private set; }
        public int QueueSize { get; set; } = 50;

        private NodeHandle nodeHandle;
        private bool statusReceived;
        private List<TGoal> goals;
        private Dictionary<string, int> goalSubscriberCount;
        private Dictionary<string, int> cancelSubscriberCount;
        private Subscriber<GoalStatusArray> statusSubscriber;
        private Subscriber<FeedbackActionMessage<TFeedback>> feedbackSubscriber;
        private Subscriber<ResultActionMessage<TResult>> resultSubscriber;
        private Publisher<GoalActionMessage<TGoal>> goalPublisher;
        private Publisher<GoalID> cancelPublisher;
        private int nextGoalId = 0; // Shared amon all clients


        public ActionClient(string name, NodeHandle parentNodeHandle)
        {
            this.Name = name;
            this.nodeHandle = new NodeHandle(parentNodeHandle, name);
            this.statusReceived = false;
            this.goals = new List<TGoal>();
            this.goalSubscriberCount = new Dictionary<string, int>();
            this.cancelSubscriberCount = new Dictionary<string, int>();

            statusSubscriber = nodeHandle.subscribe<GoalStatusArray>("status", (uint)QueueSize, OnStatusMessage);
            feedbackSubscriber = nodeHandle.subscribe<FeedbackActionMessage<TFeedback>>("status", (uint)QueueSize, OnFeedbackMessage);
            resultSubscriber = nodeHandle.subscribe<ResultActionMessage<TResult>>("status", (uint)QueueSize, OnResultMessage);

            goalPublisher = nodeHandle.advertise<GoalActionMessage<TGoal>>("goal", QueueSize, OnGoalConnectCallback,
                OnGoalDisconnectCallback
            );
            cancelPublisher = nodeHandle.advertise<GoalID>("cancel", QueueSize, OnCancelConnectCallback,
                OnCancelDisconnectCallback);
        }


        public void SendGoal(TGoal goal, Action<ClientGoalHandle<TGoal, TResult, TFeedback>> OnTransistionCallback)
        {
            var goalAction = new GoalActionMessage<TGoal>();

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


        private void OnFeedbackMessage(FeedbackActionMessage<TFeedback> feedback)
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


        private void OnResultMessage(ResultActionMessage<TResult> result)
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
