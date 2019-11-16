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
    /// <summary> Helper class to allow for easier manipulation of the allomantic properties tree </summary>
    public class AllomancyPropertyHelper
    {
        public Entity Entity;
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

        /// <summary> Construct an allomantic property helper from a given entity. </summary>
        public AllomancyPropertyHelper (Entity allomanticEntity) {
            // Obtain the tree of allomantic properties for the entity
            Entity = allomanticEntity;
            allomancyTree = (TreeAttribute)Entity.WatchedAttributes.GetTreeAttribute("allomancy");
        }

        private static Random RNG = new Random();

        /// <summary> Initialize the helper </summary>
        public void Initialize () {
            if (allomancyTree == null)
            {
                // The entity doesn't have any allomantic properties registered, so we add them.
                Entity.WatchedAttributes.SetAttribute("allomancy", allomancyTree = new TreeAttribute());
                allomancyTree.SetAttribute("powers", new TreeAttribute());
                allomancyTree.SetAttribute("metals", new TreeAttribute());
                allomancyTree.SetAttribute("status", new TreeAttribute());
                allomancyTree.SetAttribute("toggle", new TreeAttribute());
                allomancyTree.SetString("selectedMetal", "none");
                // Make the entity a random misting.
                string chosenPower = MistModSystem.METALS[RNG.Next(0, MistModSystem.METALS.Length)];
                AllomanticPowers.SetBool(chosenPower, true);
                Entity.WatchedAttributes.MarkPathDirty("allomancy");
                return;
            }
        }

        /// <summary> Set the currently selected metal for the entity with this behavior </summary>
        public void SetSelectedMetal (string metal) {
            allomancyTree.SetString("selectedMetal", metal);
            Entity.WatchedAttributes.MarkPathDirty("allomancy");
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
            Entity.WatchedAttributes.MarkPathDirty("allomancy");
        }

        /// <summary> Clear all metal reserves from the entity </summary>
        public void ClearAllReserves () {
            foreach (string metal in MistModSystem.METALS) {
                SetMetalReserve(metal, 0);
            }
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
            Entity.WatchedAttributes.MarkPathDirty("allomancy");
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
            Entity.WatchedAttributes.MarkPathDirty("allomancy");
        }
        
        /// <summary> Get the burn status of a specific metal </summary> 
        /// <param id="metal"> The name of the metal power to get the burn of </param>
        public int GetBurnStatus (string metal) {
            return BurnStatus.GetInt(metal);
        }
        
        /// <summary> Get the burn status of a metal given all conditions needed to burned it </summary>
        /// <param id="metal"> The name of the metal power to get the burn of </param>
        public int GetEffectiveBurnStatus (string metal) {
            if (!GetPower(metal)) return 0;
            if (!GetBurnToggle(metal)) return 0;
            if (GetMetalReserve(metal) <= 0) return 0;
            return GetBurnStatus(metal);
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
            Entity.WatchedAttributes.MarkPathDirty("allomancy");
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
    }
}