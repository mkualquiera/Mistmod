using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
namespace MistMod {

    /// <summary> Server side handler for mod functionality </summary<>
    public class ServerAllomancyHandler {
        
        /// <summary> Handler currently in use </summary>
        public static ServerAllomancyHandler Current;
        /// <summary> The api for the server-side </summary>
        public ICoreServerAPI Sapi;
        /// <summary> The Mistmod system </summary>
        public MistModSystem System;
        /// <summary> Channel for server-side communcation </summary>
        public IServerNetworkChannel Channel;
        /// <summary> Create a new server-side handler </summary> 
        public ServerAllomancyHandler(ICoreServerAPI sapi, MistModSystem system) {
            System = system;
            Sapi = sapi;
            Current = this;
        }
        /// <summary> Initialize the handler </summary>
        public void Initialize () {
            Sapi.RegisterEntityBehaviorClass("allomancy",typeof(EntityBehaviorAllomancy));

            Channel = Sapi.Network.RegisterChannel(MistModSystem.MOD_ID)
			    .RegisterMessageType(typeof(BurnMessage))
                .RegisterMessageType(typeof(SelectedMetalMessage));

            Channel.SetMessageHandler<BurnMessage>(OnBurnMetalMessage);
            Channel.SetMessageHandler<SelectedMetalMessage>(OnSelectedMetalMessage);
        }

        private void OnSelectedMetalMessage(IServerPlayer player, SelectedMetalMessage message) {
            EntityBehaviorAllomancy allomancy = (EntityBehaviorAllomancy)(player.Entity.GetBehavior("allomancy"));
            if (message._metal_id < -1 || message._metal_id >= MistModSystem.METALS.Length) {
                return;
            }
            if (message._metal_id == -1) { // The client doesn't know what the selected metal is
                string selectedMetal = allomancy.GetSelectedMetal();
                int result = -1;
                if (selectedMetal != "none") {
                    result = Array.IndexOf(MistModSystem.METALS, selectedMetal);
                }
                Channel.SendPacket(new SelectedMetalMessage(result), player);
            } else { // The client does know what the selected metal is.
                string selectedMetal = MistModSystem.METALS[message._metal_id];
                allomancy.SetSelectedMetal(selectedMetal);
            }
            allomancy.Debug();
        }

        private void OnBurnMetalMessage(IServerPlayer player, BurnMessage message) {
            EntityBehaviorAllomancy allomancy = ((EntityBehaviorAllomancy)player.Entity.GetBehavior("allomancy"));
            if (message._burn_strength == 1) { // Flare the metal
                int currentStrength = allomancy.GetBurnStatus(MistModSystem.METALS[message._metal_id]);
                allomancy.TryExecuteAllomanticEffect(MistModSystem.METALS[message._metal_id], currentStrength, true);
            } 
            if (message._burn_strength == 2) { // Decrease the burn status
                allomancy.IncrementBurnStatus(MistModSystem.METALS[message._metal_id], -1);
            }
            if (message._burn_strength == 3) { // Increase the burn status
                allomancy.IncrementBurnStatus(MistModSystem.METALS[message._metal_id], 1);
            }
            if (message._burn_strength == 4) { // Toggle the burn of the metal
                allomancy.ToggleBurn(MistModSystem.METALS[message._metal_id]);
            }
            allomancy.Debug();
        }        
    }
}