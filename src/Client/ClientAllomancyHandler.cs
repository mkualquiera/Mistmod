using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
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

        GuiDialogMetalSelector metalSelector;

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