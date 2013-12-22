using System;
using System.Reflection;
using DNLibs;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;


[assembly: Addin("DNMentorModule", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace DNMentors
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
    class MentorModule : ISharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // Loaded Config Data  with defaults
        private bool Enabled = true;
        private int CommandChan = 7799;

//        string FromUUID = "fd43af94-7120-44ae-f5b7-24acd1c66142";
        string FromName  = "Mentor Alert";
        string GroupUUID = "0e5f89bb-eb25-4b5d-b792-e77f7a383450";

        // Some varable to be set up 
        IDialogModule m_dialogMod;
        Scene m_scene;

        #region ISharedRegion implementation

        public string Name
        {
            get { return "DNMentorModule"; }
        }

        public void Initialise(IConfigSource config)
        {
            IConfig cfg = config.Configs["Mentor"];

            if (null == cfg)
                return;

            Enabled = cfg.GetBoolean("enabled", false);
            CommandChan = cfg.GetInt("commandchan", 7799);
            GroupUUID = cfg.GetString("groupuuid", "");

            if (GroupUUID == "") Enabled = false;
          

            if (!Enabled)
                return;

        }

        public void PostInitialise() { }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            if (!Enabled)
                return;

//            SceneHandler.Instance.AddScene(scene);

//            scene.AddCommand(this, "OMBaseTest", "Test Open Metaverse Economy Connection", "Test Open Metaverse Economy Connection", testConnection);
//            scene.AddCommand(this, "OMRegister", "Registers the Metaverse Economy Module", "Registers the Metaverse Economy Module", registerModule);

        }

        public void RegionLoaded(Scene scene)
        {
            if (!Enabled)
                return;
            m_scene = scene;
            scene.EventManager.OnMakeRootAgent += new Action<ScenePresence>(EventManager_OnMakeRootAgent);
            scene.EventManager.OnClientClosed += new EventManager.ClientClosed(EventManager_OnClientClosed);
            scene.EventManager.OnChatFromClient += new EventManager.ChatFromClientEvent(EventManager_OnChatFromClient);
            m_dialogMod = scene.RequestModuleInterface<IDialogModule>();
        }


        public void RemoveRegion(Scene scene)
        {
            if (!Enabled)
                return;

            scene.EventManager.OnMakeRootAgent -= EventManager_OnMakeRootAgent;
            scene.EventManager.OnClientClosed -= EventManager_OnClientClosed;

        }

        public void Close()
        {
            if (Enabled)
            {
            }
        }

        #endregion



        #region Events
        void EventManager_OnMakeRootAgent(ScenePresence sp)
        {

            IClientAPI client = SceneHandler.Instance.LocateClientObject(sp.UUID);
            Scene currentScene = SceneHandler.Instance.LocateSceneClientIn(sp.UUID);

            DateTime Born = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime LastOn = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            

            IUserAccountService UAS = sp.Scene.RequestModuleInterface<IUserAccountService>();
            if (UAS == null)
            {
                m_log.ErrorFormat("[Mentor]: Get USer Account Service Failed");
            }
            UserAccount UA = UAS.GetUserAccount(sp.Scene.RegionInfo.ScopeID, sp.UUID);
            if (UA == null)
            {
                m_log.ErrorFormat("[Mentor]: Get User Account Failed");
            }
            else
            {
                Born = Born.AddSeconds(UA.Created);
                
            }
            m_log.InfoFormat("[MENTOR] Enter Sim: {0}({3})-{1}   {2}", sp.Name, sp.Grouptitle, sp.Viewer, Born.ToString("yyyy-MM-dd") );
            TimeSpan AGE = DateTime.Now - Born;
            if (AGE.Days < 32)
            {
                IGroupsMessagingModule GIM = sp.Scene.RequestModuleInterface<IGroupsMessagingModule>();
                if (GIM == null) m_log.Debug("[MENTOR] No group Interface?");
                else
                {
                    if (GIM.StartGroupChatSession(UUID.Random(), UUID.Parse(GroupUUID)))
                    {
                        
                        string sGM = string.Format("{0} Has entered region: {1}  Born: {2}({3} days)",
                                                                sp.Name,
                                                                sp.Scene.RegionInfo.RegionName,
                                                                Born.ToString("yyyy-MM-dd"),
                                                                AGE.Days);
                        m_log.DebugFormat("[MENTOR] {0}", sGM);
                        GIM.SendMessageToGroup(new GridInstantMessage(null, sp.UUID, FromName, UUID.Parse(GroupUUID), (byte)InstantMessageDialog.SessionSend, sGM, false, Vector3.Zero), UUID.Parse(GroupUUID));
                    }
                    else m_log.Error("[Mentor] Group Session Failed");
                }
            }

        }


        void EventManager_OnClientClosed(UUID clientID, Scene scene)
        {
            m_log.Info("[Mentor]: <Client Close>");

        }

        void EventManager_OnChatFromClient(object sender, OSChatMessage chat)
        {
            if((chat.Message != "") && (chat.Channel == CommandChan)) ProcessCommand( sender, chat);
            
        }

        #endregion


    



        

        void ProcessCommand( object Sender,OSChatMessage Chat)
        {
            ScenePresence sp = m_scene.GetScenePresence(Chat.SenderUUID);
            MessageDialog( sp.ControllingClient, String.Format( "Recieved: {1}:{0}", Chat.Message, sp.ControllingClient.Name ));
            string[] MessParts = Chat.Message.Split(default(char[]));
            m_log.DebugFormat("[MENTOR] Command recieved: {0}", MessParts[0]);
            switch (MessParts[0].ToLower())
            {

                case "alert":
                    switch (MessParts[1].ToLower())
                    {
                        case "on":
                            Enabled = true;
                            MessageDialog(sp.ControllingClient, "Mentor Alert is On");
                            break;

                        case "off":
                            Enabled = false;
                            MessageDialog(sp.ControllingClient, "Mentor Alert is Off");
                            break;
                        default:
                            if(Enabled) MessageDialog(sp.ControllingClient, "Mentor Alert is on");
                            else MessageDialog(sp.ControllingClient, "Mentor Alert is off");
                            break;

                    }
                    break;
                default:
                    
                    break;
            }


        }

        void MessageTest()
        {





        }

        void MessageDialog(IClientAPI Sender, string Message)
        {
            if (m_dialogMod != null)
            {
                m_dialogMod.SendAlertToUser(Sender, Message, false);
            }


        }

        void SendGroupMessage( IClientAPI NewUser, DateTime Born)
        {


        }

    }
}
