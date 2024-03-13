using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using System.Security.Cryptography;

namespace storagecontroller
{
    internal class ItemStorageLinker : Item
    {
        public static string islkey="linkto";
        public static string isldesc = "linktodesc";

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (blockSel == null) return;

            ICoreAPI api = byEntity.Api;

            if (api == null || slot == null || slot.Itemstack == null || slot.Itemstack.Item == null)
                return;

            // Ensure block isn't reinforced
            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
                return;

            // Ensure player has access rights
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
                return;

            // Get targeted block entity
            BlockEntity targetEntity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            handling = EnumHandHandling.PreventDefaultAction;

            // If the block is a storage controller, set it as the target
            if (targetEntity is StorageControllerMaster && !byPlayer.Entity.Controls.CtrlKey)
            {
                slot.Itemstack.Attributes.SetBlockPos(islkey, blockSel.Position);
                slot.Itemstack.Attributes.SetString(isldesc, blockSel.Position.ToLocalPosition(api).ToString());
                slot.MarkDirty();
                return;
            }

            // Otherwise, check if it's a valid storage device and tell controller about it
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

            StorageControllerMaster scm = api.World.BlockAccessor.GetBlockEntity(scmPos) as StorageControllerMaster;
            if (scm == null)
                return;

            // Handle adding or removing container
            if (api is ICoreServerAPI && byPlayer.Entity.Controls.ShiftKey)
            {
                scm.RemoveContainer(slot, byEntity, blockSel);
            }
            else if (api is ICoreServerAPI)
            {
                scm.AddContainer(slot, byEntity, blockSel);
            }
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
