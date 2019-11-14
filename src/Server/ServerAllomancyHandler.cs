using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
namespace MistMod {
    public class ServerAllomancyHandler {

        public static ServerAllomancyHandler Current;
        public ICoreServerAPI Sapi;
        public MistModSystem System;
        public IServerNetworkChannel Channel;
        public ServerAllomancyHandler(ICoreServerAPI sapi, MistModSystem system) {
            System = system;
            Sapi = sapi;
            Current = this;
        }

        public void Initialize () {
            Sapi.RegisterEntityBehaviorClass("allomancy",typeof(EntityBehaviorAllomancy));
            Channel = Sapi.Network.RegisterChannel(MistModSystem.MOD_ID)
			.RegisterMessageType(typeof(BurnMessage));
            Channel.SetMessageHandler<BurnMessage>(OnBurnMetalMessage);
        }

        public void OnBurnMetalMessage(IServerPlayer player, BurnMessage message) {
            EntityBehaviorAllomancy allomancy = ((EntityBehaviorAllomancy)player.Entity.GetBehavior("allomancy"));
            if (message._burn_strength == 1) {
                int currentStrength = allomancy.GetBurnStatus(MistModSystem.METALS[message._metal_id]);
                allomancy.TryExecuteAllomanticEffect(MistModSystem.METALS[message._metal_id], currentStrength, true);
            } 
            if (message._burn_strength == 2) {
                allomancy.IncrementBurnStatus(MistModSystem.METALS[message._metal_id], -1);
            }
            if (message._burn_strength == 3) {
                allomancy.IncrementBurnStatus(MistModSystem.METALS[message._metal_id], 1);
            }
            if (message._burn_strength == 4) {
                allomancy.ToggleBurn(MistModSystem.METALS[message._metal_id]);
            }
            allomancy.Debug();
        }        
    }
}