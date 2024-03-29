using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MistMod {
    /// <summary> Handler for client-side mod functionality </summary>
    public class ClientAllomancyHandler {

        MistModSystem System;
        ICoreClientAPI Capi;
        /// <summary> Channel for client-side networking </summary>
        public IClientNetworkChannel Channel { get; private set; }
        /// <summary> The handler currently in use </summary>
        public ClientAllomancyHandler Current;
        /// <summary> Helper for getting allomantic properties </summary>
        public AllomancyPropertyHelper AllomancyHelper;

        /// <summary> Construct a new allomancy handler </summary>
        /// <param name="capi"> The client api </summary>
        /// <param name="system"> The system api </summary>
        public ClientAllomancyHandler(ICoreClientAPI capi, MistModSystem system) {
            Capi = capi;
            System = system;
            Current = this;
        }

        private static SimpleParticleProperties motionParticles = new SimpleParticleProperties(
            1,
            1,
            ColorUtil.ColorFromRgba(0, 255, 255, 50),
            new Vec3d(),
            new Vec3d(),
            new Vec3f(0,0.2f,0),
            new Vec3f(0,0.2f,0)
        );



        GuiDialogMetalSelector metalSelector;

        float targetVignete;
        float targetNightvision;
        int previousTinStatus;
        

        /// <summary> Initialize the client handler. </summary> 
        public void Initialize () {
            // Register the networking channel.
            Channel = Capi.Network.RegisterChannel(MistModSystem.MOD_ID)
			    .RegisterMessageType(typeof(BurnMessage))
                .RegisterMessageType(typeof(SelectedMetalMessage))
                .RegisterMessageType(typeof(ReplaceAlloHelperEntity));

            Channel.SetMessageHandler<SelectedMetalMessage>(OnSelectedMetalMessage);
            Channel.SetMessageHandler<ReplaceAlloHelperEntity>(OnUpdateAlloHelper);

            // Hotkeys for burning metals.
            Capi.Input.RegisterHotKey("burn-metal-toggle", 
                "Toggle allomantic metal burn", 
                GlKeys.Z, 
                HotkeyType.CharacterControls,
                false,
                true,
                true);
            Capi.Input.RegisterHotKey(
                "burn-metal-inc", 
                "Increase allomantic metal burn", 
                GlKeys.Z, 
                HotkeyType.CharacterControls,
                false,
                false,
                true);
            Capi.Input.RegisterHotKey(
                "burn-metal-dec", 
                "Decrease allomantic metal burn",
                GlKeys.Z, 
                HotkeyType.CharacterControls,
                false,
                true,
                false);
            Capi.Input.RegisterHotKey(
                "burn-metal-flare", 
                "Flare allomantic metal", 
                GlKeys.Z, 
                HotkeyType.CharacterControls);

            Capi.Input.SetHotKeyHandler("burn-metal-toggle", a => {
                Channel.SendPacket(new BurnMessage(metalSelector.SelectedMetal, 4));
				return true;
			});
			Capi.Input.SetHotKeyHandler("burn-metal-inc", a => {
                Channel.SendPacket(new BurnMessage(metalSelector.SelectedMetal, 3));
				return true;
			});
            Capi.Input.SetHotKeyHandler("burn-metal-dec", a => {
                Channel.SendPacket(new BurnMessage(metalSelector.SelectedMetal, 2));
				return true;
			});
            Capi.Input.SetHotKeyHandler("burn-metal-flare", a => {
                Channel.SendPacket(new BurnMessage(metalSelector.SelectedMetal, 1));
				return true;
			});

            // Hotkeys for GUI
            Capi.Input.RegisterHotKey(
                "guimetalselect", 
                "Select allomantic metal", 
                GlKeys.K, 
                HotkeyType.GUIOrOtherControls);

            Capi.Input.SetHotKeyHandler("guimetalselect", ToggleMetalSelectGui);
            
            // Add event to know when the game has loaded.
            Capi.Event.BlockTexturesLoaded += OnLoad;

            // Create an allomancy helper for the entity.
            Capi.Event.LevelFinalize += () => {
                AllomancyHelper = new AllomancyPropertyHelper(Capi.World.Player.Entity);
            };

            // Register UI updates
            Capi.Event.RegisterGameTickListener((float dt) => {
                AllomancyHelper.Entity = Capi.World.Player.Entity;
                AllomancyHelper.UpdateTree();
                if (AllomancyHelper != null) {
                    metalSelector.UpdateUI(dt);
                }
            }, 10);

            motionParticles.gravityEffect = 0;

            // Visual effects updates
            Capi.Event.RegisterGameTickListener((float dt) => {
                if (AllomancyHelper != null) {
                    float maxhealth = ((ITreeAttribute)Capi.World.Player.Entity.WatchedAttributes["health"]).GetFloat("maxhealth");
                    float fatigue = AllomancyHelper.GetPewterFatigue();
                    targetVignete = fatigue / maxhealth;
                    if (targetVignete > 1) { targetVignete = 1; }
                    ShaderLoader.VigneteStrength += (targetVignete - ShaderLoader.VigneteStrength)/5;
                    int tinstatus = AllomancyHelper.GetEffectiveBurnStatus("tin");
                    targetNightvision = tinstatus * (1.0f / 5.0f);
                    ShaderLoader.NightvisionStrength += (targetNightvision - ShaderLoader.NightvisionStrength)/5;
                }
            }, 0);
            Capi.Event.RegisterGameTickListener((float dt) => {
                int tinstatus = AllomancyHelper.GetEffectiveBurnStatus("tin");
                if (previousTinStatus == 0 && tinstatus != 0) {
                    Capi.Settings.Int["cachedfov"] = Capi.Settings.Int["fieldOfView"];
                } 
                if (tinstatus == 0 && previousTinStatus != 0) {
                    if (Capi.Settings.Int["cachedfov"] != 0)
                        Capi.Settings.Int["fieldOfView"] = Capi.Settings.Int["cachedfov"];
                }
                if (tinstatus > 0) {
                    Capi.Settings.Int["fieldOfView"] = 100 - tinstatus * 18;
                    motionParticles.glowLevel = (byte)(255.0f * tinstatus * (1.0f / 5.0f));
                    float vspeed = 3.0f * tinstatus * (1.0f / 5.0f) + 0.1f;
                    motionParticles.minSize = 3.0f * tinstatus * (1.0f / 5.0f) + 1f;
                    motionParticles.maxSize = 3.0f * tinstatus * (1.0f / 5.0f) + 1f;
                    motionParticles.minVelocity.Y = vspeed;                        
                    Entity[] nearbyEnts = Capi.World.GetEntitiesAround(Capi.World.Player.Entity.Pos.XYZ, 100, 100);
                    foreach(Entity ent in nearbyEnts) {
                        if (ent == Capi.World.Player.Entity) continue;
                        motionParticles.minPos = ent.Pos.XYZ;
                        Capi.World.SpawnParticles(motionParticles);
                    }
                }
                previousTinStatus = tinstatus;
            }, 100);
        }

        private void OnUpdateAlloHelper(ReplaceAlloHelperEntity message) {
            AllomancyHelper = new AllomancyPropertyHelper(Capi.World.Player.Entity);
        }

        private void OnSelectedMetalMessage (SelectedMetalMessage message) {
            if (message._metal_id < -1 || message._metal_id >= MistModSystem.METALS.Length) {
                return;
            }
            if (message._metal_id != -1) {
                metalSelector.SelectMetal(message._metal_id);
            }
        }

        private void OnLoad () {
            // Instantiate the metal selector.
            metalSelector = new GuiDialogMetalSelector(Capi, this);
            // Ask the server for the cached selected slot
            Channel.SendPacket(new SelectedMetalMessage(-1));
        }

        private bool ToggleMetalSelectGui (KeyCombination comb) {
            if(metalSelector.IsOpened()) metalSelector.TryClose();
            else metalSelector.TryOpen();
            return true;
        }        
    }
}