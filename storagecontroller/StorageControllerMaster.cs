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
using Vintagestory.API.Config;
using System.Reflection;
using Vintagestory.API.Util;


namespace storagecontroller
{
    public class StorageControllerMaster:BlockEntityGenericTypedContainer
    {
        public static string containerlistkey = "containerlist";
        List<BlockPos> containerlist; //TODO: add to treeattributes
        List<string> supportedChests;
        public virtual List<string> SupportedChests => supportedChests;
        List<string> supportedCrates;
        public virtual List<string> SupportedCrates => supportedCrates;
        public virtual int TickTime => tickTime; //how many ms between ticks
        public virtual int MaxTransferPerTick => maxTransferPerTick; // The maximum of items to transfer
        public virtual int MaxRange => maxRange; //maximum distance (in blocks) that this controller will link to
        int maxTransferPerTick = 1;
        int maxRange = 10;
        int tickTime = 250;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            supportedChests = new List<string> { "GenericTypedContainer", "BEGenericSortableTypedContainer", "BESortableLabeledChest", "LabeledChest" };
            supportedCrates = new List<string> { "BBetterCrate", "BEBetterCrate", "Crate" };
            if (Block.Attributes != null)
            {
                maxTransferPerTick = Block.Attributes["maxTransferPerTick"].AsInt(maxTransferPerTick);
                maxRange = Block.Attributes["maxRange"].AsInt(maxRange);
                tickTime = Block.Attributes["tickTime"].AsInt(TickTime);
            }
                if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, TickTime); }
            
        }
        //Better crates: BBetterCrate, BEBetterCrate, Crate, GenericTypedContainer
        
        //stuff to do every so often
        public void OnServerTick(float dt)
        {
            //Check if we have any inventory to bother with
            if (this.Inventory == null || this.Inventory.Empty) { return; }
            if (containerlist==null||containerlist.Count==0) { return; }
            //Manage linked container list
            // - only check so many blocks per tick
            List<BlockPos> prunelist = new List<BlockPos>(); //This is a list of invalid blockpos that should be deleted from list
            
                
            foreach (BlockPos pos in containerlist)
            {
                    
                Block b = Api.World.BlockAccessor.GetBlock(pos);
                if (!(SupportedChests.Contains(b.EntityClass) || SupportedCrates.Contains(b.EntityClass))) 
                {
                    prunelist.Add(pos); continue;
                }
                BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
                if (be == null||!(be is BlockEntityContainer)) { prunelist.Add(pos); continue; }
            }
                
            foreach(BlockPos pos in prunelist)
            {
                containerlist.RemoveAll(x=>x.Equals(pos));
            }
            if (containerlist == null || containerlist.Count == 0) { return; } //if no locations were valid then quit
            //Need to populate containers if this container has inventory
            // - from list of container first find a stack or locked container with inventory
            // - then push in inventory if possible
            // - otherwise look for empty slots we can put our inventory into
            // TO FIX:
            //  - issues with crates:
            //     - crates do have multiple stacks, and will let you force in items, but really everything should be the same stack

            // Fill order:
            // - first top up any filtered better crates (including empties)
            // - top up any crate that has something in it that matches  
            // - start filling any other empty crate
            // - fill any chests with partial stacks
            // - fill empty chests last

            //Priority slots: filtered crate slots with space, other populated crates with space
            Dictionary<CollectibleObject, List<ItemSlot>> priorityslots = new Dictionary<CollectibleObject, List<ItemSlot>>();
            //Partial Chest slots - slots in chests that have space
            List<ItemSlot> populatedslots = new List<ItemSlot>();
            //Empty slots with nothing, including the first slot of an empty, unfiltered crate
            List<ItemSlot> emptyslots = new List<ItemSlot>();
            //This slotreference is to match the slot back up to its originating container
            Dictionary<ItemSlot,BlockEntityContainer>slotreference=new Dictionary<ItemSlot,BlockEntityContainer>();
            //Cycle thru our blocks to find containers we can use
            foreach (BlockPos p in containerlist)
            {
                BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(p);
                Block b = Api.World.BlockAccessor.GetBlock(p);
                BlockEntityContainer cont = be as BlockEntityContainer;
                
                if (cont == null||cont.Inventory==null) { continue; }//this shouldn't happen, but better to check
                //find better crates
                FieldInfo bettercratelock = be.GetType().GetField("lockedItemInventory");
                //ah, we have discovered a better crate!
                //HANDLE BETTER CRATES
                if (bettercratelock != null)
                {
                    var bettercratelockingslot = bettercratelock.GetValue(be) as InventoryGeneric;
                    bool lockedcrate = false;
                    bool emptycrate = false;
                    CollectibleObject inslot = null;
                    if (bettercratelockingslot != null && bettercratelockingslot[0] != null && bettercratelockingslot[0].Itemstack != null)
                    {
                        lockedcrate = true;
                        inslot = bettercratelockingslot[0].Itemstack.Collectible;
                    }
                    else if (cont.Inventory.Empty) { emptycrate = true; }
                    else { inslot = cont.Inventory[0].Itemstack.Collectible; }
                    //case one - filtered or not empty - add first slot with space to priority list
                    if (lockedcrate||!emptycrate)
                    {
                        foreach (ItemSlot crateslot in cont.Inventory)
                        {
                            if (crateslot.StackSize < inslot.MaxStackSize)
                            {
                                if (priorityslots.ContainsKey(inslot))
                                {
                                    priorityslots[inslot].Add(crateslot);
                                    slotreference[crateslot] = cont;
                                    break;
                                }
                                else
                                {
                                    priorityslots[inslot]= new List<ItemSlot> { crateslot };
                                    slotreference[crateslot] = cont;
                                    break;
                                }
                            }
                        }
                    }
                    //If create is empty and unfiltered then we add first slot to empty slots
                    else if (emptycrate)
                    {
                        emptyslots.Add(cont.Inventory[0]);
                        slotreference[cont.Inventory[0]] = cont;
                    }
                    ///*** ADD NONE EMPTY CRATE CODE ***

                }
                //NOT A BETTER CRATE So check if it's another crate and deal with it accordingly
                else if (SupportedCrates.Contains(b.EntityClass))
                {
                    //add to empty list if empty
                    if (cont.Inventory.Empty)
                    {
                        emptyslots.Add(cont.Inventory[0]);
                        slotreference[cont.Inventory[0]] = cont;
                    }
                    else
                    {
                        //use the contents of the first slot to set what this crate should contain
                        CollectibleObject inslot = cont.Inventory[0].Itemstack.Collectible;
                        foreach (ItemSlot crateslot in cont.Inventory)
                        {
                            //if (crateslot.Itemstack == null || crateslot.Itemstack.Collectible == null) { continue; }
                            if (crateslot.StackSize < inslot.MaxStackSize)
                            {
                                if (priorityslots.ContainsKey(inslot))
                                {
                                    priorityslots[inslot].Add(crateslot);
                                    slotreference[crateslot] = cont;
                                    break;
                                }
                                else
                                {
                                    priorityslots[inslot] = new List<ItemSlot> { crateslot };
                                    slotreference[crateslot] = cont;
                                    break;
                                }
                            }
                        }
                    }
                }
                //last of all deal with chests - slot by slot
                else if (supportedChests.Contains(b.EntityClass))
                {
                    foreach (ItemSlot slot in cont.Inventory)
                    {
                        if (slot == null || slot.Inventory == null) { continue; }
                        //add empty slots
                        if (slot.Empty || slot.Itemstack == null)
                        {
                            emptyslots.Add(slot);
                            slotreference[slot] = cont;
                        }
                        //ignore full slots
                        else if (slot.Itemstack.StackSize >= slot.Itemstack.Collectible.MaxStackSize) { continue; }
                        //this is a filled slot with space so add it
                        else { populatedslots.Add(slot); slotreference[slot] = cont; }
                    }
                    
                }
    
            }
            
            //NEXT CYCLE THRU OWN STACKS AND DISTRIBUTE
            //  *Note we only do one transfer operation per tick, so the first successful one gets done then it returns
            foreach (ItemSlot ownslot in Inventory)
            {
                //skip empty slots
                if (ownslot==null||ownslot.Itemstack==null|| ownslot.Empty) { continue; }
                CollectibleObject myitem = ownslot.Itemstack.Collectible;
                
                
                //start trying to find an empty slot
                ItemSlot outputslot = null;
                if (priorityslots.ContainsKey(myitem))
                {
                    if (priorityslots[myitem] != null)
                    {
                        outputslot = priorityslots[myitem][0]; //grab the first viable slot (anything in here should be legit)
                    }
                }
                //we didn't find anything in a priority slot, next check for other populated slots to fill in
                if (outputslot == null)
                {
                    outputslot = populatedslots.FirstOrDefault(x => (x.Itemstack != null) && (x.Itemstack.Collectible == myitem));

                }
                //if we didn't anything still, try and find an empty slot
                if (outputslot == null)
                {
                    if (emptyslots == null || emptyslots.Count == 0) { continue; } //we found nothing for this object
                    outputslot = emptyslots[0];
                }

                //Finally we can attempt to transfer some inventory and then return out of function if sucessful (or move ot next stack)
                int startamt = ownslot.StackSize;
                ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, Math.Min(ownslot.StackSize,maxTransferPerTick));
                int rem = ownslot.TryPutInto(outputslot, ref op);
                if (ownslot.StackSize != startamt)
                {

                    if (rem == 0) { ownslot.Itemstack = null; }
                    MarkDirty(false);
                    outputslot.MarkDirty();
                    slotreference[outputslot].MarkDirty(true);

                    return;
                }
                
            }
        }

        //add a container to the list of managed containers (usually called by a storage linker)
        public void AddContainer(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            //don't want to link to ourself!
            if (blockSel.Position == this.Pos) { return; }
            //check for valid container
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            BlockEntityContainer cont = Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityContainer;
            if (cont == null) { return; }
            int blockdistance = (int)(blockSel.Position.AsVec3i.DistanceTo(Pos.AsVec3i));
            if (blockdistance > maxRange) { return; }
            //if container isn't on list then add it
            if (containerlist==null) { containerlist = new List<BlockPos>();}
            if (!containerlist.Contains(blockSel.Position)) {
                if (Api is ICoreServerAPI)
                {
                    
                    containerlist.Add(blockSel.Position); MarkDirty();
                    Api.World.HighlightBlocks(byPlayer, 1, containerlist);
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byPlayer);
                    MarkDirty();
                }
            }
            
        }

        public void RemoveContainer(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (containerlist==null) { return; }

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (containerlist.Contains(blockSel.Position)) {
                containerlist.Remove(blockSel.Position);
                Api.World.HighlightBlocks(byPlayer, 1, containerlist);
                Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byPlayer);
                MarkDirty();
            }
            
        }
        bool showingblocks = false;
        public static int highlightid = 1;
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            //if (Api is ICoreClientAPI && containerlist!=null && containerlist.Count>0)
            //{
            //    ICoreAPI capi= Api as ICoreClientAPI;

                showingblocks = !showingblocks;
                if (showingblocks)
                {
                    Api.World.HighlightBlocks(byPlayer, 1, containerlist);
                //Api.World.HighlightBlocks(byPlayer, highlightid, containerlist, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
                }
                else
                {
                Api.World.HighlightBlocks(byPlayer, 1, new List<BlockPos>());
            }
                //else
                //{
               //     Api.World.HighlightBlocks(byPlayer, 0, containerlist, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
                //}
            //}
            return base.OnPlayerRightClick(byPlayer, blockSel);
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

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendLine("Range: "+MaxRange);
            if (MaxTransferPerTick <= 512)
            {
                dsc.AppendLine("Transfer Speed: " + MaxTransferPerTick + " Items at a time.");
            }
            else { dsc.AppendLine("Transfers full Stacks at a time"); }
            if (!(containerlist==null)&& containerlist.Count > 0)
            {
                dsc.AppendLine("Linked to "+containerlist.Count+" containers.");
            }
            else
            {
                dsc.AppendLine("Not linked to any containers");
            }

        }
    }
}
