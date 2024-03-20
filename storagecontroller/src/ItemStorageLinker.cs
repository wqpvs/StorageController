using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace storagecontroller
{
    internal class ItemStorageLinker : Item
    {
        public static string islkey="linkto";

        public static string isldesc = "linktodesc";

        public bool toggle = false;

        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || slot == null) return;

            if (byEntity is not EntityPlayer entityPlayer) return;

            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
                return;

            if (!byEntity.World.Claims.TryAccess(entityPlayer?.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                return;

            if (slot.Empty || slot.Itemstack == null) return;

            BlockEntity targetEntity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            handling = EnumHandHandling.PreventDefaultAction;

            if (entityPlayer.Controls.CtrlKey)
            {
                if (api is ICoreClientAPI)
                {
                    toggle = !toggle;

                    (targetEntity as BlockEntityStorageController)?.ToggleHighLight(toggle);
                }

                return;
            }

            // If the block is a storage controller, set it as the target
            if (targetEntity is BlockEntityStorageController)
            {
                slot.Itemstack.Attributes.SetBlockPos(islkey, blockSel.Position);
                slot.Itemstack.Attributes.SetString(isldesc, blockSel.Position.ToLocalPosition(api).ToString());
                slot.MarkDirty();
                return;
            }

            BlockEntityContainer targetContainer = targetEntity as BlockEntityContainer;
            if (targetContainer == null)
                return;

            // Quit if islkey is not set
            if (!slot.Itemstack.Attributes.HasAttribute(islkey + "X"))
                return;

            // Check for valid SCM
            BlockPos scmPos = slot.Itemstack.Attributes.GetBlockPos(islkey);
            if (scmPos == null)
                return;

            BlockEntityStorageController scm = api.World.BlockAccessor.GetBlockEntity(scmPos) as BlockEntityStorageController;
            if (scm == null)
                return;


            scm.ToggleContainer(slot, byEntity, blockSel);

        }

        public override string GetHeldItemName(ItemStack itemStack)
        {
            if (itemStack != null && itemStack.Attributes.HasAttribute(isldesc))
            {
                
                return "Storage Linker (Linking to "+itemStack.Attributes.GetString(isldesc)+")";
            }
            return base.GetHeldItemName(itemStack);
        }
        
    }
}
