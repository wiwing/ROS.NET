local ros = require 'ros'
require 'ros.actionlib.ActionServer'
local actionlib = ros.actionlib


local serverState = 0
local clock = 0
local timeOut = 0
local readyForNewGoal = true
local workOnIt = false
local currentHandle = nil

local function ActionServer_Goal(goal_handle)
  ros.INFO("ActionServer_Goal")
  local g = goal_handle:getGoal()
  print(g)

  local r = goal_handle:createResult()
  r.result = 0
  print("result")
  print(r)
  if serverState == 0 or readyForNewGoal == false then
    print("Reject goal")
    goal_handle:setRejected(r, "reject")
    if serverState == 0 then
      serverState = 1
    end
  elseif serverState == 1 then
    print("Received goal left pending, waiting for cancel request")
    serverState = 2
    readyForNewGoal = false
  elseif serverState == 2 then
    print("Accepted goal, waiting for cancel request")
    goal_handle:setAccepted('accepted')
    serverState = 4
    readyForNewGoal = false
  elseif serverState == 4 then
    print("Accepted goal, will abort in a second")
    clock = 0
    timeOut = 100
    goal_handle:setAccepted('accepted')
    serverState = 5
    readyForNewGoal = false
    workOnIt = true
    currentHandle = goal_handle
  elseif serverState >= 5 then
    print("Accepted goal, will succeed in a second")
    goal_handle:setAccepted('accepted')
    clock = 0
    timeOut = 100
    serverState = 6
    readyForNewGoal = false
    workOnIt = true
    currentHandle = goal_handle
  end
end


local function ActionServer_Cancel(goal_handle)
  ros.INFO("ActionServer_Cancel")
  goal_handle:setCanceled(nil, 'blub')
  readyForNewGoal = true
end


local function WorkOnGoal(as)
  if workOnIt == false then
    return
  end
  clock = clock + 1
  if (timeOut - clock <= 0) then
    if serverState == 5 then
      print("abort goal")
      currentHandle:setAborted(r, 'no')
      workOnIt = false
    elseif serverState > 5 then
      serverState = 0
      print("succeeded goal")
      local r = currentHandle:createResult()
      r.result = 123
      print(r)
      currentHandle:setSucceeded(r, 'done')
      workOnIt = false
    end
    readyForNewGoal = true
  end
  if (true) then
    local fb = as:createFeeback()
    fb.feedback = clock;
    as:publishFeedback(currentHandle.goal_status, fb)
  end
end


local function testActionServer()
  ros.init('testActionServer')
  nh = ros.NodeHandle()
  ros.console.setLoggerLevel('actionlib', ros.console.Level.Debug)

  local as = actionlib.ActionServer(nh, 'test_action', 'actionlib/Test')

  as:registerGoalCallback(ActionServer_Goal)
  as:registerCancelCallback(ActionServer_Cancel)

  print('Starting action server...')
  as:start()

  while ros.ok() do
    ros.spinOnce()
    --sys.sleep(0.01)
    WorkOnGoal(as)
  end

  as:shutdown()
  nh:shutdown()
  ros.shutdown()
end


testActionServer()
