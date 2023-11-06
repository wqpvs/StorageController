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
        
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            ICoreAPI api = byEntity.Api;
            
            // get targetted block
            BlockEntity targetentity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            
            //if the block is a storage controller then set it as the target
            if (api!=null && slot!=null && slot.Itemstack!=null && slot.Itemstack.Item!=null&& targetentity != null && targetentity is StorageControllerMaster) {
                slot.Itemstack.Attributes.SetBlockPos(islkey, blockSel.Position);
                slot.Itemstack.Attributes.SetString(isldesc, blockSel.Position.ToLocalPosition(api).ToString());
                if (api is ICoreServerAPI)
                {
                    slot.MarkDirty();
                }
                handling = EnumHandHandling.Handled;
                return;
            }

            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

        }
        public override string GetHeldItemName(ItemStack itemStack)
        {
            if (itemStack != null && itemStack.Attributes.HasAttribute(isldesc))
            {
                
                return "Linking to "+itemStack.Attributes.GetString(isldesc);
            }
            return base.GetHeldItemName(itemStack);
        }
        
    }
}
