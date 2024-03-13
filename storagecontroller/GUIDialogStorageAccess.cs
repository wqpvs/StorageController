using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System;
using System.Linq;
using Vintagestory.GameContent;
using HarmonyLib;
using Vintagestory.API.Datastructures;
using System.Xml.Linq;

namespace storagecontroller
{
    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        private ICoreClientAPI coreClientAPI;

        public ElementBounds mainDialogBound;

        public ElementBounds gridSlots;

        public ElementBounds mainElement;

        public int number => StorageVirtualInv.Count;

        private byte[] data => capi.World.Player.InventoryManager?.CurrentHoveredSlot?.Itemstack?.ToBytes();

        public StorageVirtualInv StorageVirtualInv;

        public InventoryBase InventoryBase;

        public StorageControllerMaster StorageControllerMaster;

        protected int curTab = 0;

        public GUIDialogStorageAccess(string dialogTitle, InventoryBase Inventory, StorageVirtualInv storageVirtualInv, BlockPos BlockEntityPosition, ICoreClientAPI capi)
             : base(dialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            InventoryBase = Inventory;

            coreClientAPI = capi;

            if (capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) is StorageControllerMaster storageControllerMaster) 
            {
                StorageControllerMaster = storageControllerMaster;
            }

            StorageVirtualInv = storageVirtualInv;

