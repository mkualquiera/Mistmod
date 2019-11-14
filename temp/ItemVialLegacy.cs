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
    public class ItemVialLegacy : Item
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
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

            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform tf = new ModelTransform();

                tf.EnsureDefaultValues();

                tf.Origin.Set(0f, 0, 0f);

                tf.Translation.X -= Math.Min(1.5f, secondsUsed * 4 * 1.57f);

                //tf.Rotation.X += Math.Min(30f, secondsUsed * 350);
                tf.Rotation.Y += Math.Min(130f, secondsUsed * 350);

                byEntity.Controls.UsingHeldItemTransformAfter = tf;

                return secondsUsed < 0.25f;
            }

            return true;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed > 0.2f && byEntity.World.Side == EnumAppSide.Server)
            {
                if (slot.Itemstack.Item.LastCodePart() == "steel") {
                    float pitch = GameMath.PI - byEntity.ServerPos.Pitch + GameMath.PI;
                    float yaw = byEntity.ServerPos.Yaw + GameMath.PIHALF;
                    BlockSelection blockSelectionUnlimited = null;
                    EntitySelection entitySelectionUnlimited = null;
                    byEntity.World.RayTraceForSelection(byEntity.ServerPos.XYZ,byEntity.ServerPos.Pitch, byEntity.ServerPos.Yaw, 30, ref blockSelectionUnlimited, ref entitySelectionUnlimited);
                    if (blockSelectionUnlimited != null) {
                        Block selectedBlock = byEntity.World.BlockAccessor.GetBlock(blockSelectionUnlimited.Position);
                        Console.WriteLine(selectedBlock.Code.ToString());
                    }
                    byEntity.ServerPos.Motion.Add(
                    (GameMath.Sin(yaw) * GameMath.Cos(pitch)) / 7,
                    (GameMath.Sin(pitch)) / 7,
                    (GameMath.Cos(yaw) * GameMath.Cos(pitch)) / 7);
                    ((IServerPlayer)byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID)).SendPositionToClient();
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