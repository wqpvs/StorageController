using System.Collections.Generic;
using Microsoft.VisualBasic;
using StorageController;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Reflection.Metadata.BlobBuilder;

namespace storagecontroller
{
    public class ItemVirtualStorageAccess:Item
    {
        /*
         - right click opens interface
            - spot for storage id and for a password (for testing we'll just use a fixed one)
            - default player user id?
            - some kind of admin interface?
            - single input slot
            - click on an item to extract slot
            - maybe do sorting etc client side, then confirm the transaction only server side

         - accesses a virtual storage area
       
         - where to save?
            - ensure we capture all the attribute data as well

         - spot to insert a storage module which can increase the number of slots
            - basically just different tiers, like copper adds one slot, bronze adds 2 or something etc
            

         */
        public override void OnHeldUseStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, bool firstEvent, ref EnumHandHandling handling)
        {
            ICoreClientAPI capi=byEntity.Api as ICoreClientAPI;
            if (capi != null)
            {
                StorageControllerModSystem modSystem = byEntity.Api.ModLoader.GetModSystem<StorageControllerModSystem>();
                
                (modSystem.storagenet as IClientNetworkChannel).SendPacket<string>("");
            }
            

            base.OnHeldUseStart(slot, byEntity, blockSel, entitySel, useType, firstEvent, ref handling);
        }
    }
}