            SetupDialog();
        }

        public void SetupDialog()
        {
            ComposersDialog();
        }

        public string storageCompKey => "storageCompo";
        public string optionCompKey => "storageOptionCompo";
        public string sideSlotsComkey => "sideSlots";

        protected void ComposersDialog()
        {
            //Main Element
            mainElement = ElementBounds.Fixed(0, 0, 500, 400);

            ElementBounds buttonlist = ElementBounds.Fixed(0, 0, -200, -390).FixedUnder(mainElement, 10);

            gridSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, 10)
           .FixedUnder(mainElement, 10)
           .WithFixedPosition(0, 30);

            ElementBounds button1 = ElementBounds.Fixed(100, -25) //Clear All
                .FixedUnder(buttonlist, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button2 = ElementBounds.Fixed(185, -10) //Link All Chests
                .FixedUnder(button1, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button3 = ElementBounds.Fixed(312, -10) //Highlight
                .FixedUnder(button2, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button4 = ElementBounds.Fixed(415, -10) //PreviousGrid
                .FixedUnder(button3, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button5 = ElementBounds.Fixed(450, -10) //NextGrid
                .FixedUnder(button4, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);


            ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds7.BothSizing = ElementSizing.FitToChildren;
            elementBounds7.WithChildren(mainElement, buttonlist, gridSlots);

            //Main Gui
            mainDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(15.0 - GuiStyle.DialogToScreenPadding, 0.0);
            Composers[optionCompKey] =
                coreClientAPI.Gui
                .CreateCompo(optionCompKey, mainDialogBound)
                .AddShadedDialogBG(elementBounds7)
                .AddDialogTitleBar("Controls", CloseIconPressed)
                .AddButton("Clear All", () => { return OnClickClearAll(); }, button1, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "clearactive")
                .AddAutoSizeHoverText("Clears all connections from controller", CairoFont.WhiteSmallText(), 300, button1)
                .AddButton("Link All Chests", () => { return OnClickLinkAllChests(); }, button2, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "linkbutton")
                .AddAutoSizeHoverText("Links all chests in range", CairoFont.WhiteSmallText(), 300, button2)
                .AddButton("Highlight", () => { return OnClickHighlightAttached(); }, button3, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "highlightbutton")
                .AddAutoSizeHoverText("Highlight linked containers and range", CairoFont.WhiteSmallText(), 300, button3)
                .AddButton("<", PreviousGrid, button4, CairoFont.WhiteSmallText(), EnumButtonStyle.Small, "prevGrid")
                .AddButton(">", NextGrid, button5, CairoFont.WhiteSmallText(), EnumButtonStyle.Small, "nextGrid")
                .Compose();
        
            //Input Slots
            ElementBounds inputDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedPosition(570, -120).FixedUnder(mainElement).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

            ElementBounds inputslots = ElementStdBounds.SlotGrid(EnumDialogArea.None ,0, 0, 2, 4) //Highlight
                .WithFixedPosition(0, 50);

            ElementBounds elementBounds8 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds8.BothSizing = ElementSizing.FitToChildren;
            elementBounds8.WithChildren(inputslots);


            Composers[sideSlotsComkey] = coreClientAPI.Gui.CreateCompo(sideSlotsComkey, inputDialogBound).AddShadedDialogBG(elementBounds8)
                .AddDialogTitleBar("Input", CloseIconPressed)
                .AddItemSlotGrid(InventoryBase, SendInvPacket, 2, new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 }, inputslots, "inputslotgrid")
                .AddInset(inputslots, 10, 0.7f)
                .Compose();


            GridSlots();
        }


        public void GridSlots()
        {
            //SlotsGrid
            GuiComposer storagecompKey = Composers[storageCompKey] = coreClientAPI.Gui.CreateCompo(storageCompKey, mainDialogBound);

            if (StorageVirtualInv != null && !StorageVirtualInv.Empty)
            {
                storagecompKey
                .AddItemSlotGrid(StorageVirtualInv, SendVirtualPacket, 10, gridSlots, "slotgrid")
                .AddInset(gridSlots, 10, 0.7f)
                .Compose();
               
                curTab = 1;

                //Fixed number are slot showed

                int slotsCount = StorageVirtualInv?.Count ?? 0; // Number of slots in virtualInventory, or 0 if virtualInventory is null
                Composers[storageCompKey].GetSlotGrid("slotgrid")
                    .DetermineAvailableSlots(Enumerable.Range(0, Math.Min(100, slotsCount)).ToArray());
            }
            else
            {
                storagecompKey.AddInset(gridSlots, 10, 0.7f)
                .Compose();
            }

            storagecompKey.Compose();
        }

        public override void OnGuiClosed()
        {
            StorageControllerMaster?.OnPlayerExitStorageInterface();
     
            capi.Gui.PlaySound(CloseSound, randomizePitch: true);

            base.OnGuiClosed();
        }

        public override void OnGuiOpened()
        {
            StorageControllerMaster?.OnPlayerEnterStorageInterface();

            capi.Gui.PlaySound(OpenSound, randomizePitch: true);

            base.OnGuiOpened();
        }

        private void SendInvPacket(object obj)
        {
            coreClientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, obj);
        }

        private void SendVirtualPacket(object packet)
        {
            coreClientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.inventoryPacket, data);
        }

        
        public void RefreshGrid()
        {
            // Set the virtual inventory
            StorageControllerMaster.SetVirtualInventory();

            var storageVirtualInv = StorageControllerMaster.StorageVirtualInv;

            if (storageVirtualInv == null)
            {
                // Handle the case where the storage virtual inventory is null
                if (StorageVirtualInv != null)
                {
                    // Clear the virtual inventory
                    StorageVirtualInv.Clear();
                    // Refresh the grid display
                    GridSlots();
                }

                return; // Exit the method
            }

            // Check if the storage virtual inventory has been initialized but is empty
            if (StorageVirtualInv?.FirstNonEmptySlot == null)
            {
                // Update the storage virtual inventory
                StorageVirtualInv = storageVirtualInv;
                // Refresh the grid display
                GridSlots();
                return;
            }
            else if (storageVirtualInv.Count < 0)
            {
                // Handle the case where the virtual inventory count is negative (an error condition?)
                GridSlots();
                return;
            }

            // Get the previous count
            int previousCount = StorageVirtualInv?.Count ?? 0;

            // Update the storage virtual inventory
            StorageVirtualInv = StorageControllerMaster.StorageVirtualInv;

            // Get the current count after updating
            int currentCount = StorageVirtualInv.Count;

            // Check if there's been a change in the number of items
            if (currentCount != previousCount)
            {
                // Refresh the grid display
                GridSlots();
            }
        }

        private void GridPage()
        {

            int slots = StorageVirtualInv?.Count ?? 0;
            int startIndex = (curTab - 1) * 100; // Calculate the starting slot index
            startIndex = Math.Max(0, startIndex); // Ensure startIndex doesn't go below 0

            var compKey = Composers[storageCompKey];

            if (compKey.GetSlotGrid("slotgrid") != null)
            {
                int remainingSlots = Math.Max(0, slots - startIndex); // Calculate the number of slots remaining after startIndex
                int minSlots = Math.Min(100, remainingSlots); // Determine the number of slots to display, capped at 100
                Composers[storageCompKey].GetSlotGrid("slotgrid")
                    .DetermineAvailableSlots(Enumerable.Range(startIndex, minSlots).ToArray());
            }


            compKey.Compose();
        }

        private bool PreviousGrid()
        {
            curTab = Math.Max(1, curTab - 1); // Decrease curTab by 1, but ensure it doesn't go below 1
            GridPage();
            return true;
        }

        private bool NextGrid()
        {
            int slots = StorageVirtualInv?.Count ?? 0;
            int maxTabs = (int)Math.Ceiling((double) slots / 100); // Calculate the maximum number of tabs
            curTab = Math.Min(maxTabs, curTab + 1); // Increase curTab by 1, but ensure it doesn't exceed the maximum
            GridPage(); 
            return true;
        }

        private bool OnClickClearAll()
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.clearInventoryPacket, null);
            StorageControllerMaster?.ClearHighlighted();
            TryClose();
            return true;
        }

        private bool OnClickLinkAllChests()
        {
            StorageControllerMaster?.LinkAll(StorageControllerMaster.enLinkTargets.ALL, capi.World.Player);
            TryClose();
            return true;
        }
        private bool OnClickHighlightAttached()
        {
            if (StorageControllerMaster != null)
            {
                StorageControllerMaster.ToggleHightlights();
            }

            return true;
        }

    }
}
