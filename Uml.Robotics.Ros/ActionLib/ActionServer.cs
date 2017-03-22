using System;
using System.Collections.Generic;
using System.Text;

using Messages.std_msgs;
using Messages.actionlib_msgs;
using Uml.Robotics.Ros.ActionLib.Interfaces;
using Messages;


namespace Uml.Robotics.Ros.ActionLib
{
    class ActionServer<TGoal, TResult, TFeedback> : IActionServer<TGoal, TResult, TFeedback>
        where TGoal : RosMessage, IActionGoal, new()
        where TResult : RosMessage, IActionResult, new()
        where TFeedback : RosMessage, IActionFeedback, new()
    {
        public int QueueSize { get; set; } = 50;
        public TimeSpan StatusListTimeout { get; set; } = new TimeSpan(0, 0, 5);
        public double StatusFrequencySeconds { get; set; } = 5;

        private bool started;
        private Dictionary<string, ServerGoalHandle<TGoal, TResult, TFeedback>> goalHandles;
        private NodeHandle nodeHandle;
        private DateTime lastCancel;
        private Action<ServerGoalHandle<TGoal, TResult, TFeedback>> goalCallback;
        private Action<ServerGoalHandle<TGoal, TResult, TFeedback>> cancelCallback;
        private Publisher<TResult> resultPublisher;
        private Publisher<TFeedback> feedbackPublisher;
        private Publisher<GoalStatusArray> goalStatusPublisher;
        private Subscriber<TGoal> goalSubscriber;
        private Subscriber<GoalID> cancelSubscriber;
        private TimeSpan statusInterval;
        private DateTime nextStatusPublishTime;
        private ulong spinCallbackId = 0;


        public ActionServer(NodeHandle nodeHandle, string actionName)
        {
            this.goalHandles = new Dictionary<string, ServerGoalHandle<TGoal, TResult, TFeedback>>();
            this.nodeHandle = new NodeHandle(nodeHandle, actionName);
            this.lastCancel = DateTime.Now;
            this.started = false;
        }


        public TFeedback CreateFeedback()
        {
            var feedback = new TFeedback();
            return feedback;
        }


        public TResult CreateResult()
        {
            var result = new TResult();
            return result;
        }


        public void RegisterCancelCallback(Action<ServerGoalHandle<TGoal, TResult, TFeedback>> cancelCallback)
        {
            this.cancelCallback = cancelCallback;
        }


        public void RegisterGoalCallback(Action<ServerGoalHandle<TGoal, TResult, TFeedback>> goalCallback)
        {
            this.goalCallback = goalCallback;
        }


        public void Shutdown()
        {
            if (spinCallbackId != 0)
            {
                ROS.GlobalCallbackQueue.removeByID(spinCallbackId);
                spinCallbackId = 0;
            }
            resultPublisher.shutdown();
            feedbackPublisher.shutdown();
            goalStatusPublisher.shutdown();
            goalSubscriber.shutdown();
            cancelSubscriber.shutdown();
        }


        public void Start()
        {
            if (started)
            {
                return;
            }

            // Emmitting topics
            resultPublisher = nodeHandle.advertise<TResult>("result", QueueSize);
            feedbackPublisher = nodeHandle.advertise<TFeedback>("feedback", QueueSize);
            goalStatusPublisher = nodeHandle.advertise<GoalStatusArray>("status", QueueSize);

            if (StatusFrequencySeconds > 0)
            {
                int seconds = (int)StatusFrequencySeconds;
                int milliseconds = (int)((StatusFrequencySeconds - seconds) * 1000);
                statusInterval = new TimeSpan(0, 0, seconds, milliseconds);
                nextStatusPublishTime = DateTime.Now + statusInterval;
                spinCallbackId = (ulong)(DateTime.Now.Ticks + (new Random()).Next());
                ROS.GlobalCallbackQueue.addCallback(new CallbackInterface(SpinCallback), spinCallbackId);
            }

            // Message consumers
            goalSubscriber = nodeHandle.subscribe<TGoal>("goal", (uint)QueueSize, GoalCallback);
            cancelSubscriber = nodeHandle.subscribe<GoalID>("cancel", (uint)QueueSize, CancelCallback);

            started = true;
            PublishStatus();
        }


        public void PublishFeedback(GoalStatus goalStatus, TFeedback feedback)
        {
            var newFeedback = new TFeedback();
            newFeedback.Header.stamp = ROS.GetTime();
            newFeedback.GoalStatus = goalStatus;
            newFeedback.Feedback = feedback;
            ROS.Debug()("actionlib", $"Publishing feedback for goal with id: {goalStatus.goal_id.id} and stamp: " +
                $"{new DateTimeOffset(ROS.GetTime(goalStatus.goal_id.stamp)).ToUnixTimeSeconds()}"
            );
            feedbackPublisher.publish(newFeedback);
        }


        public void PublishResult(GoalStatus goalStatus, TResult result)
        {
            var newResult = new TResult();
            newResult.Header.stamp = ROS.GetTime();
            newResult.GoalStatus = goalStatus;
            if (result != null)
            {
                newResult.Result = result.Result;
            }
            ROS.Debug()("actionlib", $"Publishing result for goal with id: {goalStatus.goal_id.id} and stamp: " +
                $"{new DateTimeOffset(ROS.GetTime(goalStatus.goal_id.stamp)).ToUnixTimeSeconds()}"
            );
            resultPublisher.publish(newResult);
            PublishStatus();
        }


        public void PublishStatus()
        {
            var now = DateTime.Now;
            var statusArray = new GoalStatusArray();
            statusArray.header.stamp = ROS.GetTime(now);
            var goalStatuses = new List<GoalStatus>();

            foreach (var pair in goalHandles)
            {
                goalStatuses.Add(pair.Value.GoalStatus);

                if ((pair.Value.DestructionTime != null) && (pair.Value.DestructionTime + StatusListTimeout < now))
                {
                    ROS.Debug()("actionlib", $"Removing server goal handle for goal id: {pair.Value.GoalId.id}");
                    goalHandles.Remove(pair.Value.GoalId.id);
                }
            }
        }


        private void CancelCallback(GoalID goalId)
        {
            if (!started)
            {
                return;
            }

            ROS.Debug()("actionlib", "The action server has received a new cancel request");

            if (goalId.id == null)
            {
                var timeZero = DateTime.Now;

                foreach(var valuePair in goalHandles)
                {
                    var goalHandle = valuePair.Value;
                    if ((ROS.GetTime(goalId.stamp) == timeZero) || (ROS.GetTime(goalHandle.GoalId.stamp) < ROS.GetTime(goalId.stamp)))
                    {
                        if (goalHandle.SetCancelRequested() && (cancelCallback != null))
                        {
                            cancelCallback(goalHandle);
                        }
                    }
                }
            } else
            {
                ServerGoalHandle<TGoal, TResult, TFeedback> goalHandle;
                var foundGoalHandle = goalHandles.TryGetValue(goalId.id, out goalHandle);
                if (foundGoalHandle)
                {
                    if (goalHandle.SetCancelRequested() && (cancelCallback != null))
                    {
                        cancelCallback(goalHandle);
                    }
                } else
                {
                    // We have not received the goal yet, prepare to cancel goal when it is received
                    var goalStatus = new GoalStatus();
                    goalStatus.status = GoalStatus.RECALLING;
                    goalHandle = new ServerGoalHandle<TGoal, TResult, TFeedback>(this, goalId, goalStatus, null);
                    goalHandle.DestructionTime = ROS.GetTime(goalId.stamp);
                    goalHandles[goalId.id] = goalHandle;
                }

            }

            // Make sure to set lastCancel based on the stamp associated with this cancel request
            if (ROS.GetTime(goalId.stamp) > lastCancel)
            {
                lastCancel = ROS.GetTime(goalId.stamp);
            }
        }


        private void GoalCallback(TGoal goal)
        {
            if (!started)
            {
                return;
            }

            GoalID goalId = goal.GoalId;

            ROS.Debug()("actionlib", "The action server has received a new goal request");
            ServerGoalHandle<TGoal, TResult, TFeedback> observedGoalHandle = null;
            if (goalHandles.ContainsKey(goalId.id))
            {
                observedGoalHandle = goalHandles[goalId.id];
            }

            if (observedGoalHandle != null)
            {
                // The goal could already be in a recalling state if a cancel came in before the goal
                if (observedGoalHandle.GoalStatus.status == GoalStatus.RECALLING)
                {
                    observedGoalHandle.GoalStatus.status = GoalStatus.RECALLED;
                    PublishResult(observedGoalHandle.GoalStatus, null); // Empty result
                }
            } else
            {
                // Create and register new goal handle
                GoalStatus goalStatus = new GoalStatus();
                goalStatus.status = GoalStatus.PENDING;
                var newGoalHandle = new ServerGoalHandle<TGoal, TResult, TFeedback>(this, goalId,
                    goalStatus, goal
                );
                goalHandles[goalId.id] = newGoalHandle;
                goalCallback?.Invoke(newGoalHandle);
            }
        }


        private void SpinCallback(RosMessage message)
        {
            if (DateTime.Now > nextStatusPublishTime)
            {
                PublishStatus();
                nextStatusPublishTime = DateTime.Now + statusInterval;
            }
        }
    }
}
