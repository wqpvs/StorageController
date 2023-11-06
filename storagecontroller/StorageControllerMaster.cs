using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using ProtoBuf;
using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace storagecontroller
{
    public class StorageControllerMaster:BlockEntityGenericTypedContainer
    {
        public static string containerlistkey = "containerlist";
        List<BlockPos> containerlist; //TODO: add to treeattributes
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
        }

        //stuff to do every so often
        public void OnServerTick(float dt)
        {
            //Check if we have any inventory to bother with
            if (this.Inventory == null || this.Inventory.Empty) { return; }
            
            //Manage linked container list
            // - only check so many blocks per tick
            List<BlockPos> prunelist = new List<BlockPos>(); //This is a list of invalid blockpos that should be deleted from list
            List<BlockEntityGenericTypedContainer> validcontainers = new List<BlockEntityGenericTypedContainer>(); //list of usable containers
            if (containerlist != null) {
                
                foreach (BlockPos pos in containerlist)
                {
                    BlockEntityGenericTypedContainer thiscont = Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityGenericTypedContainer;
                    //no valid container here so set to remove this location ***Should we do that or just let new containers be placed later??
                    if (thiscont == null)
                    {
                        prunelist.Add(pos);
                    }
                    else
                    {
                        validcontainers.Add(thiscont);
                    }
                }
            }
            //Need to populate containers if this container has inventory
            // - from list of container first find a stack or locked container with inventory
            // - then push in inventory if possible
            // - otherwise look for empty slots we can put our inventory into

        }

        //add a container to the list of managed containers (usually called by a storage linker)
        public void AddContainer(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            //don't want to link to ourself!
            if (blockSel.Position == this.Pos) { return; } 
            //check for valid container
            BlockEntityGenericTypedContainer cont = Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGenericTypedContainer;
            if (cont == null) { return; }
            //ensure block isn't reinforced

            //ensure player as access rights
            
            //if container isn't on list then add it
            if (containerlist==null) { containerlist = new List<BlockPos>();}
            if (!containerlist.Contains(blockSel.Position)) {
                if (Api is ICoreServerAPI)
                {
                    containerlist.Add(blockSel.Position); MarkDirty();
                }
            }

        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            var asString = tree.GetString(containerlistkey);
            if (asString!=null)
            {
                containerlist=JsonConvert.DeserializeObject<List<BlockPos>>(asString);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            var asString = JsonConvert.SerializeObject(containerlist);
            tree.SetString(containerlistkey, asString);
            base.ToTreeAttributes(tree);
        }
    }
}
