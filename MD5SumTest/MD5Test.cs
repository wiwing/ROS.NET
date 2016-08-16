﻿using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MD5SumTest
{
    [TestClass]
    public class MD5Test
    {
#region TEST SUMS
        private const string fuerteSums = @"ROSARIA/BumperState f81947761ff7e166a3bbaf937b9869b5
actionlib_msgs/GoalID 302881f31927c1df708a2dbab0e80ee8
actionlib_msgs/GoalStatus d388f9b87b3c471f784434d671988d4a
actionlib_msgs/GoalStatusArray 8b2b82f13216d0a8ea88bd3af735e619
actionlib_tutorials/AveragingAction 628678f2b4fa6a5951746a4a2d39e716
actionlib_tutorials/AveragingActionFeedback 78a4a09241b1791069223ae7ebd5b16b
actionlib_tutorials/AveragingActionGoal 1561825b734ebd6039851c501e3fb570
actionlib_tutorials/AveragingActionResult 8672cb489d347580acdcd05c5d497497
actionlib_tutorials/AveragingFeedback 9e8dfc53c2f2a032ca33fa80ec46fd4f
actionlib_tutorials/AveragingGoal 32c9b10ef9b253faa93b93f564762c8f
actionlib_tutorials/AveragingResult d5c7decf6df75ffb4367a05c1bcc7612
actionlib_tutorials/FibonacciAction f59df5767bf7634684781c92598b2406
actionlib_tutorials/FibonacciActionFeedback 73b8497a9f629a31c0020900e4148f07
actionlib_tutorials/FibonacciActionGoal 006871c7fa1d0e3d5fe2226bf17b2a94
actionlib_tutorials/FibonacciActionResult bee73a9fe29ae25e966e105f5553dd03
actionlib_tutorials/FibonacciFeedback b81e37d2a31925a0e8ae261a8699cb79
actionlib_tutorials/FibonacciGoal 6889063349a00b249bd1661df429d822
actionlib_tutorials/FibonacciResult b81e37d2a31925a0e8ae261a8699cb79
arm_navigation_msgs/AllowedCollisionEntry 90d1ae1850840724bb043562fe3285fc
arm_navigation_msgs/AllowedCollisionMatrix c5785d58d2d0b6270738f65222dbec5d
arm_navigation_msgs/AllowedContactSpecification 81f9b47ac49a467ae008d3d9485628a3
arm_navigation_msgs/ArmNavigationErrorCodes 5acf26755415e1ec18a6d523028f204d
arm_navigation_msgs/AttachedCollisionObject 3fd8ca730863e3d97d109c317d106cf9
arm_navigation_msgs/CollisionMap 18ca54db41ccebbe82f61f9801dede89
arm_navigation_msgs/CollisionObject 7b972910c23ece1b873b3de0cf92ba97
arm_navigation_msgs/CollisionObjectOperation 66a2b3b971d193145f8da8c3e401a474
arm_navigation_msgs/CollisionOperation e0cf3073b26bd86266c918a0c779f8a2
arm_navigation_msgs/Constraints fe6b6f09c687fd46c05a2de4ca18378a
arm_navigation_msgs/ContactInformation 85f341c0166ad4e68b4421391bbd2e15
arm_navigation_msgs/DisplayTrajectory 382f217803665e4718c4edbac445582c
arm_navigation_msgs/JointConstraint c02a15146bec0ce13564807805b008f0
arm_navigation_msgs/JointLimits 8ca618c7329ea46142cbc864a2efe856
arm_navigation_msgs/JointTrajectoryWithLimits e31e1ba1b3409bbb645c8dfcca5935cd
arm_navigation_msgs/LinkPadding b3ea75670df55c696fedee97774d5947
arm_navigation_msgs/MakeStaticCollisionMapAction aa13998383a2996b29b6fe4862547a17
arm_navigation_msgs/MakeStaticCollisionMapActionFeedback aae20e09065c3809e8a8e87c4c8953fd
arm_navigation_msgs/MakeStaticCollisionMapActionGoal 9233244d249847c9ee000cb0fccbaf8e
arm_navigation_msgs/MakeStaticCollisionMapActionResult 1eb06eeff08fa7ea874431638cb52332
arm_navigation_msgs/MakeStaticCollisionMapFeedback d41d8cd98f00b204e9800998ecf8427e
arm_navigation_msgs/MakeStaticCollisionMapGoal 43564281ea7e3c1ca0f01095edc909f7
arm_navigation_msgs/MakeStaticCollisionMapResult d41d8cd98f00b204e9800998ecf8427e
arm_navigation_msgs/MotionPlanRequest 75408e881303c6ad5069bd5df65ecb00
arm_navigation_msgs/MoveArmAction 6a991a3116cabdf4675f6b122822116b
arm_navigation_msgs/MoveArmActionFeedback 50284463bfe759fbf589fc263537baad
arm_navigation_msgs/MoveArmActionGoal d560cc046d6b5e8bf3f70a960054d6c6
arm_navigation_msgs/MoveArmActionResult 3e2bd2d3bd64d9942c0ef04de381c628
arm_navigation_msgs/MoveArmFeedback 321f3feadd0d5c1b7d7135738e673560
arm_navigation_msgs/MoveArmGoal 229373059043ad35d3ceeb2161f005d6
arm_navigation_msgs/MoveArmResult 3229301226a0605e3ffc9dfdaeac662f
arm_navigation_msgs/MoveArmStatistics d83dee1348791a0d1414257b41bc161f
arm_navigation_msgs/MultiDOFJointState ddd04f13c06870db031db8d5c0a6379d
arm_navigation_msgs/MultiDOFJointTrajectory 524f128fb0a65e2838b0e3e3f75207d0
arm_navigation_msgs/MultiDOFJointTrajectoryPoint 9be3ee3b5fa289b5394ab4ca9e54fa4e
arm_navigation_msgs/OrderedCollisionOperations f171f973b185d4d0121795080114026a
arm_navigation_msgs/OrientationConstraint 27d99749ba49d4a822298bbd1e0988ba
arm_navigation_msgs/OrientedBoundingBox a9b13162620bd04a7cb84cf207e7a8ac
arm_navigation_msgs/PlanningScene 6d1add8ef6efdd62d194ef430abd4b75
arm_navigation_msgs/PositionConstraint 7e3d9697e64b346b9d3cb7311bb88ccb
arm_navigation_msgs/RobotState 970d46b2ca41b9686adbdaeb592d97a7
arm_navigation_msgs/RobotTrajectory 5bc8324620001e5c07a09d0bbfaaf093
arm_navigation_msgs/Shape 59935940044147de75e7523b5d37c4d7
arm_navigation_msgs/SimplePoseConstraint 3483d830eb84ecd3059741fd417b30da
arm_navigation_msgs/SyncPlanningSceneAction 98a7de8683022cf0184b72a226932f22
arm_navigation_msgs/SyncPlanningSceneActionFeedback 3c839b650088826440a3debf312b0464
arm_navigation_msgs/SyncPlanningSceneActionGoal d841beb927266bf620ac574e2b28ec55
arm_navigation_msgs/SyncPlanningSceneActionResult b05528ce2d57b2b18c666f6eabecb171
arm_navigation_msgs/SyncPlanningSceneFeedback 5470cffcd2540df5b10d2ed9ddfde7e4
arm_navigation_msgs/SyncPlanningSceneGoal 285525c9abe002fbafa99af84a14b4cb
arm_navigation_msgs/SyncPlanningSceneResult 6f6da3883749771fac40d6deb24a8c02
arm_navigation_msgs/VisibilityConstraint ab297b6588ea21c1a862067d8447cb08
arm_navigation_msgs/WorkspaceParameters 1487490edff0df276863abf2cf221de5
base_local_planner/Position2DInt 3b834ede922a0fff22c43585c533b49f
bond/Constants 6fc594dc1d7bd7919077042712f8c8b0
bond/Status eacc84bf5d65b6777d4c50f463dfb9c8
control_msgs/FollowJointTrajectoryAction a1222b69ec4dcd1675e990ca2f8fe9be
control_msgs/FollowJointTrajectoryActionFeedback 868e86353778bcb1c5689adaa01a40d7
control_msgs/FollowJointTrajectoryActionGoal 8f3e00277a7b5b7c60e1ac5be35ddfa2
control_msgs/FollowJointTrajectoryActionResult bce83d50f7bb28226801436caf0e2043
control_msgs/FollowJointTrajectoryFeedback b11d532a92ee589417fdd76559eb1d9e
control_msgs/FollowJointTrajectoryGoal 01f6d702507b59bae3fc1e7149e6210c
control_msgs/FollowJointTrajectoryResult 6243274b5d629dc838814109754410d5
control_msgs/JointTolerance f544fe9c16cf04547e135dd6063ff5be
control_msgs/PointHeadActionAction 6a07e5bfef9eb077d70c657418c28471
control_msgs/PointHeadActionActionFeedback aae20e09065c3809e8a8e87c4c8953fd
control_msgs/PointHeadActionActionGoal b53a8323d0ba7b310ba17a2d3a82a6b8
control_msgs/PointHeadActionActionResult 1eb06eeff08fa7ea874431638cb52332
control_msgs/PointHeadActionFeedback d41d8cd98f00b204e9800998ecf8427e
control_msgs/PointHeadActionGoal 8b92b1cd5e06c8a94c917dc3209a4c1d
control_msgs/PointHeadActionResult d41d8cd98f00b204e9800998ecf8427e
costmap_2d/VoxelGrid
custom_msgs/arrayofdeez
custom_msgs/ptz
custom_msgs/robotMortality
custom_msgs/servosPos
diagnostic_msgs/DiagnosticArray 3cfbeff055e708a24c3d946a5c8139cd
diagnostic_msgs/DiagnosticStatus 67d15a62edb26e9d52b0f0efa3ef9da7
diagnostic_msgs/KeyValue cf57fdc6617a881a88c16e768132149c
driver_base/ConfigString bc6ccc4a57f61779c8eaae61e9f422e0
driver_base/ConfigValue d8512f27253c0f65f928a67c329cd658
driver_base/SensorLevels 6322637bee96d5489db6e2127c47602c
dynamic_reconfigure/BoolParameter 23f05028c1a699fb83e22401228c3a9e
dynamic_reconfigure/Config 958f16a05573709014982821e6822580
dynamic_reconfigure/ConfigDescription 757ce9d44ba8ddd801bb30bc456f946f
dynamic_reconfigure/DoubleParameter d8512f27253c0f65f928a67c329cd658
dynamic_reconfigure/Group 9e8cd9e9423c94823db3614dd8b1cf7a
dynamic_reconfigure/GroupState a2d87f51dc22930325041a2f8b1571f8
dynamic_reconfigure/IntParameter 65fedc7a0cbfb8db035e46194a350bf1
dynamic_reconfigure/ParamDescription 7434fcb9348c13054e0c3b267c8cb34d
dynamic_reconfigure/SensorLevels 6322637bee96d5489db6e2127c47602c
dynamic_reconfigure/StrParameter bc6ccc4a57f61779c8eaae61e9f422e0
dynamixel_msgs/JointState 2b8449320cde76616338e2539db27c32
dynamixel_msgs/MotorState 1cefdc3ff0c7d52e475886024476b74d
dynamixel_msgs/MotorStateList 9e94ccf6563ca78afce19eb097f9343c
experiment/SimonSays d62913c52a48d1ff62fdc6f5baa1a2e2
gazebo/ContactState 7f688fc24d90d16872fdc9ea8c6e45ab
gazebo/ContactsState 9d29ce6da289d3d303cc64b4cfdd0e84
gazebo/LinkState 0818ebbf28ce3a08d48ab1eaa7309ebe
gazebo/LinkStates 48c080191eb15c41858319b4d8a609c2
gazebo/ModelState 9330fd35f2fcd82d457e54bd54e10593
gazebo/ModelStates 48c080191eb15c41858319b4d8a609c2
gazebo/ODEJointProperties 1b744c32a920af979f53afe2f9c3511f
gazebo/ODEPhysics cecbd7ae4dd73d0ce24e775cee96d4a6
gazebo/WorldState de1a9de3ab7ba97ac0e9ec01a4eb481e
gazebo_msgs/ContactState 48c0ffb054b8c444f870cecea1ee50d9
gazebo_msgs/ContactsState acbcb1601a8e525bf72509f18e6f668d
gazebo_msgs/LinkState 0818ebbf28ce3a08d48ab1eaa7309ebe
gazebo_msgs/LinkStates 48c080191eb15c41858319b4d8a609c2
gazebo_msgs/ModelState 9330fd35f2fcd82d457e54bd54e10593
gazebo_msgs/ModelStates 48c080191eb15c41858319b4d8a609c2
gazebo_msgs/ODEJointProperties 1b744c32a920af979f53afe2f9c3511f
gazebo_msgs/ODEPhysics 667d56ddbd547918c32d1934503dc335
gazebo_msgs/WorldState de1a9de3ab7ba97ac0e9ec01a4eb481e
gazebo_plugins/ContactsState acbcb1601a8e525bf72509f18e6f668d
geometry_msgs/Point 4a842b65f413084dc2b10fb484ea7f17
geometry_msgs/Point32 cc153912f1453b708d221682bc23d9ac
geometry_msgs/PointStamped c63aecb41bfdfd6b7e1fac37c7cbe7bf
geometry_msgs/Polygon cd60a26494a087f577976f0329fa120e
geometry_msgs/PolygonStamped c6be8f7dc3bee7fe9e8d296070f53340
geometry_msgs/Pose e45d45a5a1ce597b249e23fb30fc871f
geometry_msgs/Pose2D 938fa65709584ad8e77d238529be13b8
geometry_msgs/PoseArray 916c28c5764443f268b296bb671b9d97
geometry_msgs/PoseStamped d3812c3cbc69362b77dc0b19b345f8f5
geometry_msgs/PoseWithCovariance c23e848cf1b7533a8d7c259073a97e6f
geometry_msgs/PoseWithCovarianceStamped 953b798c0f514ff060a53a3498ce6246
geometry_msgs/Quaternion a779879fadf0160734f906b8c19c7004
geometry_msgs/QuaternionStamped e57f1e547e0e1fd13504588ffc8334e2
geometry_msgs/Transform ac9eff44abf714214112b05d54a3cf9b
geometry_msgs/TransformStamped b5764a33bfeb3588febc2682852579b0
geometry_msgs/Twist 9f195f881246fdfa2798d1d3eebca84a
geometry_msgs/TwistStamped 98d34b0043a2093cf9d9345ab6eef12e
geometry_msgs/TwistWithCovariance 1fe8a28e6890a4cc3ae4c3ca5c7d82e6
geometry_msgs/TwistWithCovarianceStamped 8927a1a12fb2607ceea095b2dc440a96
geometry_msgs/Vector3 4a842b65f413084dc2b10fb484ea7f17
geometry_msgs/Vector3Stamped 7b324c7325e683bf02a9b14b01090ec7
geometry_msgs/Wrench 4f539cf138b23283b520fd271b567936
geometry_msgs/WrenchStamped d78d3cb249ce23087ade7e7d0c40cfa7
kinematics_msgs/KinematicSolverInfo cc048557c0f9795c392dd80f8bb00489
kinematics_msgs/PositionIKRequest 737bb756c6253bdd460b1383d0b12dac
move_base_msgs/MoveBaseAction 70b6aca7c7f7746d8d1609ad94c80bb8
move_base_msgs/MoveBaseActionFeedback 7d1870ff6e0decea702b943b5af0b42e
move_base_msgs/MoveBaseActionGoal 660d6895a1b9a16dce51fbdd9a64a56b
move_base_msgs/MoveBaseActionResult 1eb06eeff08fa7ea874431638cb52332
move_base_msgs/MoveBaseFeedback 3fb824c456a757373a226f6d08071bf0
move_base_msgs/MoveBaseGoal 257d089627d7eb7136c24d3593d05a16
move_base_msgs/MoveBaseResult d41d8cd98f00b204e9800998ecf8427e
nav_msgs/GridCells b9e4f5df6d28e272ebde00a3994830f5
nav_msgs/MapMetaData 10cfc8a2818024d3248802c00c95f11b
nav_msgs/OccupancyGrid 3381f2d731d4076ec5c71b0759edbe4e
nav_msgs/Odometry cd5e73d190d741a2f92e81eda573aca7
nav_msgs/Path 6227e2b7e9cce15051f669a5e197bbf7
ompl_ros_interface/OmplPlannerDiagnostics 5b3711264bf69e94abcd2caafc0c541d
pcl/ModelCoefficients
pcl/PointIndices
pcl/PolygonMesh
pcl/Vertices
perf_roscpp/LatencyMessage be90d117303321f392404ed7edeb7c8c
perf_roscpp/ThroughputMessage dda33390139e301b6c212139192418ca
pr2_msgs/AccelerometerState 26492e97ed8c13252c4a85592d3e93fd
pr2_msgs/AccessPoint 810217d9e35df31ffb396ea5673d7d1b
pr2_msgs/BatteryServer 4f6d6e54c9581beb1df7ea408c0727be
pr2_msgs/BatteryServer2 5f2cec7d06c312d756189db96c1f3819
pr2_msgs/BatteryState 00e9f996c2fc26700fd25abcd8422db0
pr2_msgs/BatteryState2 91b4acb000aa990ac3006834f9a99669
pr2_msgs/DashboardState db0cd0d535d75e0f6257b20c403e87f5
pr2_msgs/GPUStatus 4c74e5474b8aade04e56108262099c6e
pr2_msgs/LaserScannerSignal 78f41e618127bce049dd6104d9c31dc5
pr2_msgs/LaserTrajCmd 68a1665e9079049dce55a0384cb2e9b5
pr2_msgs/PeriodicCmd 95ab7e548e3d4274f83393129dd96c2e
pr2_msgs/PowerBoardState 08899b671e6a1a449e7ce0000da8ae7b
pr2_msgs/PowerState e6fa46a387cad0b7a80959a21587a6c9
pr2_msgs/PressureState 756fb3b75fa8884524fd0789a78eb04b
rlucid_msgs/collisionMsg
rlucid_msgs/totalMsg
rosapi/TypeDef bd8529b0edb168fde8dd58032743f1f7
rosbridge_test/CharTest 7b8d15902c8b049d5a32b4cb73fa86f5
rosbridge_test/DurationArrayTest 8b3bcadc803a7fcbc857c6a1dab53bcd
rosbridge_test/HeaderArrayTest d7be0bb39af8fb9129d5a76e6b63a290
rosbridge_test/HeaderTest d7be0bb39af8fb9129d5a76e6b63a290
rosbridge_test/HeaderTestTwo d7be0bb39af8fb9129d5a76e6b63a290
rosbridge_test/TimeArrayTest 237b97d24fd33588beee4cd8978b149d
rosbridge_test/UInt8Test f43a8e1b362b75baa741461b46adc7e0
roscpp/Logger a6069a2ff40db7bd32143dd66e1f408e
rosgraph_msgs/Clock a9c97c1d230cfc112e270351a944ee47
rosgraph_msgs/Log acffd30cd6b6de30f120938c17c593fb
rospy_tutorials/Floats 420cd38b6b071cd49f2970c3e2cee511
rospy_tutorials/HeaderString c99a9440709e4d4a9716d55b8270d5e7
sensor_msgs/CameraInfo c9a58c1b0b154e0e6da7578cb991d214
sensor_msgs/ChannelFloat32 3d40139cdd33dfedcb71ffeeeb42ae7f
sensor_msgs/CompressedImage 8f7a12909da2c9d3332d540a0977563f
sensor_msgs/Image 060021388200f6f0f447d0fcd9c64743
sensor_msgs/Imu 6a62c6daae103f4ff57a132d6f95cec2
sensor_msgs/JointState 3066dcd76a6cfaef579bd0f34173e9fd
sensor_msgs/Joy 5a9ea5f83505693b71e785041e67a8bb
sensor_msgs/JoyFeedback f4dcd73460360d98f36e55ee7f2e46f1
sensor_msgs/JoyFeedbackArray cde5730a895b1fc4dee6f91b754b213d
sensor_msgs/LaserScan 90c7ef2dc6895d81024acba2ac42f369
sensor_msgs/NavSatFix 2d3a8cd499b9b4a0249fb98fd05cfa48
sensor_msgs/NavSatStatus 331cdbddfa4bc96ffc3b9ad98900a54c
sensor_msgs/PointCloud d8e9c3f5afbdd8a130fd1d2763945fca
sensor_msgs/PointCloud2 1158d486dd51d683ce2f1be655c3c181
sensor_msgs/PointField 268eacb2962780ceac86cbd17e328150
sensor_msgs/Range c005c34273dc426c67a020a87bc24148
sensor_msgs/RegionOfInterest bdb633039d588fcccb441a4d43ccfe09
sensor_msgs/TimeReference fded64a0265108ba86c3d38fb11c0c16
shape_msgs/Mesh 1ffdae9486cd3316a121c578b47a85cc
shape_msgs/MeshTriangle 23688b2e6d2de3d32fe8af104a903253
shape_msgs/Plane 2c1b92ed8f31492f8e73f6a4a44ca796
shape_msgs/SolidPrimitive d8f8cbc74c5ff283fca29569ccefb45d
smach_msgs/SmachContainerInitialStatusCmd 45f8cf31fc29b829db77f23001f788d6
smach_msgs/SmachContainerStatus 5ba2bb79ac19e3842d562a191f2a675b
smach_msgs/SmachContainerStructure 3d3d1e0d0f99779ee9e58101a5dcf7ea
spline_smoother/LSPBSplineCoefficients c00dc8b55f1156bf5a7d2645875397b1
spline_smoother/LSPBTrajectoryMsg ce1139991f603c3d37b77cd9b60c5c3d
spline_smoother/LSPBTrajectorySegmentMsg 53054857ee1d2a19ca83edc07b14eef2
spline_smoother/SplineCoefficients c4e5d982f9108827e742320d3c247546
spline_smoother/SplineTrajectory 45d783dc5c58ac7ae093c93ba1b8d451
spline_smoother/SplineTrajectorySegment 1c95257e91547459aede67dd02a209d6
std_msgs/Bool 8b94c1b53db61fb6aed406028ad6332a
std_msgs/Byte ad736a2e8818154c487bb80fe42ce43b
std_msgs/ByteMultiArray 70ea476cbcfd65ac2f68f3cda1e891fe
std_msgs/Char 1bf77f25acecdedba0e224b162199717
std_msgs/ColorRGBA a29a96539573343b1310c73607334b00
std_msgs/Duration 3e286caf4241d664e55f3ad380e2ae46
std_msgs/Empty d41d8cd98f00b204e9800998ecf8427e
std_msgs/Float32 73fcbf46b49191e672908e50842a83d4
std_msgs/Float32MultiArray 6a40e0ffa6a17a503ac3f8616991b1f6
std_msgs/Float64 fdb28210bfa9d7c91146260178d9a584
std_msgs/Float64MultiArray 4b7d974086d4060e7db4613a7e6c3ba4
std_msgs/Header 2176decaecbce78abc3b96ef049fabed
std_msgs/Int16 8524586e34fbd7cb1c08c5f5f1ca0e57
std_msgs/Int16MultiArray d9338d7f523fcb692fae9d0a0e9f067c
std_msgs/Int32 da5909fbe378aeaf85e547e830cc1bb7
std_msgs/Int32MultiArray 1d99f79f8b325b44fee908053e9c945b
std_msgs/Int64 34add168574510e6e17f5d23ecc077ef
std_msgs/Int64MultiArray 54865aa6c65be0448113a2afc6a49270
std_msgs/Int8 27ffa0c9c4b8fb8492252bcad9e5c57b
std_msgs/Int8MultiArray d7c1af35a1b4781bbe79e03dd94b7c13
std_msgs/MultiArrayDimension 4cd0c83a8683deae40ecdac60e53bfa8
std_msgs/MultiArrayLayout 0fed2a11c13e11c5571b4e2a995a91a3
std_msgs/String 992ce8a1687cec8c8bd883ec73ca41d1
std_msgs/Time cd7166c74c552c311fbcc2fe5a7bc289
std_msgs/UInt16 1df79edf208b629fe6b81923a544552d
std_msgs/UInt16MultiArray 52f264f1c973c4b73790d384c6cb4484
std_msgs/UInt32 304a39449588c7f8ce2df6e8001c5fce
std_msgs/UInt32MultiArray 4d6a180abc9be191b96a7eda6c8a233d
std_msgs/UInt64 1b2a79973e8bf53d7b53acb71299cb57
std_msgs/UInt64MultiArray 6088f127afb1d6c72927aa1247e945af
std_msgs/UInt8 7c8164229e7d2c17eb95e9231617fdee
std_msgs/UInt8MultiArray 82373f1612381bb6ee473b5cd6f5d89c
stereo_msgs/DisparityImage 04a177815f75271039fa21f16acad8c9
test_crosspackage/TestMsgRename 796739ec6543fa03d2eb39f3aacaa2c0
test_crosspackage/TestSubMsgRename cb68fa50979d62961325d38238e6074d
test_ros/Arrays c5a1f18379b10bdd4df210944f6007a4
test_ros/Composite d8fb6eb869ad3956b50e8737d96dc9fa
test_ros/CompositeA a779879fadf0160734f906b8c19c7004
test_ros/CompositeB 4a842b65f413084dc2b10fb484ea7f17
test_ros/Embed 6dec891298f3675c2d964c161d28efaa
test_ros/Floats 1ee8aba2d870f75f2b5916e2cddbd928
test_ros/RosmsgA 5c9fb1a886e81e3162a5c87bf55c072b
test_ros/RosmsgB 6aac6c697d5414bc0fcede8c33981d0e
test_ros/RosmsgC cc91a7e3c1498150f3554cbcab2800d2
test_ros/Simple c9940b1313e61fed87cd22c50742578f
test_ros/TVals ae4d4f9600b9ab683a5dc9372c031758
test_ros/TestArrays 4cc9b5e2cebe791aa3e994f5bc159eb6
test_ros/TestHeader 4b5a00f536da2f756ba6aebcf795a967
test_ros/TestPrimitives 3e70f428a22c0d26ca67f87802c8e00f
test_ros/TestString 334ff4377be93faa44ebc66d23d40fd3
test_roscpp/TestArray f7c2f87680985b118316f81f28b4cfd5
test_roscpp/TestEmpty d41d8cd98f00b204e9800998ecf8427e
test_roscpp/TestStringInt 2f0ceb8aa4bbf4dbd240039d0bf240ca
test_roscpp/TestWithHeader d7be0bb39af8fb9129d5a76e6b63a290
test_roscpp_serialization/ArrayOfFixedLength 770e15962592d1fbea70b6b820ba16d9
test_roscpp_serialization/ArrayOfVariableLength e7404d454a50b82c426a3a2c76fbcefd
test_roscpp_serialization/Constants 0032309c8dd2c569e0e0d0e75974e750
test_roscpp_serialization/CustomHeader a5233fa4f3f6e00d1d85da1779d19d1e
test_roscpp_serialization/EmbeddedExternal a09b324865f98bbf605e59ed0cefbc1d
test_roscpp_serialization/EmbeddedFixedLength 770e15962592d1fbea70b6b820ba16d9
test_roscpp_serialization/EmbeddedVariableLength e7404d454a50b82c426a3a2c76fbcefd
test_roscpp_serialization/FixedLength 74143e1090cf694294f589605908b555
test_roscpp_serialization/FixedLengthArrayOfExternal cc431047757f431ecd2754e03aa592f8
test_roscpp_serialization/FixedLengthStringArray 8e291ac046196a95bbe34c1555b382cc
test_roscpp_serialization/HeaderNotFirstMember c7ed781820d7a36c5947d0f441f50964
test_roscpp_serialization/VariableLength d9a532f93b9aeffe09e3bc21ff3de0f0
test_roscpp_serialization/VariableLengthArrayOfExternal cc431047757f431ecd2754e03aa592f8
test_roscpp_serialization/VariableLengthStringArray fa992b5cca963995275d2a2f3ee7615f
test_roscpp_serialization/WithDuration 7ad52ba9d0229ba8b9c032400c77c367
test_roscpp_serialization/WithHeader ea0b9ad283b0d4dcc850b560da7b6965
test_roscpp_serialization/WithMemberNamedHeaderThatIsNotAHeader 59a71bc6c7b0e5643fa9d213cdf4c63c
test_roscpp_serialization/WithTime 60f189e40cfeaefbc79e6cdbd1605364
test_roscpp_serialization_perf/ChannelFloat32 61c47e4621e471c885edb248b5dcafd5
test_roscpp_serialization_perf/Point32 cc153912f1453b708d221682bc23d9ac
test_roscpp_serialization_perf/PointCloud c47b5cedd2b77d241b27547ed7624840
test_roslib_comm/ArrayOfMsgs 1f4cf3ffabe2555a0cfe19c974f01a01
test_roslib_comm/FieldNameChange1 31a9992534c4d020bfc4045e7dc1a786
test_roslib_comm/FieldNameChange2 dde34a89b93706fc183fd13c24ae090a
test_roslib_comm/FillEmbedTime 90e08039be001a899b8c20e680c289b0
test_roslib_comm/FillSimple da04a60d03fa22f7d301f9bd5f9a08ab
test_roslib_comm/HeaderTest 4426b8931bec8509041d64c6a89b54a2
test_roslib_comm/SameSubMsg1 49145a54e4be1a218629e518575a0bf3
test_roslib_comm/SameSubMsg2 49145a54e4be1a218629e518575a0bf3
test_roslib_comm/SameSubMsg3 49145a54e4be1a218629e518575a0bf3
test_roslib_comm/TypeNameChange1 31a9992534c4d020bfc4045e7dc1a786
test_roslib_comm/TypeNameChange2 31a9992534c4d020bfc4045e7dc1a786
test_roslib_comm/TypeNameChangeArray1 31a9992534c4d020bfc4045e7dc1a786
test_roslib_comm/TypeNameChangeArray2 31a9992534c4d020bfc4045e7dc1a786
test_roslib_comm/TypeNameChangeComplex1 a01688a0991b9d7d9facf6d94b993e93
test_roslib_comm/TypeNameChangeComplex2 a01688a0991b9d7d9facf6d94b993e93
test_rospy/ArrayVal 94e095e6a59bceb3466e4b23c166732e
test_rospy/EmbedTest f8b1fc6a0f70f541c9d6bd2886b9e249
test_rospy/Floats 420cd38b6b071cd49f2970c3e2cee511
test_rospy/HeaderHeaderVal ae71c365b9bafbc4abaf37150c80a6b5
test_rospy/HeaderVal c3262d64205f82361bc7aa4173b8fe64
test_rospy/PythonKeyword 1330d6bbfad8e75334346fec949d5133
test_rospy/TestConstants e60959d2ccf9718dc5e42767bebd1644
test_rospy/TestFixedArray 1557473dc09f1a01a00123a713c822a7
test_rospy/TransitiveImport 27665539bacd6d2d02a7538692d3d3d0
test_rospy/TransitiveMsg1 72751523a989ee2c7a44c006464315e9
test_rospy/TransitiveMsg2 eb1fa3c8b51b0e31f74e89c2eecc441e
test_rospy/Val c45577ce53559408f0f69fe465be7c70
tf/tfMessage 94810edda583a504dfda3829e70d7eec
tf2_msgs/LookupTransformAction 7ee01ba91a56c2245c610992dbaa3c37
tf2_msgs/LookupTransformActionFeedback aae20e09065c3809e8a8e87c4c8953fd
tf2_msgs/LookupTransformActionGoal f2e7bcdb75c847978d0351a13e699da5
tf2_msgs/LookupTransformActionResult ac26ce75a41384fa8bb4dc10f491ab90
tf2_msgs/LookupTransformFeedback d41d8cd98f00b204e9800998ecf8427e
tf2_msgs/LookupTransformGoal 35e3720468131d675a18bb6f3e5f22f8
tf2_msgs/LookupTransformResult 3fe5db6a19ca9cfb675418c5ad875c36
tf2_msgs/TF2Error bc6848fd6fd750c92e38575618a4917d
tf2_msgs/TFMessage 94810edda583a504dfda3829e70d7eec
theora_image_transport/Packet 33ac4e14a7cff32e7e0d65f18bb410f3
trajectory_msgs/JointTrajectory 72214029c6fba47b2135714577dd745e
trajectory_msgs/JointTrajectoryPoint 84fd2dcf68773c3dc0e9db894f4e8b40
turtle_actionlib/ShapeAction d73b17d6237a925511f5d7727a1dc903
turtle_actionlib/ShapeActionFeedback aae20e09065c3809e8a8e87c4c8953fd
turtle_actionlib/ShapeActionGoal dbfccd187f2ec9c593916447ffd6cc77
turtle_actionlib/ShapeActionResult c8d13d5d140f1047a2e4d3bf5c045822
turtle_actionlib/ShapeFeedback d41d8cd98f00b204e9800998ecf8427e
turtle_actionlib/ShapeGoal 3b9202ab7292cebe5a95ab2bf6b9c091
turtle_actionlib/ShapeResult b06c6e2225f820dbc644270387cd1a7c
turtlesim/Color 353891e354491c51aabe32df673fb446
turtlesim/Pose 863b248d5016ca62ea2e895ae5265cf9
turtlesim/Velocity 9d5c2dcd348ac8f76ce2a4307bd63a13
visualization_msgs/ImageMarker 1de93c67ec8858b831025a08fbf1b35c
visualization_msgs/InteractiveMarker 97d10a9c6371692b469f5814d4156b68
visualization_msgs/InteractiveMarkerControl f69c49e4eb251b5b0a89651eebf5a277
visualization_msgs/InteractiveMarkerFeedback ab0f1eee058667e28c19ff3ffc3f4b78
visualization_msgs/InteractiveMarkerInit 467a93e72a544fd966aafe47c72160b7
visualization_msgs/InteractiveMarkerPose a6e6833209a196a38d798dadb02c81f8
visualization_msgs/InteractiveMarkerUpdate f9687b34e042d26cfd564728fc644bff
visualization_msgs/Marker 18326976df9d29249efc939e00342cde
visualization_msgs/MarkerArray 90da67007c26525f655c1c269094e39f
visualization_msgs/MenuEntry b90ec63024573de83b57aa93eb39be2d
wiimote/IrSourceInfo 95274ca88b9f008b99984b9a61d2772e
wiimote/State a69651e8129655c6ed3c5039e468362c
wiimote/TimedSwitch e4c8d9327409cef6066fa6c368032c1e";
#endregion

        private Dictionary<string, string> sums = new Dictionary<string, string>();

        public MD5Test()
        {
            string[] lines = fuerteSums.Replace("\r","").Split('\n');
            foreach (string s in lines)
            {
                string[] pieces = s.Split(' ');
                if (pieces.Length == 2)
                {
                    sums[pieces[0]] = pieces[1];
                }
            }
        }

        [TestMethod]
        public void TestMethod1()
        {
            List<MsgTypes> failures = new List<MsgTypes>();
            foreach (MsgTypes m in Enum.GetValues(typeof(MsgTypes)))
            {
                if (m == MsgTypes.Unknown) continue;
                IRosMessage msg = IRosMessage.generate(m);
                string type = msg.GetType().FullName.Replace("Messages.", "").Replace(".", "/");
                if (!sums.ContainsKey(type)) continue;
                string desiredSum = sums[type];
                string actualSum = msg.MD5Sum();
                bool eq = String.Equals(desiredSum,actualSum);
                Debug.WriteLine("{0}\t{1}", type, eq?"OK":"FAIL");
                if (!eq)
                    failures.Add(m);
            }
            Assert.IsFalse(failures.Any());
        }
    }
}
