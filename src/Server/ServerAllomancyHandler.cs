using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

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

            Sapi.Event.PlayerJoin += OnPlayerJoin;
        }

        private void OnPlayerJoin (IServerPlayer playerJ) {
            var player = playerJ;
            var entity = player.Entity;
            Sapi.Event.RegisterCallback ((float dt) => {
                EntityBehaviorHealth health = (EntityBehaviorHealth)entity.GetBehavior("health");
                OnDamagedDelegate previousDelegate = health.onDamaged;
                health.onDamaged = (float damage, DamageSource source) => {
                    float previousDamage = previousDelegate(damage, source);
                    var allomancy = (EntityBehaviorAllomancy)entity.GetBehavior("allomancy");
                    return allomancy.OnDamageAfterArmor (previousDamage, source);
                };
            }, 500);
        }

        private void OnSelectedMetalMessage(IServerPlayer player, SelectedMetalMessage message) {
            EntityBehaviorAllomancy allomancy = (EntityBehaviorAllomancy)(player.Entity.GetBehavior("allomancy"));
            if (message._metal_id < -1 || message._metal_id >= MistModSystem.METALS.Length) {
                return;
            }
            if (message._metal_id == -1) { // The client doesn't know what the selected metal is
                string selectedMetal = allomancy.Helper.GetSelectedMetal();
                int result = -1;
                if (selectedMetal != "none") {
                    result = Array.IndexOf(MistModSystem.METALS, selectedMetal);
                }
                Channel.SendPacket(new SelectedMetalMessage(result), player);
            } else { // The client does know what the selected metal is.
                string selectedMetal = MistModSystem.METALS[message._metal_id];
                allomancy.Helper.SetSelectedMetal(selectedMetal);
            }
            allomancy.Helper.Debug();
        }

        private void OnBurnMetalMessage(IServerPlayer player, BurnMessage message) {
            EntityBehaviorAllomancy allomancy = ((EntityBehaviorAllomancy)player.Entity.GetBehavior("allomancy"));
            if (message._burn_strength == 1) { // Flare the metal
                int currentStrength = allomancy.Helper.GetBurnStatus(MistModSystem.METALS[message._metal_id]);
                allomancy.TryExecuteAllomanticEffect(MistModSystem.METALS[message._metal_id], currentStrength, true);
            } 
            if (message._burn_strength == 2) { // Decrease the burn status
                allomancy.Helper.IncrementBurnStatus(MistModSystem.METALS[message._metal_id], -1);
            }
            if (message._burn_strength == 3) { // Increase the burn status
                allomancy.Helper.IncrementBurnStatus(MistModSystem.METALS[message._metal_id], 1);
            }
            if (message._burn_strength == 4) { // Toggle the burn of the metal
                allomancy.Helper.ToggleBurn(MistModSystem.METALS[message._metal_id]);
            }
            allomancy.Helper.Debug();
        }        
    }
}