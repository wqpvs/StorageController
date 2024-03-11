using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System;
using System.Linq;

namespace storagecontroller
{
    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        ICoreClientAPI clientAPI;

        ElementBounds mainDialogBound;

        ElementBounds gridSlots;

        private byte[] data { get; set; }

        public StorageMasterInv StorageMasterInv;

        protected int curTab;

        StorageControllerMaster storageControllerMaster;
        public GUIDialogStorageAccess(string dialogTitle, StorageControllerMaster ownBlock, StorageMasterInv storageMasterInv, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, storageMasterInv, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }

            clientAPI = capi;   

            curTab = 0;

            StorageMasterInv = storageMasterInv;

            storageControllerMaster = ownBlock;

            SetupDialog();
        }


        public void SetupDialog()
        {
            ComposersDialog();
        }

        public string storageCompKey => "storageCompo";
        public string optionCompKey => "storageOptionCompo";
        public string searchComkey => "searchCompo";

        protected void ComposersDialog()
        {

            ElementBounds element = ElementBounds.Fixed(0, 0, 500, 400);
            ElementBounds buttonlist = ElementBounds.Fixed(0, 0, -200, -390).FixedUnder(element, 10);
            gridSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, 10)
                .FixedUnder(element, 10)
                .WithFixedPosition(-5, 25)
                .WithFixedPadding(2, 2);

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

            ElementBounds button4 = ElementBounds.Fixed(415, -10) //Highlight
                .FixedUnder(button3, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button5 = ElementBounds.Fixed(450, -10) //Highlight
                .FixedUnder(button4, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);


            ElementBounds button6 = ElementBounds.Fixed(0, -10) //Highlight
                .FixedUnder(button5, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedHeight(10)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds7.BothSizing = ElementSizing.FitToChildren;
            elementBounds7.WithChildren(element, buttonlist, gridSlots);

            mainDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
            Composers[optionCompKey] =
                capi.Gui
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

            GuiComposer storagecompKey = Composers[storageCompKey] = capi.Gui.CreateCompo(storageCompKey, mainDialogBound);

            if (StorageMasterInv != null && !StorageMasterInv.Empty)
            {
                storagecompKey
                .AddItemSlotGrid(StorageMasterInv, SendPacket, 10, gridSlots, "slotgrid")
                .AddInset(gridSlots, 10, 0.7f);

                curTab = 1;

                //Fixed number are slot showed
                int slotsCount = StorageMasterInv?.Count ?? 0; // Number of slots in virtualInventory, or 0 if virtualInventory is null
                Composers[storageCompKey].GetSlotGrid("slotgrid")
                    .DetermineAvailableSlots(Enumerable.Range(0, Math.Min(100, slotsCount)).ToArray());
            }
            else 
            {
                storagecompKey
                .AddInset(gridSlots, 10, 0.7f);

            }


            storagecompKey.Compose();
            Composers[optionCompKey].Compose();
        }

        private void SendPacket(object packet)
        {
            IClientPlayer byPlayer = capi.World.Player;

            data = byPlayer.InventoryManager?.MouseItemSlot?.Itemstack?.ToBytes();
            if (data != null)
            {
                byPlayer.InventoryManager.MouseItemSlot.Itemstack = null;
                clientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.inventoryPacket, data);
            }


        }

        private void GridPage()
        {
            int slots = StorageMasterInv?.Count ?? 0;
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
            int slots = StorageMasterInv?.Count ?? 0;
            int maxTabs = (int)Math.Ceiling((double) slots / 100); // Calculate the maximum number of tabs
            curTab = Math.Min(maxTabs, curTab + 1); // Increase curTab by 1, but ensure it doesn't exceed the maximum
            GridPage(); 
            return true;
        }

        private bool OnClickClearAll()
        {
            clientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.clearInventoryPacket, null);
            storageControllerMaster?.ClearHighlighted();
            TryClose();
            return true;
        }

        private bool OnClickLinkAllChests()
        {
            storageControllerMaster?.LinkAll(StorageControllerMaster.enLinkTargets.ALL, clientAPI.World.Player);
            TryClose();
            return true;
        }
        private bool OnClickHighlightAttached()
        {
            if (storageControllerMaster != null)
            {
                storageControllerMaster.ToggleHightlights();
            }

            return true;
        }

    }
}
