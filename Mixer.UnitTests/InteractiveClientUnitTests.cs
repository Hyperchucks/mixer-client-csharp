﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mixer.Base;
using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Client;
using Mixer.Base.Model.Interactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mixer.UnitTests
{
    [TestClass]
    public class InteractiveClientUnitTests : UnitTestBase
    {
        private const string GroupID = "MixerUnitTestGroup";
        private const string SceneID = "MixerUnitTestScene";
        private const string ButtonControlID = "MixerUnitTestButtonControl";
        private const string JoystickControlID = "MixerUnitTestJoystickControl";

        private static InteractiveGameListingModel testGameListing;
        private static InteractiveClient interactiveClient;

        public static InteractiveButtonControlModel CreateTestButton()
        {
            return new InteractiveButtonControlModel()
            {
                controlID = ButtonControlID,
                text = "I'm a button",
                cost = 0,
                disabled = false,
                position = new InteractiveControlPositionModel[]
                {
                    new InteractiveControlPositionModel()
                    {
                        size = "large",
                        width = 10,
                        height = 9,
                        x = 0,
                        y = 0
                    },
                    new InteractiveControlPositionModel()
                    {
                        size = "medium",
                        width = 10,
                        height = 3,
                        x = 0,
                        y = 0
                    },
                    new InteractiveControlPositionModel()
                    {
                        size = "small",
                        width = 10,
                        height = 3,
                        x = 0,
                        y = 0
                    }
                }
            };
        }

        public static InteractiveJoystickControlModel CreateTestJoystick()
        {
            return new InteractiveJoystickControlModel()
            {
                controlID = JoystickControlID,
                disabled = false,
                sampleRate = 50,
                position = new InteractiveControlPositionModel[]
                {
                    new InteractiveControlPositionModel()
                    {
                        size = "large",
                        width = 10,
                        height = 9,
                        x = 15,
                        y = 0
                    },
                    new InteractiveControlPositionModel()
                    {
                        size = "medium",
                        width = 10,
                        height = 3,
                        x = 15,
                        y = 0
                    },
                    new InteractiveControlPositionModel()
                    {
                        size = "small",
                        width = 10,
                        height = 3,
                        x = 15,
                        y = 0
                    }
                }
            };
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            TestWrapper(async (MixerConnection connection) =>
            {
                ChannelModel channel = await ChannelsServiceUnitTests.GetChannel(connection);

                testGameListing = await InteractiveServiceUnitTests.CreateTestGame(connection, channel);

                interactiveClient = await InteractiveClient.CreateFromChannel(connection, channel, testGameListing);

                Assert.IsTrue(await interactiveClient.Connect());
                Assert.IsTrue(await interactiveClient.Ready());
            });
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestWrapper(async (MixerConnection connection) =>
            {
                await interactiveClient.Disconnect();

                await InteractiveServiceUnitTests.DeleteTestGame(connection, testGameListing);
            });
        }

        [TestInitialize]
        public void TestInitialize()
        {
            this.ClearPackets();
        }

        [TestMethod]
        public void GetTime()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                DateTimeOffset? dateTime = await interactiveClient.GetTime();

                Assert.IsNotNull(dateTime);
                Assert.IsTrue(DateTimeOffset.Now.Year.Equals(dateTime.GetValueOrDefault().Year));
            });
        }

        [TestMethod]
        public void GetMemoryStates()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                InteractiveIssueMemoryWarningModel memoryWarning = await interactiveClient.GetMemoryStates();

                Assert.IsNotNull(memoryWarning);
                Assert.IsNotNull(memoryWarning.resources);
            });
        }

        [TestMethod]
        public void SetBandwidthThrottleAndGetThrottleState()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                InteractiveSetBandwidthThrottleModel bandwidthThrottle = new InteractiveSetBandwidthThrottleModel();
                bandwidthThrottle.AddThrottle("giveInput", 10000000, 3000000);

                bool result = await interactiveClient.SetBandwidthThrottleWithResponse(bandwidthThrottle);

                Assert.IsTrue(result);

                this.ClearPackets();

                InteractiveGetThrottleStateModel throttleState = await interactiveClient.GetThrottleState();

                Assert.IsNotNull(throttleState);
                Assert.IsTrue(throttleState.MethodThrottles.Count > 0);
            });
        }

        [TestMethod]
        public void GetAllParticipants()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                InteractiveParticipantCollectionModel participants = await interactiveClient.GetAllParticipants();

                Assert.IsNotNull(participants);
                Assert.IsNotNull(participants.participants);
            });
        }

        [TestMethod]
        public void GetActiveParticipants()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                DateTimeOffset dateTime = DateTimeOffset.Now;
                InteractiveParticipantCollectionModel participants = await interactiveClient.GetActiveParticipants(dateTime);

                Assert.IsNotNull(participants);
                Assert.IsNotNull(participants.participants);
            });
        }

        [TestMethod]
        public void UpdateParticipants()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                InteractiveParticipantCollectionModel participants = await interactiveClient.GetAllParticipants();

                Assert.IsNotNull(participants);
                Assert.IsNotNull(participants.participants);

                this.ClearPackets();

                participants = await interactiveClient.UpdateParticipantsWithResponse(participants.participants);

                Assert.IsNotNull(participants);
                Assert.IsNotNull(participants.participants);
            });
        }

        [TestMethod]
        public void CreateGetUpdateDeleteGroup()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                InteractiveConnectedSceneModel testScene = await this.CreateScene(interactiveClient);

                this.ClearPackets();

                InteractiveGroupModel testGroup = new InteractiveGroupModel()
                {
                    groupID = GroupID,
                    sceneID = testScene.sceneID
                };

                bool result = await interactiveClient.CreateGroupsWithResponse(new List<InteractiveGroupModel>() { testGroup });

                Assert.IsTrue(result);

                this.ClearPackets();

                InteractiveGroupCollectionModel groups = await interactiveClient.GetGroups();

                Assert.IsNotNull(groups);
                Assert.IsNotNull(groups.groups);
                Assert.IsTrue(groups.groups.Count > 0);

                testGroup = groups.groups.FirstOrDefault(g => g.groupID.Equals(GroupID));
                InteractiveGroupModel defaultGroup = groups.groups.FirstOrDefault(g => g.groupID.Equals("default"));

                this.ClearPackets();

                groups = await interactiveClient.UpdateGroupsWithResponse(new List<InteractiveGroupModel>() { testGroup });

                Assert.IsNotNull(groups);
                Assert.IsNotNull(groups.groups);
                Assert.IsTrue(groups.groups.Count > 0);

                testGroup = groups.groups.FirstOrDefault(g => g.groupID.Equals(GroupID));

                this.ClearPackets();

                result = await interactiveClient.DeleteGroupWithResponse(testGroup, defaultGroup);

                Assert.IsTrue(result);

                await this.DeleteScene(interactiveClient, testScene);
            });
        }

        [TestMethod]
        public void CreateGetUpdateDeleteScene()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                InteractiveConnectedSceneModel testScene = await this.CreateScene(interactiveClient);

                this.ClearPackets();

                InteractiveConnectedSceneCollectionModel scenes = await interactiveClient.UpdateScenesWithResponse(new List<InteractiveConnectedSceneModel>() { testScene });

                Assert.IsNotNull(scenes);
                Assert.IsNotNull(scenes.scenes);
                Assert.IsTrue(scenes.scenes.Count >= 1);

                testScene = scenes.scenes.FirstOrDefault(s => s.sceneID.Equals(SceneID));

                await this.DeleteScene(interactiveClient, testScene);
            });
        }

        [TestMethod]
        public void CreateUpdateDeleteControl()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                InteractiveConnectedSceneModel testScene = await this.CreateScene(interactiveClient);

                this.ClearPackets();

                InteractiveControlModel testControl = InteractiveClientUnitTests.CreateTestButton();

                List<InteractiveControlModel> controls = new List<InteractiveControlModel>() { testControl, InteractiveClientUnitTests.CreateTestJoystick() };
                bool result = await interactiveClient.CreateControlsWithResponse(testScene, controls);

                Assert.IsTrue(result);

                testScene = await this.GetScene(interactiveClient);
                testControl = testScene.buttons.FirstOrDefault(c => c.controlID.Equals(ButtonControlID));
                Assert.IsNotNull(testControl);

                controls = new List<InteractiveControlModel>() { testControl };
                InteractiveConnectedControlCollectionModel controlCollection = await interactiveClient.UpdateControlsWithResponse(testScene, controls);

                Assert.IsNotNull(controlCollection);
                Assert.IsNotNull(controlCollection.buttons);

                testScene = await this.GetScene(interactiveClient);
                testControl = testScene.buttons.FirstOrDefault(c => c.controlID.Equals(ButtonControlID));
                Assert.IsNotNull(testControl);

                result = await interactiveClient.DeleteControlsWithResponse(testScene, controls);

                Assert.IsTrue(result);

                await this.DeleteScene(interactiveClient, testScene);
            });
        }

        /// <summary>
        /// Not an effective unit test, as it requires a transaction to actually be sent for this to work
        /// </summary>
        [TestMethod]
        public void CaptureSparkTransaction()
        {
            this.InteractiveWrapper(async (MixerConnection connection, InteractiveClient interactiveClient) =>
            {
                this.ClearPackets();

                bool result = await interactiveClient.CaptureSparkTransactionWithResponse(Guid.Empty.ToString());

                Assert.IsTrue(result);
            });
        }

        private void InteractiveWrapper(Func<MixerConnection, InteractiveClient, Task> function)
        {
            TestWrapper(async (MixerConnection connection) =>
            {
                interactiveClient.OnReplyOccurred += InteractiveClient_OnReplyOccurred;
                interactiveClient.OnMethodOccurred += InteractiveClient_OnMethodOccurred;

                await function(connection, interactiveClient);

                interactiveClient.OnReplyOccurred -= InteractiveClient_OnReplyOccurred;
                interactiveClient.OnMethodOccurred -= InteractiveClient_OnMethodOccurred;
            });
        }

        private async Task<InteractiveConnectedSceneModel> CreateScene(InteractiveClient interactiveClient)
        {
            this.ClearPackets();

            InteractiveConnectedSceneCollectionModel scenes = await interactiveClient.CreateScenesWithResponse(new List<InteractiveConnectedSceneModel>() { new InteractiveConnectedSceneModel() { sceneID = SceneID } });

            Assert.IsNotNull(scenes);
            Assert.IsNotNull(scenes.scenes);
            Assert.IsTrue(scenes.scenes.Count >= 1);

            InteractiveConnectedSceneModel testScene = scenes.scenes.FirstOrDefault(s => s.sceneID.Equals(SceneID));
            Assert.IsNotNull(testScene);

            return await this.GetScene(interactiveClient);
        }

        private async Task<InteractiveConnectedSceneModel> GetScene(InteractiveClient interactiveClient)
        {
            this.ClearPackets();

            InteractiveConnectedSceneGroupCollectionModel scenes = await interactiveClient.GetScenes();

            Assert.IsNotNull(scenes);
            Assert.IsNotNull(scenes.scenes);
            Assert.IsTrue(scenes.scenes.Count >= 2);

            InteractiveConnectedSceneModel testScene = scenes.scenes.FirstOrDefault(s => s.sceneID.Equals(SceneID));
            Assert.IsNotNull(testScene);

            return testScene;
        }

        private async Task DeleteScene(InteractiveClient interactiveClient, InteractiveConnectedSceneModel scene)
        {
            this.ClearPackets();

            InteractiveConnectedSceneGroupCollectionModel scenes = await interactiveClient.GetScenes();

            Assert.IsNotNull(scenes);
            Assert.IsNotNull(scenes.scenes);
            Assert.IsTrue(scenes.scenes.Count >= 2);

            InteractiveConnectedSceneModel backupScene = scenes.scenes.FirstOrDefault(s => s.sceneID.Equals("default"));

            bool result = await interactiveClient.DeleteSceneWithResponse(scene, backupScene);

            Assert.IsTrue(result);
        }

        private void InteractiveClient_OnReplyOccurred(object sender, ReplyPacket e)
        {
            this.replyPackets.Add(e);
        }

        private void InteractiveClient_OnMethodOccurred(object sender, MethodPacket e)
        {
            this.methodPackets.Add(e);
        }
    }
}
