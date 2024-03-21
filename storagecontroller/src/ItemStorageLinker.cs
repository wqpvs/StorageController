using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace storagecontroller
{
    public class ItemStorageLinker : Item
    {
        public static string posValue = "linkto";

        public static string linkedValue = "linktodesc";

        public WorldInteraction WorldInteraction;

        public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            return "interactstatic";
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null || slot == null) return;

            if (byEntity is not EntityPlayer entityPlayer) return;

            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
                return;

            if (!byEntity.World.Claims.TryAccess(entityPlayer?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                return;

            if (slot.Empty || slot.Itemstack == null) return;

            BlockEntity targetEntity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);

            handling = EnumHandHandling.PreventDefault;

            ITreeAttribute attributes = slot.Itemstack.Attributes;

            // maybe have to move it over to OnPlayerRightClick on BlockEntityStorageController
            if (entityPlayer.Controls.CtrlKey)
            {
                if (api is ICoreClientAPI capi)
                {   // if player is pressing down ctrlkey and is looking at Storage Controller show high light blocks.
                    if (targetEntity is BlockEntityStorageController)
                    {
                        capi.Network.SendBlockEntityPacket(blockSel.Position, BlockEntityStorageController.showHighLightPacket);
                        return;
                    }
                }
            }
            else
            {   // If the block is a storage controller, set it as the target
                if (targetEntity is BlockEntityStorageController)
                {
                    attributes.SetBlockPos(posValue, blockSel.Position);
                    attributes.SetString(linkedValue, blockSel.Position.ToLocalPosition(api).ToString());
                    slot.MarkDirty();
                    return;
                }

                // Quit if attributes is not set
                if (!attributes.HasAttribute(posValue + "X"))
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(!attributes.HasAttribute(posValue + "X"), $"Use {slot.Itemstack.GetName()} on storage controller first", Lang.Get("storagecontroller:helditem-error-linker {0}", slot.Itemstack.GetName()));
                    return;
                }

                // Check for valid SCM
                BlockPos StorageControllerPos = slot.Itemstack.Attributes.GetBlockPos(posValue);
                if (StorageControllerPos == null)
                    return;

                BlockEntityStorageController blockEntityStorageController = api.World.BlockAccessor.GetBlockEntity(StorageControllerPos) as BlockEntityStorageController;
                if (blockEntityStorageController == null)
                    return;


                blockEntityStorageController.ToggleContainer(slot, byEntity, blockSel);
            }
        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            if (itemStack != null && itemStack.Attributes.HasAttribute(linkedValue))
            {

                return $"Storage Linker (Linked to ({itemStack.Attributes.GetString(linkedValue)})";
            }

            return base.GetHeldItemName(itemStack);
        }
        

    }
}
