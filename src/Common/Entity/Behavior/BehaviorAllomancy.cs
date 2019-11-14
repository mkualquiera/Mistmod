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

        TreeAttribute allomancyTree;

        /// <summary> Tree containing the allomantic powers for the player </summary>
        public ITreeAttribute AllomanticPowers
        {
            get { return allomancyTree.GetTreeAttribute("powers"); }
        }

        /// <summary> Tree containing the metal reserves for the player </summary>
        public ITreeAttribute MetalReserves 
        {
            get { return allomancyTree.GetTreeAttribute("metals"); }
        }

        /// <summary> Tree containing the burn status for the player </summary>
        public ITreeAttribute BurnStatus
        {
            get { return allomancyTree.GetTreeAttribute("status"); }
        }

        /// <summary> Tree containing the burn toggle the player </summary>
        public ITreeAttribute BurnToggle
        {
            get { return allomancyTree.GetTreeAttribute("toggle");}
        }

        /// <summary> Create an allomantic behavior for an entity. </summary>
        public EntityBehaviorAllomancy(Entity entity) : base(entity)
        {
        }

        private static Random RNG = new Random();

        /// <summary> Initialize the behavior </summary>
        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            // Obtain the tree of allomantic properties for the entity
            allomancyTree = (TreeAttribute)entity.WatchedAttributes.GetTreeAttribute("allomancy");
            if (allomancyTree == null)
            {
                // The entity doesn't have any allomantic properties registered, so we add them.
                entity.WatchedAttributes.SetAttribute("allomancy", allomancyTree = new TreeAttribute());
                allomancyTree.SetAttribute("powers", new TreeAttribute());
                allomancyTree.SetAttribute("metals", new TreeAttribute());
                allomancyTree.SetAttribute("status", new TreeAttribute());
                allomancyTree.SetAttribute("toggle", new TreeAttribute());
                allomancyTree.SetString("selectedMetal", "none");
                // Make the entity a random misting.
                string chosenPower = MistModSystem.METALS[RNG.Next(0, MistModSystem.METALS.Length)];
                AllomanticPowers.SetBool(chosenPower, true);
                entity.WatchedAttributes.MarkPathDirty("allomancy");
                return;
            }
        }
        
        /// <summary> Set the currently selected metal for the entity with this behavior </summary>
        public void SetSelectedMetal (string metal) {
            allomancyTree.SetString("selectedMetal", metal);
            entity.WatchedAttributes.MarkPathDirty("allomancy");
        }

        /// <summary> Get the currently selected metal for the entity with this behavior </summary>
        public string GetSelectedMetal () {
            return allomancyTree.GetString("selectedMetal");
        }
 
        /// <summary> Enable all allomantic powers for the entity with this behavior </summary>
        public void EnableAllPowers () {
            foreach (string power in MistModSystem.METALS) {
                EnablePower(power);
            }
        }

        /// <summary> Disable a specific allomantic power for this entity </summary>
        /// <param id="metal"> The name of the metal power to be removed </param>
        public void DisablePower (string metal) {
            SetPower(metal, true);
        }

        /// <summary> Disable a specific allomantic power for this entity </summary>
        /// <param id="metal"> The name of the metal power to be removed </param>
        public void EnablePower (string metal) {
            SetPower(metal, true);
        }

        /// <summary> Set a specific allomantic power for this entity </summary>
        /// <param id="metal"> The name of the metal power to be set </param> 
        public void SetPower (string metal, bool value) {
            AllomanticPowers.SetBool(metal, value);
            entity.WatchedAttributes.MarkPathDirty("allomancy");
        }

        /// <summary> Find if this entity has a specific allomantic power </summary>
        /// <param id="metal"> The name of the metal power to be queried </param>
        public bool GetPower (string metal) {
            return AllomanticPowers.GetBool(metal);
        }

        /// <summary> Toggle the burn of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to be toggled </param>
        public void ToggleBurn (string metal) {
            SetBurnToggle(metal, !GetBurnToggle(metal));
        }

        /// <summary> Set the toggle of the burn of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to be toggled </param>
        public void SetBurnToggle (string metal, bool value) {
            BurnToggle.SetBool(metal, value);
            entity.WatchedAttributes.MarkPathDirty("allomancy");
        }

        /// <summary> Get the toggle of the burn of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to be toggled </param>
        public bool GetBurnToggle (string metal) {
            return BurnToggle.GetBool(metal);
        }

        /// <summary> Set the burn status of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to change the burn </param>
        public void SetBurnStatus (string metal, int status) {
            if (status < 0) status = 0;
            if (status > 5) status = 5;
            BurnStatus.SetInt(metal, status);
            entity.WatchedAttributes.MarkPathDirty("allomancy");
        }
        
        /// <summary> Get the burn status of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to get the burn of </param>
        public int GetBurnStatus (string metal) {
            return BurnStatus.GetInt(metal);
        }

        /// <summary> Increment the burn status of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to change the burn </param>
        public void IncrementBurnStatus(string metal, int amount) {
            SetBurnStatus(metal, GetBurnStatus(metal) + amount);
        }

        /// <summary> Increment the metal reserve of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to increment the reserve </param>
        public void IncrementMetalReserve(string metal, float amount) {
            SetMetalReserve(metal, GetMetalReserve(metal) + amount);
        }

        /// <summary> Increment the metal reserve of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to increment the reserve </param>
        public void SetMetalReserve(string metal, float amount) {
            if (amount < 0) {
                amount = 0;
            }
            MetalReserves.SetFloat(metal, amount);
            entity.WatchedAttributes.MarkPathDirty("allomancy");
        }

        /// <summary> Get the metal reserve of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power of which to obtain the reserve of</param>
        public float GetMetalReserve(string metal) {
            return MetalReserves.GetFloat(metal);
        }

        /// <summary> Print all allomantic properties of the entity </summary>
        public void Debug(){
            Console.WriteLine(allomancyTree.ToJsonToken());
        }

        private int keyTick = 0;
        
        public override void OnGameTick(float deltaTime)
        {
            keyTick++;
            if (BurnToggle != null) {
                foreach (var pair in BurnToggle) {
                    if (GetBurnToggle(pair.Key)) {
                       TryExecuteAllomanticEffect(pair.Key, GetBurnStatus(pair.Key), false);
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
            if (!GetPower(power)) { return; }
            if (GetMetalReserve(power) <= 0) { return; }
            float consumption = ((float)strength / 100);
            if (flare) { consumption += 1/50; }
            IncrementMetalReserve(power, -consumption);
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