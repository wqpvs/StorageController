using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Newtonsoft.Json;
using System.Reflection;
using Vintagestory.API.Util;
using System.IO;

namespace storagecontroller
{
    public class StorageControllerMaster : BlockEntityGenericTypedContainer
    {
        /// <summary>
        /// TODO Bugs
        /// CRATES - won't fill up crates properly with same item
        /// </summary>
        public static string containerlistkey = "containerlist";

        List<BlockPos> containerlist;
        public List<BlockPos> ContainerList => containerlist;
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
        //bool dopruning = false; //should invalid locations be moved every time?

        private GUIDialogStorageAccess clientDialog;

        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        internal StorageVirtualInv storageVirtualInv;
        public virtual StorageVirtualInv StorageVirtualInv => storageVirtualInv;

        public List<ItemStack> ListStacks;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            supportedChests = new List<string> { "GenericTypedContainer", "BEGenericSortableTypedContainer", "BESortableLabeledChest", "LabeledChest", "StorageControllerMaster" };
            supportedCrates = new List<string> { "BBetterCrate", "BEBetterCrate", "Crate" };
            if (Block.Attributes != null)
            {
                maxTransferPerTick = Block.Attributes["maxTransferPerTick"].AsInt(maxTransferPerTick);
                maxRange = Block.Attributes["maxRange"].AsInt(maxRange);
                tickTime = Block.Attributes["tickTime"].AsInt(TickTime);
            }

            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, TickTime); sapi = api as ICoreServerAPI; }
            else if (Api is ICoreClientAPI) { capi = api as ICoreClientAPI; }
        }
        //Better crates: BBetterCrate, BEBetterCrate, Crate, GenericTypedContainer

        //stuff to do every so often
        public void OnServerTick(float dt)
        {
            try
            {
                //Check if we have any inventory to bother with
                if (this.Inventory == null || this.Inventory.Empty) 
                { 
                    return;
                }

                if (containerlist == null || containerlist.Count == 0 || SupportedChests == null) 
                { 
                    return;
                }

                //Manage linked container list
                //// - only check so many blocks per tick
                //List<BlockPos> prunelist = new List<BlockPos>(); //This is a list of invalid blockpos that should be deleted from list


                //foreach (BlockPos pos in containerlist)
                //{

                //    if (pos == null || Api == null || Api.World == null || Api.World.BlockAccessor == null) { continue; }
                //    if (!IsInRange(pos)) { prunelist.Add(pos); continue; }
                //    Block b = Api.World.BlockAccessor.GetBlock(pos);
                //    if (b == null || b.EntityClass == null) { prunelist.Add(pos); continue; }
                //    if (!(SupportedChests.Contains(b.EntityClass) || SupportedCrates.Contains(b.EntityClass)))
                //    {
                //        prunelist.Add(pos); continue;
                //    }
                //    BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
                //    if (be == null || !(be is BlockEntityContainer)) { prunelist.Add(pos); continue; }
                //}
                //int removecount = 0;

                //if (dopruning)
                //{
                //    foreach (BlockPos pos in prunelist)
                //    {
                //        if (pos == null) { continue; }
                //        removecount += containerlist.RemoveAll(x => x.Equals(pos));

                //    }
                //    if (removecount > 0) { MarkDirty(); }
                //}
                //if (containerlist == null || containerlist.Count == 0) { return; }

                //Priority slots: filtered crate slots with space, other populated crates with space
                Dictionary<ItemStack, List<ItemSlot>> priorityslots = new Dictionary<ItemStack, List<ItemSlot>>();
                //Partial Chest slots - slots in chests that have space
                List<ItemSlot> populatedslots = new List<ItemSlot>();
                //Empty slots with nothing, including the first slot of an empty, unfiltered crate
                List<ItemSlot> emptyslots = new List<ItemSlot>();
                //This slotreference is to match the slot back up to its originating container
                Dictionary<ItemSlot, BlockEntityContainer> slotreference = new Dictionary<ItemSlot, BlockEntityContainer>();
                //Cycle thru our blocks to find containers we can use
                foreach (BlockPos pos in containerlist)
                {
                    if (pos == null) { continue; }
                    BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(pos);
                    Block block = Api.World.BlockAccessor.GetBlock(pos);
                    BlockEntityContainer blockEntityContainer = blockEntity as BlockEntityContainer;

                    if (blockEntity == null || blockEntityContainer == null || blockEntityContainer.Inventory == null) { continue; }//this shouldn't happen, but better to check
                                                                                           //find better crates
                    FieldInfo bettercratelock = blockEntity.GetType().GetField("lockedItemInventory");
                    //ah, we have discovered a better crate!
                    //HANDLE BETTER CRATES
                    if (bettercratelock != null)
                    {
                        var bettercratelockingslot = bettercratelock.GetValue(blockEntity) as InventoryGeneric;
                        bool lockedcrate = false;
                        bool emptycrate = false;
                        ItemStack inslot = null;
                        //if there's a valid lock set to inslot
                        if (bettercratelockingslot != null && bettercratelockingslot[0] != null && bettercratelockingslot[0].Itemstack != null)
                        {
                            lockedcrate = true;
                            inslot = bettercratelockingslot[0].Itemstack.GetEmptyClone();
                        }
                        else if (blockEntityContainer.Inventory == null || blockEntityContainer.Inventory.Empty) { emptycrate = true; }
                        else if (blockEntityContainer.Inventory[0] == null || blockEntityContainer.Inventory[0].Itemstack == null) { emptycrate = true; }//Hmmm this is an odd situation
                        else { inslot = blockEntityContainer.Inventory[0].Itemstack.GetEmptyClone(); } //otherwise set inslot to the first item in crate
                        //case one - filtered or not empty - add first slot with space to priority list
                        if (lockedcrate || !emptycrate)
                        {
                            foreach (ItemSlot crateslot in blockEntityContainer.Inventory)
                            {
                                if (crateslot.StackSize < inslot.Collectible.MaxStackSize)
                                {
                                    if (priorityslots.ContainsKey(inslot))
                                    {
                                        priorityslots[inslot].Add(crateslot);
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                    else
                                    {
                                        priorityslots[inslot] = new List<ItemSlot> { crateslot };
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                }
                            }
                        }
                        //If create is empty and unfiltered then we add first slot to empty slots
                        else if (emptycrate)
                        {
                            emptyslots.Add(blockEntityContainer.Inventory[0]);
                            slotreference[blockEntityContainer.Inventory[0]] = blockEntityContainer;
                        }
                        ///*** ADD NONE EMPTY CRATE CODE ***

                    }
                    //NOT A BETTER CRATE So check if it's another crate and deal with it accordingly
                    else if (SupportedCrates.Contains(block.EntityClass))
                    {
                        //add to empty list if empty
                        if (blockEntityContainer.Inventory.Empty)
                        {
                            emptyslots.Add(blockEntityContainer.Inventory[0]);
                            slotreference[blockEntityContainer.Inventory[0]] = blockEntityContainer;
                        }
                        else
                        {
                            //use the contents of the first slot to set what this crate should contain
                            ItemStack inslot = blockEntityContainer.Inventory[0].Itemstack.GetEmptyClone();
                            foreach (ItemSlot crateslot in blockEntityContainer.Inventory)
                            {
                                //if (crateslot.Itemstack == null || crateslot.Itemstack.Collectible == null) { continue; }
                                if (crateslot.StackSize < inslot.Collectible.MaxStackSize)
                                {
                                    if (priorityslots.ContainsKey(inslot))
                                    {
                                        priorityslots[inslot].Add(crateslot);
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                    else
                                    {
                                        priorityslots[inslot] = new List<ItemSlot> { crateslot };
                                        slotreference[crateslot] = blockEntityContainer;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    //last of all deal with chests - slot by slot
                    else if (supportedChests.Contains(block.EntityClass))
                    {
                        foreach (ItemSlot slot in blockEntityContainer.Inventory)
                        {
                            if (slot == null || slot.Inventory == null) { continue; }
                            //add empty slots
                            if (slot.Empty || slot.Itemstack == null)
                            {
                                emptyslots.Add(slot);
                                slotreference[slot] = blockEntityContainer;
                            }
                            //ignore full slots
                            else if (slot.Itemstack.StackSize >= slot.Itemstack.Collectible.MaxStackSize) { continue; }
                            //this is a filled slot with space so add it
                            else { populatedslots.Add(slot); slotreference[slot] = blockEntityContainer; }
                        }

                    }

                }

                //NEXT CYCLE THRU OWN STACKS AND DISTRIBUTE
                //  *Note we only do one transfer operation per tick, so the first successful one gets done then it returns
                foreach (ItemSlot ownslot in Inventory)
                {
                    //skip empty slots
                    if (ownslot == null || ownslot.Itemstack == null || ownslot.Empty) { continue; }
                    ItemStack myitem = ownslot.Itemstack.GetEmptyClone();
                    if (myitem == null) { continue; }

                    //start trying to find an empty slot
                    ItemSlot outputslot = null;
                    if (priorityslots != null)
                    {
                        List<ItemSlot> foundslots = priorityslots.FirstOrDefault(x => x.Key.Satisfies(myitem)).Value;
                        if (foundslots != null && foundslots.Count > 0)
                        {
                            outputslot = foundslots[0];
                        }
                    }
                    //we didn't find anything in a priority slot, next check for other populated slots to fill in
                    if (outputslot == null)
                    {
                        outputslot = populatedslots.FirstOrDefault(x => (x.Itemstack != null) && (x.Itemstack.Satisfies(myitem)));

                    }
                    //if we didn't anything still, try and find an empty slot
                    if (outputslot == null)
                    {
                        if (emptyslots == null || emptyslots.Count == 0 || emptyslots[0] == null) { continue; } //we found nothing for this object
                        outputslot = emptyslots[0];
                    }

                    //Finally we can attempt to transfer some inventory and then return out of function if sucessful (or move ot next stack)
                    int startamt = ownslot.StackSize;
                    ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, Math.Min(ownslot.StackSize, maxTransferPerTick));
                    int rem = ownslot.TryPutInto(outputslot, ref op);
                    if (ownslot.StackSize != startamt)
                    {

                        if (rem == 0) { ownslot.Itemstack = null; }
                        MarkDirty(false);
                        outputslot.MarkDirty();
                        if (slotreference[outputslot] != null)
                        {
                            slotreference[outputslot].MarkDirty(true);
                        }

                        return;
                    }

                }
            }
            catch (Exception)
            {

            }
        }
        public bool IsInRange(BlockPos checkpos)
        {
            int xdiff = Math.Abs(Pos.X - checkpos.X);
            if (xdiff >= MaxRange) { return false; }
            int ydiff = Math.Abs(Pos.Y - checkpos.Y);
            if (ydiff >= MaxRange) { return false; }
            int zdiff = Math.Abs(Pos.Z - checkpos.Z);
            if (zdiff >= MaxRange) { return false; }
            return true;
        }
        //add a container to the list of managed containers (usually called by a storage linker)
        public void AddContainer(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            //don't want to link to ourself!
            if (blockSel.Position == Pos) { return; }
            //check for valid container
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            BlockEntityContainer cont = Api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityContainer;
            if (cont == null) { return; }
            if (!IsInRange(blockSel.Position)) { return; }
            //int blockdistance = (int)(blockSel.Position.AsVec3i.ManhattenDistanceTo(Pos.AsVec3i));
            //if (blockdistance > MaxRange) { return; }
            //if container isn't on list then add it
            if (containerlist == null) { containerlist = new List<BlockPos>(); }
            if (!containerlist.Contains(blockSel.Position))
            {
                if (Api is ICoreServerAPI)
                {
                    showingblocks = true;
                    containerlist.Add(blockSel.Position); MarkDirty();
                    Api.World.HighlightBlocks(byPlayer, 1, containerlist);
                    Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byPlayer);
                    MarkDirty();
                }
            }

        }
        //Remove a Container Location from the list
        public void RemoveContainer(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (containerlist == null) { return; }

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (containerlist.Contains(blockSel.Position))
            {
                containerlist.Remove(blockSel.Position);
                showingblocks = true;
                Api.World.HighlightBlocks(byPlayer, 1, containerlist);
                Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byPlayer);
                MarkDirty();
            }

        }
        bool showingblocks = false;
        public static int highlightid = 1;

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {

            if (Api.Side == EnumAppSide.Client)
            {
                SetVirtualInventory();
                toggleInventoryDialogClient(byPlayer, delegate
                {
                    clientDialog = new GUIDialogStorageAccess(DialogTitle, Inventory, StorageVirtualInv, Pos, Api as ICoreClientAPI);
                    return clientDialog;
                });
            }

            return true;

            //if (byPlayer.InventoryManager.ActiveHotbarSlot != null && !byPlayer.InventoryManager.ActiveHotbarSlot.Empty && byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item != null)
            //{
            //    Item activeitem = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item;
            //    if (activeitem == null || activeitem.Attributes == null)
            //    {
            //        return base.OnPlayerRightClick(byPlayer, blockSel);
            //    }
            //    string[] upgradesfrom = activeitem.Attributes["upgradesfrom"].AsArray<string>();
            //    if (upgradesfrom == null) { return base.OnPlayerRightClick(byPlayer, blockSel); }
            //    string mymetal = Block.Code.FirstCodePart();
            //    if (!(upgradesfrom.Contains(mymetal))) { return base.OnPlayerRightClick(byPlayer, blockSel); }
            //    string upgradesto = activeitem.Attributes["upgradesto"].AsString("");
            //    if (upgradesto == "") { return base.OnPlayerRightClick(byPlayer, blockSel); }
            //    upgradesto += "-" + Block.LastCodePart();
            //    //ok this is a valid upgrade
            //    Block upgradedblock = Api.World.GetBlock(new AssetLocation(upgradesto));
            //    Api.World.BlockAccessor.SetBlock(upgradedblock.BlockId, Pos);
            //    StorageControllerMaster newmaster = Api.World.BlockAccessor.GetBlockEntity(Pos) as StorageControllerMaster;
            //    newmaster.SetContainers(ContainerList);
            //    Inventory.DropAll(Pos.ToVec3d());
            //    if (byPlayer?.WorldData.CurrentGameMode != EnumGameMode.Creative)
            //    {
            //        byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize--;
            //        if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.StackSize == 0)
            //        {
            //            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack = null;
            //        }
            //        byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            //    }
            //}
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            var asString = tree.GetString(containerlistkey);
            if (asString != null)
            {
                containerlist = JsonConvert.DeserializeObject<List<BlockPos>>(asString);
            }
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (byItemStack != null)
            {
                if (byItemStack.Attributes.HasAttribute(containerlistkey))
                {
                    byte[] savedlistdata = byItemStack.Attributes.GetBytes(containerlistkey);
                    if (savedlistdata != null)
                    {
                        List<BlockPos> savedcontainerlist = SerializerUtil.Deserialize<List<BlockPos>>(savedlistdata);
                        if (savedcontainerlist != null)
                        {
                            containerlist = new List<BlockPos>(savedcontainerlist);
                            if (Api is ICoreServerAPI) { MarkDirty(true); }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Set a new list of containers (as block positions where those containers should be)
        /// </summary>
        /// <param name="newlist"></param>
        public void SetContainers(List<BlockPos> newlist)
        {
            if (newlist != null)
            {
                containerlist = new List<BlockPos>(newlist);
            }
            else
            {
                containerlist = new List<BlockPos>();
            }
            MarkDirty();
        }

        /// <summary>
        /// Builds a giant virtual inventory of all populated, linked containers
        /// </summary>
        public virtual void SetVirtualInventory()
        {
            List<ItemStack> allItems = new List<ItemStack>();

            if (containerlist == null || containerlist.Count == 0)
            {
                storageVirtualInv = null;
                return;
            }

            // Iterate through each container in the list
            foreach (BlockPos pos in containerlist)
            {
                BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(pos);
                BlockEntityContainer container = be as BlockEntityContainer;

                if (container == null || container.Inventory == null || container.Inventory.Empty)
                {
                    continue;
                }

                // Iterate through each slot in the container's inventory
                foreach (ItemSlot slot in container.Inventory)
                {
                    // Check if the slot contains an item stack
                    if (!slot.Empty && slot.Itemstack != null && slot.StackSize > 0)
                    {
                        // Find an equivalent item stack in allItems
                        ItemStack existingStack = allItems.FirstOrDefault(x => x.Satisfies(slot.Itemstack));

                        if (existingStack == null)
                        {
                            // If not found, add a clone of the item stack to allItems
                            allItems.Add(slot.Itemstack.Clone());
                        }
                        else
                        {   // If found, update the stack size of the existing item stack
                            if (existingStack.Id.Equals(slot.Itemstack.Id))
                            {
                                existingStack.StackSize += slot.StackSize;
                            }
                        }
                    }
                }
            }

            // Sort the list of item stacks by name
            allItems.Sort((x, y) => x.GetName().CompareTo(y.GetName()));

            // Create a new StorageVirtualInv instance and populate it with the item stacks
            storageVirtualInv = new StorageVirtualInv(Api, allItems.Count);

            for (int i = 0; i < allItems.Count; i++)
            {   
                storageVirtualInv[i].Itemstack = allItems[i];
            }
        }

        public static int itemStackPacket = 320000;
        public static int clearInventoryPacket = 320001;
        public static int linkAllChestsPacket = 320002;
        public static int linkChestPacket = 320003;
        public static int InvOpenPacket = 320004;
        public static int InvClosePacket = 320005;

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            //How to handle taking multiple stacks?
            //just search and grab/relieve the first stack we find 

            if (packetid == itemStackPacket)
            {
                if (data == null) 
                {
                    (Api as ICoreClientAPI)?.SendChatMessage("Data return null", 0);
                    return;
                }

                ItemStack itemStack = new ItemStack(data).Clone();

                if (itemStack == null) 
                {
                    (Api as ICoreClientAPI)?.SendChatMessage("itemstack return null", 0);
                    return;
                }

                itemStack.ResolveBlockOrItem(Api.World);

                if (itemStack == null) { return; }

                // we got the stack now let's see if we can send it to the player

                int stacksize = GetStackOf(itemStack);

                if (stacksize == 0) { return; }

                itemStack.StackSize = stacksize;

                //no valid slot
                if (!player.InventoryManager.TryGiveItemstack(itemStack))
                {
                    Api.World.SpawnItemEntity(itemStack, player.Entity.Pos.XYZ);
                }

                return;
            }
            else if (packetid == clearInventoryPacket)
            {
                ClearConnections();
                return;
            }
            else if (packetid == linkAllChestsPacket)
            {

                LinkAll(enLinkTargets.ALL, player);
                return;
            }
            else if (packetid == linkChestPacket) //link a particular chest
            {
                BlockPos p = SerializerUtil.Deserialize<BlockPos>(data);
                if (p == null || p == Pos) { return; }

                if (containerlist == null) { containerlist = new List<BlockPos>(); }
                //do nothing if container in list
                if (containerlist.Contains(p)) { return; }
                //don't link if container is reinforced
                if (sapi.ModLoader.GetModSystem<ModSystemBlockReinforcement>()?.IsReinforced(p) == true)
                {
                    return;
                }
                //ensure player as access rights
                if (!player.Entity.World.Claims.TryAccess(player, p, EnumBlockAccessFlags.BuildOrBreak))
                {

                    return;
                }
                containerlist.Add(p);
                MarkDirty();
            }


            base.OnReceivedClientPacket(player, packetid, data);



        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);
        }

        //public float refreshIntervalSeconds = 2.5f; 

        //public float timeSinceLastRefreshSeconds = 0;

        //Method to handle the refresh logic
        //public void RefreshStorageInterface(float deltaTime)
        //{
        //    // Accumulate the elapsed time since the last refresh
        //    timeSinceLastRefreshSeconds += deltaTime;  

        //    if (timeSinceLastRefreshSeconds >= refreshIntervalSeconds) 
        //    { 
        //        SetVirtualInventory();
        //        timeSinceLastRefreshSeconds = 0;
        //    }

        //    clientDialog.RefreshGrid();
        //}

        // Register the game tick listener when the player enters the storage interface
        //public long storageInterfaceTickListenerId = -1; // Initialize listener ID with a default value

        // Register the game tick listener when the player enters the storage interface
        //public void OnPlayerEnterStorageInterface()
        //{
        //    storageInterfaceTickListenerId = Api.World.RegisterGameTickListener(RefreshStorageInterface, 200);
        //}

        // Unregister the game tick listener when the player exits the storage interface
        //public void OnPlayerExitStorageInterface()
        //{
        //    if (storageInterfaceTickListenerId != -1)
        //    {
        //        Api.World.UnregisterGameTickListener(storageInterfaceTickListenerId);
        //        storageInterfaceTickListenerId = -1; // Reset the listener ID
        //    }
        //}

        /// <summary>
        /// Attempts to find the item in the connected inventory, relieves it and returns the amount found
        /// </summary>
        /// <param name="findstack"></param>
        /// <returns></returns>
        public virtual int GetStackOf(ItemStack findstack)
        {
            int stacksize = 0;

            foreach (BlockPos blockPos in containerlist)
            {
                if (stacksize != 0) { break; }

                Block block = Api.World.BlockAccessor.GetBlock(blockPos);

                BlockEntityContainer blockEntityContainer = Api.World.BlockAccessor.GetBlockEntity(blockPos) as BlockEntityContainer;

                if (block == null || blockEntityContainer == null || blockEntityContainer.Inventory == null || blockEntityContainer.Inventory.Empty) continue;
                //search inventory of this container if it exists and isn't empty
                foreach (ItemSlot slot in blockEntityContainer.Inventory)
                {
                    if (slot == null || slot.Empty || slot.Itemstack == null || slot.StackSize == 0) { continue; }
                    //if we don't have one yet then add one

                    if (slot.Itemstack.Satisfies(findstack))
                    {
                        stacksize = slot.Itemstack.StackSize;
                        slot.Itemstack = null;
                        slot.MarkDirty();
                        blockEntityContainer.MarkDirty();
                        break;
                    }
                }
            }

            return stacksize;
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
            dsc.AppendLine("Range: " + MaxRange);
            if (MaxTransferPerTick <= 512)
            {
                dsc.AppendLine("Transfer Speed: " + MaxTransferPerTick + " Items at a time.");
            }
            else { dsc.AppendLine("Transfers full Stacks at a time"); }
            if (!(containerlist == null) && containerlist.Count > 0)
            {
                dsc.AppendLine("Linked to " + containerlist.Count + " containers.");
            }
            else
            {
                dsc.AppendLine("Not linked to any containers");
            }

        }

        /// <summary>
        /// removes all connections to this controller
        /// </summary>
        public void ClearConnections()
        {
            containerlist = new List<BlockPos>();
            MarkDirty(true);
        }

        /// <summary>
        /// Clears all highlighted blocks on client
        /// </summary>
        /// <param name="byPlayer"></param>
        public void ClearHighlighted()
        {
            if (Api is ICoreServerAPI) { return; }
            showingblocks = false;
            Api.World.HighlightBlocks(capi.World.Player, 1, new List<BlockPos>());
            Api.World.HighlightBlocks(capi.World.Player, 2, new List<BlockPos>());
        }

        public void ToggleHightlights()
        {
            if (Api is ICoreClientAPI)
            {
                if (showingblocks)
                {
                    ClearHighlighted();
                }
                else
                {
                    HighLightBlocks();
                }
            }
        }

        public void HighLightBlocks()
        {
            if (containerlist == null || containerlist.Count == 0 || Api is ICoreServerAPI) return;
            showingblocks = true;
            List<int> colors = new List<int>
            {
                ColorUtil.ColorFromRgba(0, 255, 0, 128)
            };

            Api.World.HighlightBlocks(capi.World.Player, 1, containerlist, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Arbitrary);

            List<BlockPos> range = new List<BlockPos>
            {
                new BlockPos(Pos.X - maxRange, Pos.Y - maxRange, Pos.Z - maxRange, 0)
            };

            colors[0] = ColorUtil.ColorFromRgba(255, 255, 0, 128);
            range.Add(new BlockPos(Pos.X + maxRange, Pos.Y + maxRange, Pos.Z + maxRange, 0));

            Api.World.HighlightBlocks(capi.World.Player, 2, range, colors, EnumHighlightBlocksMode.Absolute, EnumHighlightShape.Cube);
        }


        public enum enLinkTargets { ALL }
        /// <summary>
        /// Attempt to link all chests in range
        /// will build a list of valid chests
        /// Logic:
        /// - (CLIENT) WalkBlocks which calls LinkChestPos for each block basically
        /// - (CLIENT) LinkChestPos checks for chests and if it finds one sends a message to server for linking
        /// - (SERVER) On Receiving the packet (which encodes the blockpos), the server just ensures that the block
        ///            location is not reinforced or claimed, and if it's ok adds it to the list and then marks the list dirty
        /// </summary>
        /// <param name="targets"></param>
        public void LinkAll(enLinkTargets targets, IPlayer forplayer)
        {
            BlockPos startPos = Pos.Copy();
            startPos.X -= MaxRange;
            startPos.Y -= MaxRange;
            startPos.Z -= MaxRange;
            BlockPos endPos = Pos.Copy();
            endPos.X += MaxRange;
            endPos.Y += MaxRange;
            endPos.Z += MaxRange;

            Api.World.BlockAccessor.WalkBlocks(startPos, endPos, LinkChestPos, true);
        }

        /// <summary>
        /// Check supplied position for relevant blocks and 
        /// if linkable block found send request to link to the server
        /// </summary>
        /// <param name="toblock"></param>
        /// <param name="tox"></param>
        /// <param name="toy"></param>
        /// <param name="toz"></param>
        public void LinkChestPos(Block toblock, int tox, int toy, int toz)
        {

            if (capi == null) { return; }
            if (toblock == null) { return; }
            if (toblock.EntityClass == null) { return; }

            if (toblock.EntityClass != "StorageControllerMaster" && !SupportedChests.Contains(toblock.EntityClass) && !SupportedCrates.Contains(toblock.EntityClass)) { return; }
            BlockPos p = new BlockPos(tox, toy, toz, 0);
            byte[] data = SerializerUtil.Serialize<BlockPos>(p);
            capi.Network.SendBlockEntityPacket(Pos, linkChestPacket, data);

        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            //Api.World.UnregisterGameTickListener(storageInterfaceTickListenerId);

            if (clientDialog?.IsOpened() ?? false)
            {
                clientDialog?.TryClose();
            }

            clientDialog?.Dispose();
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            ClearConnections();
            ClearHighlighted();

            //Api.World.UnregisterGameTickListener(storageInterfaceTickListenerId);

            if (clientDialog?.IsOpened() ?? false)
            {
                clientDialog?.TryClose();
            }

            clientDialog?.Dispose();
        }
    }
}