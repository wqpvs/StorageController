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
    internal class ItemStorageLinker:Item
    {
        public static string islkey="linkto";
        public static string isldesc = "linktodesc";
        //public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        //{
        //    base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        //}
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            ICoreAPI api = byEntity.Api;
            if (api == null || slot == null || slot.Itemstack == null||slot.Itemstack.Item==null)
            {
                base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }
            // get targetted block
            BlockEntity targetentity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            
            //if the block is a storage controller then set it as the target
            if (targetentity is StorageControllerMaster) {
                slot.Itemstack.Attributes.SetBlockPos(islkey, blockSel.Position);
                slot.Itemstack.Attributes.SetString(isldesc, blockSel.Position.ToLocalPosition(api).ToString());
                slot.MarkDirty();
                handling = EnumHandHandling.Handled;
                return;
            }

            //otherwise we will see if it's a valid storage device and tell controller about it
            BlockEntityContainer targetcont=targetentity as BlockEntityContainer;
            if ( targetentity==null)
            {
                return;
            }
            //quit if islkey is not set
            if (!slot.Itemstack.Attributes.HasAttribute(islkey+"X")) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel,  ref handling); return; }

            //Check for valid SCM
            BlockPos scmpos = slot.Itemstack.Attributes.GetBlockPos(islkey);
            if (scmpos == null) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel,  ref handling); return; }
            StorageControllerMaster scm = api.World.BlockAccessor.GetBlockEntity(scmpos) as StorageControllerMaster;
            if (scm == null) { base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel,  ref handling); return; }
            if (api is ICoreServerAPI) { scm.AddContainer(slot,byEntity,blockSel); }
            handling=EnumHandHandling.Handled;

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
