using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Text;

using Messages;
using Messages.actionlib_msgs;
using Messages.std_msgs;
using System.Threading;

namespace Uml.Robotics.Ros.ActionLib
{
    public class ActionClient<TGoal, TResult, TFeedback> : IActionClient<TGoal, TResult, TFeedback>
        where TGoal : InnerActionMessage, new()
        where TResult : InnerActionMessage, new()
        where TFeedback : InnerActionMessage, new()
    {
        public string Name { get; private set; }
        public int QueueSize { get; set; } = 50;
        public Publisher<GoalActionMessage<TGoal>> GoalPublisher { get; private set; }
        public Publisher<GoalID> CancelPublisher { get; private set; }
        public Time LatestStatusTime { get; private set; }

        private NodeHandle nodeHandle;
        private bool statusReceived;
        private Dictionary<string, ClientGoalHandle<TGoal, TResult, TFeedback>> goalHandles;
        private Dictionary<string, int> goalSubscriberCount;
        private Dictionary<string, int> cancelSubscriberCount;
        private Subscriber<GoalStatusArray> statusSubscriber;
        private Subscriber<FeedbackActionMessage<TFeedback>> feedbackSubscriber;
        private Subscriber<ResultActionMessage<TResult>> resultSubscriber;
        private int nextGoalId = 0; // Shared amon all clients
        private string statusCallerId = null;


        public ActionClient(string name, NodeHandle parentNodeHandle)
        {
            this.Name = name;
            this.nodeHandle = new NodeHandle(parentNodeHandle, name);
            this.statusReceived = false;
            this.goalHandles = new Dictionary<string, ClientGoalHandle<TGoal, TResult, TFeedback>>();
            this.goalSubscriberCount = new Dictionary<string, int>();
            this.cancelSubscriberCount = new Dictionary<string, int>();

            statusSubscriber = nodeHandle.subscribe<GoalStatusArray>("status", (uint)QueueSize, OnStatusMessage);
            feedbackSubscriber = nodeHandle.subscribe<FeedbackActionMessage<TFeedback>>("feedback", (uint)QueueSize, OnFeedbackMessage);
            resultSubscriber = nodeHandle.subscribe<ResultActionMessage<TResult>>("result", (uint)QueueSize, OnResultMessage);

            GoalPublisher = nodeHandle.advertise<GoalActionMessage<TGoal>>("goal", QueueSize, OnGoalConnectCallback,
                OnGoalDisconnectCallback
            );
            CancelPublisher = nodeHandle.advertise<GoalID>("cancel", QueueSize, OnCancelConnectCallback,
                OnCancelDisconnectCallback);
        }


        /// <summary>
        /// Cancel all goals that were stamped at and before the specified time. All goals stamped at or before `time` will be canceled
        /// </summary>
        public void CancelGoalsAtAndBeforeTime(Time time)
        {
            var cancelMessage = new GoalID();
            cancelMessage.stamp = time;
            CancelPublisher.publish(cancelMessage);
        }


        /// <summary>
        /// Cancel all goals currently running on the action server
        /// This preempts all goals running on the action server at the point that this message is serviced by the ActionServer.
        /// </summary>
        public void CancelGoalsAtAndBeforeTime()
        {
            var time = DateTime.Now;
            CancelGoalsAtAndBeforeTime(ROS.GetTime(time));
        }


        public TGoal CreateGoal()
        {
            return new TGoal();
        }


        public ClientGoalHandle<TGoal, TResult, TFeedback> SendGoal(TGoal goal,
            Action<ClientGoalHandle<TGoal, TResult, TFeedback>> OnTransistionCallback,
            Action<ClientGoalHandle<TGoal, TResult, TFeedback>, FeedbackActionMessage<TFeedback>> OnFeedbackCallback)
        {
            // Create Goal Message;
            var goalId = new GoalID();
            var now = ROS.GetTime();
            goalId.id = $"{this_node.Name}-{nextGoalId}-{now.data.sec}.{now.data.nsec}";
            goalId.stamp = now;
            nextGoalId = nextGoalId + 1;

            // Prepaer Goal Message
            var goalAction = new GoalActionMessage<TGoal>();
            goalAction.Header = new Messages.std_msgs.Header();
            goalAction.Header.stamp = ROS.GetTime();
            goalAction.GoalId = goalId;
            goalAction.Goal = goal;

            // Register goal message
            var goalHandle = new ClientGoalHandle<TGoal, TResult, TFeedback>(this, goalAction,
                OnTransistionCallback, OnFeedbackCallback
            );
            goalHandles[goalAction.GoalId.id] = goalHandle;

            // Publish goal message
            GoalPublisher.publish(goalAction);
            ROS.Debug()("actionlib", "Goal published");

            return goalHandle;
        }


        public void Shutdown()
        {
            statusSubscriber.shutdown();
            feedbackSubscriber.shutdown();
            resultSubscriber.shutdown();
            GoalPublisher.shutdown();
            CancelPublisher.shutdown();
        }


        /// <summary>
        /// Waits for the ActionServer to connect to this client
        /// Often, it can take a second for the action server & client to negotiate
        /// a connection, thus, risking the first few goals to be dropped.This call lets
        /// the user wait until the network connection to the server is negotiated
        /// NOTE: Using this call in a single threaded ROS application, or any
        /// application where the action client's callback queue is not being
        /// serviced, will not work.Without a separate thread servicing the queue, or
        /// a multi-threaded spinner, there is no way for the client to tell whether
        /// or not the server is up because it can't receive a status message.
        /// </summary>
        /// <param name="timeout">timeout Max time to block before returning. A zero timeout is interpreted as an infinite timeout.</param>
        /// <returns>True if the server connected in the allocated time, false on timeout</returns>
        public bool WaitForActionServerToStart(TimeSpan timeout)
        {
            var tic = DateTime.Now;
            var spinner = new SingleThreadSpinner();
            while (ROS.ok) {
                if (IsServerConnected())
                {
                    return true;
                }

                if (timeout != null)
                {
                    var toc = DateTime.Now;
                    if (toc - tic > timeout)
                    {
                        return false;
                    }
                }

                // ToDo: ROS.spinOnce not implemented?
                Thread.Sleep(1);
            }

            return false;
        }


        public void TransitionToState(ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle, CommunicationState nextState)
        {
            ROS.Debug()("actionlib", $"Transitioning CommState from {goalHandle.State} to {nextState}");
            goalHandle.State = nextState;
            goalHandle.OnTransitionCallback?.Invoke(goalHandle);
        }


        public bool IsServerConnected()
        {
            if (!statusReceived)
            {
                ROS.Debug()("actionlib", "isServerConnected: Didn't receive status yet, so not connected yet");
                return false;
            }

            if (!goalSubscriberCount.ContainsKey(statusCallerId))
            {
                ROS.Debug()("actionlib", $"isServerConnected: Server {statusCallerId} has not yet subscribed to the cancel " +
                    $"topic, so not connected yet"
                );
                ROS.Debug()("actionlib", FormatSubscriberDebugString("goalSubscribers", goalSubscriberCount));
                return false;
            }

            if (!cancelSubscriberCount.ContainsKey(statusCallerId))
            {
                ROS.Debug()("actionlib", $"isServerConnected: Server {statusCallerId} has not yet subscribed to the cancel " +
                    $"topic, so not connected yet"
                );
                ROS.Debug()("actionlib", FormatSubscriberDebugString("goalSubscribers", cancelSubscriberCount));
                return false;
            }

            if (feedbackSubscriber.NumPublishers == 0)
            {
                ROS.Debug()("actionlib", $"isServerConnected: Client has not yet connected to feedback topic of server " +
                    $"{statusCallerId}"
                );
                return false;
            }

            if (resultSubscriber.NumPublishers == 0)
            {
                ROS.Debug()("actionlib", $"isServerConnected: Client has not yet connected to feedback topic of server " +
                    $"{statusCallerId}"
                );
                return false;
            }

            ROS.Debug()("actionlib", $"isServerConnected: Server {statusCallerId} is fully connected.");
            return true;
        }


        private GoalStatus FindGoalInStatusList(GoalStatusArray statusArray, string goalId)
        {
            for (int i = 0; i < statusArray.status_list.Length; i++)
            {
                if (statusArray.status_list[i].goal_id.id == goalId)
                {
                    return statusArray.status_list[i];
                }
            }

            return null;
        }


        private string FormatSubscriberDebugString(string name, Dictionary<string, int> subscriberCount)
        {
            var result = name + $" ({subscriberCount.Count} total)";
            foreach (var pair in subscriberCount)
            {
                result += pair.Key + " ";
            }

            return result;
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
            }
            else
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
            ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle;
            var goalExists = goalHandles.TryGetValue(feedback.GoalStatus.goal_id.id, out goalHandle);
            if (goalExists && (goalHandle.OnFeedbackCallback != null))
            {
                goalHandle.OnFeedbackCallback(goalHandle, feedback);
            }
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
            }
            else
            {
                ROS.Debug()("actionlib", $"goalDisconnectCallback: Removing {publisher.subscriber_name} from goalSubscribers, " +
                    $"(remaining with same name: {subscriberCount - 1})"
                );
                if (subscriberCount <= 1)
                {
                    goalSubscriberCount.Remove(publisher.subscriber_name);
                }
                else
                {
                    goalSubscriberCount[publisher.subscriber_name] = subscriberCount - 1;
                }
            }
        }


        private void OnResultMessage(ResultActionMessage<TResult> result)
        {
            ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle;
            var goalExists = goalHandles.TryGetValue(result.GoalStatus.goal_id.id, out goalHandle);
            if (goalExists)
            {
                goalHandle.LatestGoalStatus = result.GoalStatus;
                goalHandle.LatestResultAction = result;

                if (goalHandle.State == CommunicationState.DONE)
                {
                    ROS.Error()("actionlib", "Got a result when we were already in the DONE state (goal_id:" +
                        $" {result.GoalStatus.goal_id.id})"
                    );
                }
                else if ((goalHandle.State == CommunicationState.WAITING_FOR_GOAL_ACK) ||
                    (goalHandle.State == CommunicationState.WAITING_FOR_GOAL_ACK) ||
                    (goalHandle.State == CommunicationState.PENDING) ||
                    (goalHandle.State == CommunicationState.ACTIVE) ||
                    (goalHandle.State == CommunicationState.WAITING_FOR_RESULT) ||
                    (goalHandle.State == CommunicationState.WAITING_FOR_CANCEL_ACK) ||
                    (goalHandle.State == CommunicationState.RECALLING) ||
                    (goalHandle.State == CommunicationState.PREEMPTING))
                {
                    UpdateStatus(goalHandle, result.GoalStatus);
                    TransitionToState(goalHandle, CommunicationState.DONE);
                } else
                {
                    ROS.Error()("actionlib", $"Invalid comm for result message state: {goalHandle.State}.");
                }
            }
        }


        private void OnStatusMessage(GoalStatusArray statusArray)
        {
            string callerId;
            bool callerIdPresent = statusArray.connection_header.TryGetValue("callerid", out callerId);
            if (callerIdPresent)
            {
                ROS.Debug()("actionlib", $"Getting status over the wire (callerid: {callerId}; count: " +
                    $"{statusArray.status_list.Length})."
                );

                if (statusReceived)
                {
                    if (statusCallerId != callerId)
                    {
                        ROS.Warn()("actionlib", $"onStatusMessage: Previously received status from {statusCallerId}, but we now" +
                            $" received status from {callerId}. Did the ActionServer change?"
                        );
                        statusCallerId = callerId;
                    }
                }
                else
                {
                    ROS.Debug()("actionlib", "onStatusMessage: Just got our first status message from the ActionServer at " +
                        $"node {callerId}"
                    );
                    statusReceived = true;
                    statusCallerId = callerId;
                }
                LatestStatusTime = statusArray.header.stamp;

                // Process status message
                foreach (var pair in goalHandles)
                {
                    var goalStatus = FindGoalInStatusList(statusArray, pair.Key);
                    UpdateStatus(pair.Value, goalStatus);
                }
            } else
            {
                ROS.Error()("actionlib", "Received StatusMessage with no caller ID");
            }
        }


        private void ProcessLost(ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle)
        {
            ROS.Warn()("actionlib", "Transitioning goal to LOST");
            if (goalHandle.LatestGoalStatus != null)
            {
                goalHandle.LatestGoalStatus.status = GoalStatus.LOST;
                goalHandle.LatestGoalStatus.text = "LOST";
            }
            TransitionToState(goalHandle, CommunicationState.DONE);
        }


        private void UpdateStatus(ClientGoalHandle<TGoal, TResult, TFeedback> goalHandle, GoalStatus goalStatus)
        {
            // Check if ping action is correctly reflected by the status message
            if (goalStatus != null)
            {
                goalHandle.LatestGoalStatus = goalStatus;
            }
            else
            {
                if ((goalHandle.State != CommunicationState.WAITING_FOR_GOAL_ACK) &&
                    (goalHandle.State != CommunicationState.WAITING_FOR_RESULT) &&
                    (goalHandle.State != CommunicationState.DONE))
                {
                    ProcessLost(goalHandle);
                    return;
                }
            }

            if (goalHandle.State == CommunicationState.WAITING_FOR_GOAL_ACK)
            {
                if (goalStatus.status == GoalStatus.PENDING)
                {
                    TransitionToState(goalHandle, CommunicationState.PENDING);
                }
                else if (goalStatus.status == GoalStatus.ACTIVE)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTED)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.SUCCEEDED)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.ABORTED)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.REJECTED)
                {
                    TransitionToState(goalHandle, CommunicationState.PENDING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.RECALLED)
                {
                    TransitionToState(goalHandle, CommunicationState.PENDING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    TransitionToState(goalHandle, CommunicationState.PENDING);
                    TransitionToState(goalHandle, CommunicationState.RECALLING);
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown status from the ActionServer. status = %u", goalStatus.status);
                }
            }
            else if (goalHandle.State == CommunicationState.PENDING)
            {
                if (goalStatus.status == GoalStatus.PENDING)
                {
                    // NOP
                }
                else if (goalStatus.status == GoalStatus.ACTIVE)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTED)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.SUCCEEDED)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.ABORTED)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.REJECTED)
                {
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.RECALLED)
                {
                    TransitionToState(goalHandle, CommunicationState.RECALLING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    TransitionToState(goalHandle, CommunicationState.ACTIVE);
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    TransitionToState(goalHandle, CommunicationState.RECALLING);
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown status from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else if (goalHandle.State == CommunicationState.ACTIVE)
            {

                if (goalStatus.status == GoalStatus.PENDING)
                {
                    ROS.Error()("actionlib", "Invalid transition from ACTIVE to PENDING");
                }
                else if (goalStatus.status == GoalStatus.ACTIVE)
                {
                    // NOP
                }
                else if (goalStatus.status == GoalStatus.REJECTED)
                {
                    ROS.Error()("actionlib", "Invalid transition from ACTIVE to REJECTED");
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    ROS.Error()("actionlib", "Invalid transition from ACTIVE to RECALLING");
                }
                else if (goalStatus.status == GoalStatus.RECALLED)
                {
                    ROS.Error()("actionlib", "Invalid transition from ACTIVE to RECALLED");
                }
                else if (goalStatus.status == GoalStatus.PREEMPTED)
                {
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.SUCCEEDED ||
                  goalStatus.status == GoalStatus.ABORTED)
                {
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown status from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else if (goalHandle.State == CommunicationState.WAITING_FOR_RESULT)
            {

                if (goalStatus.status == GoalStatus.PENDING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from WAITING_FOR_RESUT to PENDING");
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    ROS.Error()("actionlib", "Invalid transition from WAITING_FOR_RESUT to PREEMPTING");
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    ROS.Error()("actionlib", "Invalid transition from WAITING_FOR_RESUT to RECALLING");
                }
                else if (goalStatus.status == GoalStatus.ACTIVE ||
                  goalStatus.status == GoalStatus.PREEMPTED ||
                  goalStatus.status == GoalStatus.SUCCEEDED ||
                  goalStatus.status == GoalStatus.ABORTED ||
                  goalStatus.status == GoalStatus.REJECTED ||
                  goalStatus.status == GoalStatus.RECALLED)
                {
                    // NOP
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown status from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else if (goalHandle.State == CommunicationState.WAITING_FOR_CANCEL_ACK)
            {

                if (goalStatus.status == GoalStatus.PENDING ||
                    goalStatus.status == GoalStatus.ACTIVE)
                {
                    // NOP
                }
                else if (goalStatus.status == GoalStatus.SUCCEEDED ||
                  goalStatus.status == GoalStatus.ABORTED ||
                  goalStatus.status == GoalStatus.PREEMPTED)
                {
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.RECALLED)
                {
                    TransitionToState(goalHandle, CommunicationState.RECALLING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.REJECTED)
                {
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    TransitionToState(goalHandle, CommunicationState.RECALLING);
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown State from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else if (goalHandle.State == CommunicationState.RECALLING)
            {

                if (goalStatus.status == GoalStatus.PENDING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from RECALLING to PENDING");
                }
                else if (goalStatus.status == GoalStatus.ACTIVE)
                {
                    ROS.Error()("actionlib", "Invalid Transition from RECALLING to ACTIVE");
                }
                else if (goalStatus.status == GoalStatus.SUCCEEDED ||
                  goalStatus.status == GoalStatus.ABORTED ||
                  goalStatus.status == GoalStatus.PREEMPTED)
                {
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.RECALLED)
                {
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.REJECTED)
                {
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    TransitionToState(goalHandle, CommunicationState.PREEMPTING);
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    // NOP
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown State from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else if (goalHandle.State == CommunicationState.PREEMPTING)
            {

                if (goalStatus.status == GoalStatus.PENDING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from PREEMPTING to PENDING");
                }
                else if (goalStatus.status == GoalStatus.ACTIVE)
                {
                    ROS.Error()("actionlib", "Invalid Transition from PREEMPTING to ACTIVE");
                }
                else if (goalStatus.status == GoalStatus.REJECTED)
                {
                    ROS.Error()("actionlib", "Invalid Transition from PREEMPTING to REJECTED");
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from PREEMPTING to RECALLING");
                }
                else if (goalStatus.status == GoalStatus.RECALLED)
                {
                    ROS.Error()("actionlib", "Invalid Transition from PREEMPTING to RECALLED");
                }
                else if (goalStatus.status == GoalStatus.PREEMPTED ||
                  goalStatus.status == GoalStatus.SUCCEEDED ||
                  goalStatus.status == GoalStatus.ABORTED)
                {
                    TransitionToState(goalHandle, CommunicationState.WAITING_FOR_RESULT);
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    // NOP
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown State from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else if (goalHandle.State == CommunicationState.DONE)
            {

                if (goalStatus.status == GoalStatus.PENDING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from DONE to PENDING");
                }
                else if (goalStatus.status == GoalStatus.ACTIVE)
                {
                    ROS.Error()("actionlib", "Invalid Transition from DONE to ACTIVE");
                }
                else if (goalStatus.status == GoalStatus.RECALLING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from DONE to RECALLING");
                }
                else if (goalStatus.status == GoalStatus.PREEMPTING)
                {
                    ROS.Error()("actionlib", "Invalid Transition from DONE to PREEMPTING");
                }
                else if (goalStatus.status == GoalStatus.PREEMPTED ||
                  goalStatus.status == GoalStatus.SUCCEEDED ||
                  goalStatus.status == GoalStatus.ABORTED ||
                  goalStatus.status == GoalStatus.RECALLED ||
                  goalStatus.status == GoalStatus.REJECTED)
                {
                    // NOP
                }
                else
                {
                    ROS.Error()("actionlib", "BUG: Got an unknown status from the ActionServer. status = %u", goalStatus.status);
                }


            }
            else
            {
                ROS.Error()("actionlib", "Invalid comm State: %u", goalHandle.State);
            }

        }
    }


    public enum CommunicationState
    {
        [Display(Description = "WAITING_FOR_GOAL_ACK")]
        WAITING_FOR_GOAL_ACK,
        [Display(Description = "PENDING")]
        PENDING,
        [Display(Description = "ACTIVE")]
        ACTIVE,
        [Display(Description = "WAITING_FOR_RESULT")]
        WAITING_FOR_RESULT,
        [Display(Description = "WAITING_FOR_CANCEL_ACK")]
        WAITING_FOR_CANCEL_ACK,
        [Display(Description = "RECALLING")]
        RECALLING,
        [Display(Description = "PREEMPTING")]
        PREEMPTING,
        [Display(Description = "DONE")]
        DONE
    }
}
