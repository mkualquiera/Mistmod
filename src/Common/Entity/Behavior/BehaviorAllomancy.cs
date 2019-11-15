using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MistMod
{
    public class EntityBehaviorAllomancy : EntityBehavior
    {
        /// <summary> The helper for easily interacting with the allomantic properties of the entity </summary>
        public AllomancyPropertyHelper Helper;
        /// <summary> Create an allomantic behavior for an entity. </summary>
        public EntityBehaviorAllomancy(Entity entity) : base(entity)
        {
        }


        /// <summary> Initialize the behavior </summary>
        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            Helper = new AllomancyPropertyHelper(entity);
            Helper.Initialize();
        }

        private int keyTick = 0;
        
        public override void OnGameTick(float deltaTime)
        {
            keyTick++;
            if (Helper.BurnToggle != null) {
                foreach (var pair in Helper.BurnToggle) {
                    if (Helper.GetBurnToggle(pair.Key)) {
                       TryExecuteAllomanticEffect(pair.Key, Helper.GetBurnStatus(pair.Key), false);
                    }
                }
            }
            if (keyTick >= 1000) {
                keyTick = 0;
            }
        }

        /// <summary> Try to execute an allomantic effect from this entity </summary>
        public void TryExecuteAllomanticEffect (string power, int strength, bool flare) {
            //Console.WriteLine(GetPower(power) + " " + GetMetalReserve(power));
            if (!Helper.GetPower(power)) { return; }
            if (Helper.GetMetalReserve(power) <= 0) { return; }
            float consumption = ((float)strength / 100);
            if (flare) { consumption += 1/50; }
            Helper.IncrementMetalReserve(power, -consumption);
            if (power == "steel" | power == "iron") {
                if (keyTick % 15 == 0 | flare) {
                    float divider = 23;
                    float magnitude = strength / divider;
                    if (flare) { magnitude += 2/10; }
                    float forwardpitch = GameMath.PI - entity.ServerPos.Pitch + GameMath.PI;
                    float forwardyaw = entity.ServerPos.Yaw + GameMath.PIHALF;
                    float inversepitch = GameMath.PI - entity.ServerPos.Pitch + GameMath.PI;
                    float inverseyaw = entity.ServerPos.Yaw + GameMath.PIHALF + GameMath.PI;
                    float playerpitch = power == "steel" ? forwardpitch : inversepitch;
                    float playeryaw = power == "steel" ? forwardyaw : inverseyaw;
                    float targetpitch = power == "steel" ? inversepitch : forwardpitch;
                    float targetyaw = power == "steel" ? inverseyaw : forwardyaw;
                    entity.ServerPos.Motion.Add(
                    (GameMath.Sin(playeryaw) * GameMath.Cos(playerpitch)) * magnitude,
                    (GameMath.Sin(playerpitch)) * magnitude,
                    (GameMath.Cos(playeryaw) * GameMath.Cos(playerpitch)) * magnitude);
                    ((IServerPlayer)entity.World.PlayerByUid(((EntityPlayer)entity).PlayerUID)).SendPositionToClient();
                }
            }
		}

        public override string PropertyName()
        {
            return "allomancy";
        }
    }
}