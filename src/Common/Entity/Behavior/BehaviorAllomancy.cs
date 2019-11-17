using System;
using System.Collections.Generic;
using Vintagestory.API.Server;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

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


        /// <summary> Called when the entity has received damage, but after armor damage reduction was applied </summary>
        public float OnDamageAfterArmor (float damage, DamageSource source) {
            float newDamage = damage;
            int effectivePewterBurnStatus = Helper.GetEffectiveBurnStatus("pewter");
            if (effectivePewterBurnStatus > 0 && source.Type != EnumDamageType.Heal) {
                float reductionAmount = (1f / 5.0f * effectivePewterBurnStatus);
                float reducedDamage = damage * reductionAmount;
                newDamage -= reducedDamage;
                Helper.IncreasePewterFatigue(reducedDamage * 2);
            }
            EntityBehaviorHealth health = (EntityBehaviorHealth)entity.GetBehavior("health");
            if (newDamage > health.Health) {
                if (Helper.GetPower("pewter") && source.Type != EnumDamageType.Heal) {
                    if (Helper.GetMetalReserve("pewter") > 0) {
                        Helper.SetBurnToggle("pewter", true);
                        Helper.IncrementBurnStatus("pewter", 1);
                        Helper.IncreasePewterFatigue(newDamage * Helper.GetEffectiveBurnStatus("pewter"));
                        newDamage = health.Health / 2;
                    }
                }
            }
            if (source.SourceEntity != null) {
                if (source.SourceEntity.HasBehavior("allomancy")) {
                    EntityBehaviorAllomancy enemyAllomancy = (EntityBehaviorAllomancy)source.SourceEntity.GetBehavior("allomancy");
                    int effectiveEnemyChromiumBurnStatus = enemyAllomancy.Helper.GetEffectiveBurnStatus("chromium");
                    if (effectiveEnemyChromiumBurnStatus > 0) {
                        Helper.ClearAllReserves();
                    }
                }
            }
            return newDamage;
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath) {
            Helper.ClearAllReserves();
            foreach (string metal in MistModSystem.METALS) {
                Helper.SetBurnStatus(metal, 0);
            }
        }

        private int keyTick = 0;
        
        private Vec3d prevPos;
        public override void OnGameTick(float deltaTime)
        {
            keyTick++;
            Helper.IncreasePewterFatigue(-Helper.GetPewterFatigue() / 15 * deltaTime);
            float speedBoost = Helper.GetEffectiveBurnStatus("pewter") * (1.0f / 5.0f);
            entity.Stats.Set("walkspeed", "allomancy", speedBoost, false);
            if (prevPos == null){
                prevPos = entity.Pos.XYZ.Clone();
            }
            float distance = prevPos.HorizontalSquareDistanceTo(entity.Pos.XYZ);
            prevPos = entity.Pos.XYZ.Clone();
            if (Helper.GetEffectiveBurnStatus("pewter") > 0) {
                Helper.IncreasePewterFatigue(distance / 16);
            }
            if (Helper.BurnToggle != null) {
                foreach (var pair in Helper.BurnToggle) {
                    if (Helper.GetBurnToggle(pair.Key)) {
                       TryExecuteActiveAllomanticEffect(pair.Key, Helper.GetBurnStatus(pair.Key), false);
                    }
                }
            }
            if (keyTick >= 1000) {
                keyTick = 0;
            }
        }

        /// <summary> Try to execute an allomantic effect from this entity </summary>
        public void TryExecuteActiveAllomanticEffect (string power, int strength, bool flare) {
            //Console.WriteLine(GetPower(power) + " " + GetMetalReserve(power));
            if (!Helper.GetPower(power)) { return; }
            if (Helper.GetMetalReserve(power) <= 0) { return; }
            float consumption = ((float)strength / 100);
            if (flare) { consumption += 1/50; }
            Helper.IncrementMetalReserve(power, -consumption);
            if (power == "aluminium") {
                Helper.ClearAllReserves();
            }
            EntityBehaviorHealth health = (EntityBehaviorHealth)entity.GetBehavior("health");
            if (power == "pewter") {
                if (health.Health < health.MaxHealth) {
                    float divider = 50;
                    float magnitude = strength / divider;
                    if (flare) { magnitude = 2; }
                    entity.ReceiveDamage(new DamageSource(){
                        Source = EnumDamageSource.Internal,
                        Type = EnumDamageType.Heal
                    }, magnitude);
                    Helper.IncreasePewterFatigue(magnitude);
                }
            } 
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