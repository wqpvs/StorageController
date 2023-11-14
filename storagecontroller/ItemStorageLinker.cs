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
                
                //base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
                return;
            }
            //ensure block isn't reinforced
            if (api.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(blockSel.Position) == true)
            {
                return;
            }
            //ensure player as access rights
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {

                return;
            }
            // get targetted block
            BlockEntity targetentity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            handling = EnumHandHandling.PreventDefaultAction;
            //if the block is a storage controller then set it as the target
            if (targetentity is StorageControllerMaster) {
                slot.Itemstack.Attributes.SetBlockPos(islkey, blockSel.Position);
                slot.Itemstack.Attributes.SetString(isldesc, blockSel.Position.ToLocalPosition(api).ToString());
                slot.MarkDirty();
                
                return;
            }

            //otherwise we will see if it's a valid storage device and tell controller about it
            BlockEntityContainer targetcont=targetentity as BlockEntityContainer;
            if ( targetentity==null)
            {
                
                return;
            }
            //quit if islkey is not set
            if (!slot.Itemstack.Attributes.HasAttribute(islkey+"X")) { return; }

            //Check for valid SCM
            BlockPos scmpos = slot.Itemstack.Attributes.GetBlockPos(islkey);
            if (scmpos == null) {  return; }
            StorageControllerMaster scm = api.World.BlockAccessor.GetBlockEntity(scmpos) as StorageControllerMaster;
            if (scm == null) { return; }
            if (api is ICoreServerAPI && byPlayer.Entity.Controls.ShiftKey) { scm.RemoveContainer(slot, byEntity, blockSel); }
            else if (api is ICoreServerAPI) { scm.AddContainer(slot,byEntity,blockSel); }
            

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
