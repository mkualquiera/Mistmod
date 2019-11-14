using System;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace MistMod
{
    /// <summary> Class for items that provide allomantic metals when consumed </summary>
    public class ItemAllomanticMetalProvider : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            // Play the sound of consuming the item.
            byEntity.World.RegisterCallback((dt) =>
            {
                if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
                {
                    IPlayer player = null;
                    if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/player/poultice"), byEntity, player);
                }
            }, 200);
            
            handling = EnumHandHandling.PreventDefault;

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            return;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // Play the animation
            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0, 0f);

                tf.Translation.X -= Math.Min(1.5f, secondsUsed * 4 * 1.57f);

                //tf.Rotation.X += Math.Min(30f, secondsUsed * 350);
                tf.Rotation.Y += Math.Min(130f, secondsUsed * 350);

                byEntity.Controls.UsingHeldItemTransformAfter = tf;

                return secondsUsed < 0.75f;
            }

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            // Add the efects once the interaction has finalized.
            if (secondsUsed > 0.7f && byEntity.World.Side == EnumAppSide.Server)
            {
                EntityBehaviorAllomancy allomancy = (EntityBehaviorAllomancy)byEntity.GetBehavior("allomancy");
                if (allomancy != null) {
                    JsonObject attr = slot.Itemstack.Collectible.Attributes;
                    float amount = attr["amount"].AsFloat();
                    string metal = attr["metal"].AsString();
                    allomancy.IncrementMetalReserve(metal, amount);
                    slot.TakeOut(1);
                    allomancy.Debug();
                }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            JsonObject attr = inSlot.Itemstack.Collectible.Attributes;
            if (attr != null && attr["metal"].Exists)
            {
                float amount = attr["amount"].AsFloat();
                string metal = attr["metal"].AsString();
                dsc.AppendLine(amount + " of " + metal);
            }
        }



        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "heldhelp-burn",
                    MouseButton = EnumMouseButton.Right
                },
                new WorldInteraction() {
                    HotKeyCode = "CTRL",
                    ActionLangCode = "heldhelp-flare",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }
    }
}