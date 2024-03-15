using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using System;
using System.Linq;
using Vintagestory.API.Config;

namespace storagecontroller
{
    public class GuiElementIcon : GuiElement
    {
        private readonly LoadedTexture texture;
        public ActionConsumable onClick;
        public Action<int> OnSlotOver;
        public ICoreClientAPI capi;
        public static double unscaledButtonSize = 21.0;

        public GuiElementIcon(ICoreClientAPI capi, LoadedTexture texture, ActionConsumable onClick, ElementBounds bounds) : base(capi, bounds)
        {
            this.texture = texture;
            this.onClick = onClick;
            this.capi = capi;

            // Calculate and set fixed height and width using unscaled values
            this.Bounds.fixedHeight = GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementIcon.unscaledButtonSize;
            this.Bounds.fixedWidth = GuiElementItemSlotGridBase.unscaledSlotPadding + GuiElementIcon.unscaledButtonSize;
        }

        public override void RenderInteractiveElements(float deltaTime)
        {
            double num = GuiElement.scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
            double num2 = GuiElement.scaled(GuiElementIcon.unscaledButtonSize);
            double num3 = GuiElement.scaled(GuiElementIcon.unscaledButtonSize);
            int num4 = this.api.Input.MouseX - (int)this.Bounds.absX;
            int num5 = this.api.Input.MouseY - (int)this.Bounds.absY;
            Vec4f shadow = new Vec4f(0.231f, 0.188f, 0.145f, 1.0f);
            Vec4f highline = new Vec4f(0.75f, 0.055f, 0.055f, 0.7f);
            if (this.texture != null)
            {
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX, (float)this.Bounds.renderY, (float)num2, (float)num3, 50f, shadow);
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX, (float)this.Bounds.renderY, (float)num2, (float)num3, 50f);
            }
            bool flag = num4 >= 0 && num5 >= 0 && (double)num4 < num2 + num && (double)num5 < num3 + num;
            if (flag)
            {
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX + -0.5f, (float)this.Bounds.renderY + -1f, (float)num2, (float)num3, 51f, highline);
                if (flag)
                {
                    Action<int> onSlotOver = this.OnSlotOver;
                    if (onSlotOver == null)
                    {
                        return;
                    }
                    onSlotOver(0);
                }
            }
        }

    
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            onClick?.Invoke();
        }
    }

    public static class GuiComposerHelpers
    {
        public static GuiComposer AddRefreshButton(this GuiComposer composer, ICoreClientAPI capi, LoadedTexture texture, ActionConsumable onClick, ElementBounds bounds, string key = null)
        {
            if (!composer.Composed)
            {
                composer.AddInteractiveElement(new GuiElementIcon(composer.Api, texture, onClick, bounds), key);
            }
            return composer;
        }
    }

    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        private ICoreClientAPI coreClientAPI;

        public ElementBounds mainDialogBound;

        public ElementBounds gridSlots;

        public ElementBounds inputslots;

        public ElementBounds mainElement;

        private LoadedTexture refreshButtonTexture => capi.Gui.LoadSvg(new AssetLocation("game:textures/Icons/refresh.svg"), 55, 55, 65, 65, new int?(ColorUtil.ColorFromRgba(206, 221, 233, 255)));

        private byte[] data { get; set; }

        public StorageVirtualInv StorageVirtualInv;

        public InventoryBase InventoryBase;

        public InventoryBase inventoryBin;

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

            inventoryBin = new InventoryGeneric(1, "bin", "1", capi);

            curTab = 1;

            SetupDialog();
        }

        public void SetupDialog()
        {
            ComposersDialog();
        }

        public string storageCompKey => "storageCompo";
        public string optionCompKey => "storageOptionCompo";
        public string sideSlotsComkey => "sideSlots";

        public string binSlotComkey => "binSlotComkey";

        protected void ComposersDialog()
        {

            //Main Element
            mainElement = ElementBounds.Fixed(0, 0, 525, 400);

            ElementBounds buttonlist = ElementBounds.Fixed(0, 0, -200, -390).FixedUnder(mainElement, 10);

            gridSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 10, 10) 
           .FixedUnder(mainElement, 10)
           .WithFixedPosition(8, 30);

            ElementBounds button1 = ElementBounds.Fixed(150, -25) //Clear All
                .FixedUnder(buttonlist, 10)
                .WithAlignment(EnumDialogArea.LeftFixed)
                .WithFixedPadding(10.0, 2.0)
                .WithFixedMargin(-30, -30);

            ElementBounds button2 = ElementBounds.Fixed(235, -10) //Link All Chests
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

            ElementBounds refreshButtonBounds = ElementBounds.Fixed(EnumDialogArea.None, 0, 0, 10.0, 26.0)
               .WithFixedPosition(485, 5)
               .WithFixedPadding(0, -1)
               .WithFixedMargin(1, 1);

            ElementBounds pageNumbersBounds = ElementBounds.Fixed(EnumDialogArea.None, 0, 0, 10.0, 26.0)
               .WithFixedPosition(485, 5)
               .WithFixedPadding(0, -1)
               .WithFixedMargin(1, 1);

            ElementBounds elementBounds7 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds7.BothSizing = ElementSizing.FitToChildren;
            elementBounds7.WithChildren(mainElement, buttonlist, gridSlots);
            //Main Gui
            mainDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedAlignmentOffset(15.0 - GuiStyle.DialogToScreenPadding, 0.0);
           var option = Composers[optionCompKey] =
                coreClientAPI.Gui
                .CreateCompo(optionCompKey, mainDialogBound)
                .AddShadedDialogBG(elementBounds7)
                .AddDialogTitleBar(Lang.Get("storagecontroller:gui-storageinventory"), CloseIconPressed)
                .AddButton(Lang.Get("storagecontroller:gui-button-clear-all"), OnClickClearAll, button1, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "clearAll")
                .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-clear-all"), CairoFont.WhiteSmallText(), 300, button1)
                .AddButton(Lang.Get("storagecontroller:gui-button-link-all"), OnClickLinkAllChests, button2, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "linkAll")
                .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-link-all"), CairoFont.WhiteSmallText(), 300, button2)
                .AddButton(Lang.Get("storagecontroller:gui-button-highlight"), OnClickHighlightAttached, button3, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, Lang.Get("highLight"))
                .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-highlight"), CairoFont.WhiteSmallText(), 300, button3)
                .AddRefreshButton(capi, refreshButtonTexture, OnRefresh, refreshButtonBounds, "refresh")
                .AddAutoSizeHoverText(Lang.Get("storagecontroller:gui-hover-refresh"), CairoFont.WhiteSmallText(), 300, refreshButtonBounds)
                .AddButton("<", PreviousGrid, button4, CairoFont.WhiteSmallText(), EnumButtonStyle.Small, "prevGrid")
                .AddButton(">", NextGrid, button5, CairoFont.WhiteSmallText(), EnumButtonStyle.Small, "nextGrid")
                .Compose();

            InputSlots();

            GridSlots();

            BinSlot();
        }


        //Bin Slot

        private void BinSlot()
        {
            ElementBounds binDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedPosition(600, -300).FixedUnder(mainElement).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

            ElementBounds binslots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 1, 1) //Highlight
                .WithAlignment(EnumDialogArea.LeftFixed)  
                .WithFixedPosition(0, 20);

            ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding +  20);
            elementBounds.BothSizing = ElementSizing.FitToChildren;
            elementBounds.WithChildren(binslots);

            if (inventoryBin != null)
            {
                Composers[binSlotComkey] = coreClientAPI.Gui.CreateCompo(binSlotComkey, binDialogBound).AddShadedDialogBG(elementBounds)
                .AddDialogTitleBar(Lang.Get("storagecontroller:gui-storageinventory-bin"), CloseIconPressed, CairoFont.WhiteDetailText())
                .AddItemSlotGrid(inventoryBin, SendBinPacket, 1, new int[1] { 0 }, binslots, "binslot")
                .AddInset(binslots, 8, 0.7f)
                .Compose();
            }
     
        }

        //Input Slots
        private void InputSlots()
        {
            ElementBounds inputDialogBound = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftFixed).WithFixedPosition(600, -115).FixedUnder(mainElement).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);

            ElementBounds inputslots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, 2, 4) //Highlight
                .WithFixedPosition(0, 50);

            ElementBounds elementBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            elementBounds.BothSizing = ElementSizing.FitToChildren;
            elementBounds.WithChildren(inputslots);

            Composers[sideSlotsComkey] = coreClientAPI.Gui.CreateCompo(sideSlotsComkey, inputDialogBound).AddShadedDialogBG(elementBounds)
                .AddDialogTitleBar(Lang.Get("storagecontroller:gui-storageinventory-input"), CloseIconPressed, CairoFont.WhiteDetailText())
                .AddItemSlotGrid(InventoryBase, SendInvPacket, 2, new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 }, inputslots, "inputslotgrid")
                .AddInset(inputslots, 10, 0.7f)
                .Compose();
        }

        public void GridSlots()
        {
            //SlotsGrid
            var stoCompKey = Composers[storageCompKey] = coreClientAPI.Gui.CreateCompo(storageCompKey, mainDialogBound);

            if (StorageVirtualInv != null && !StorageVirtualInv.Empty)
            {
                stoCompKey
                .AddItemSlotGrid(StorageVirtualInv, SendVirtualPacket, 10, gridSlots, "slotgrid")
                .AddInset(gridSlots, 10, 0.7f).Compose();


                int slotsCount = StorageVirtualInv?.Count ?? 0; // Number of slots in virtualInventory, or 0 if virtualInventory is null


                //this will stop the page going back to 1 when refreshing the gridslots.
                if (curTab > 1)
                {
                    GridPage();
                }
                else
                {   
                    //Fixed number are slot showed
                    stoCompKey.GetSlotGrid("slotgrid")
                        .DetermineAvailableSlots(Enumerable.Range(0, Math.Min(100, slotsCount)).ToArray());
                }
            }
            else
            {
                stoCompKey.AddInset(gridSlots, 10, 0.7f);
            }

            stoCompKey.Compose(true);
        }


        private void UpdateInv() 
        {
            StorageControllerMaster.SetVirtualInventory();

            StorageVirtualInv = StorageControllerMaster.StorageVirtualInv;

            Composers[storageCompKey].Compose();
        }

        private void SendBinPacket(object packet)
        {
            var ItemStack = inventoryBin[0]?.Itemstack;

            if (packet == null || ItemStack == null) return;

            inventoryBin[0].Itemstack = null;

            coreClientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.binItemStackPacket, data);
        }

        private void SendInvPacket(object packet)
        {
            if (packet == null) return;

            coreClientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
        }

        private void SendVirtualPacket(object packet)
        {
            if (packet == null) return;

            ItemStack itemStack = capi.World.Player.InventoryManager.MouseItemSlot?.Itemstack;

            if (itemStack == null)
            {
                return;
            }
            
            data = itemStack.ToBytes();

            if (data != null)
            {
                capi.World.Player.InventoryManager.MouseItemSlot.Itemstack = null;
                coreClientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.itemStackPacket, data);
            }
          
        }

        //Refresh button?
        public bool OnRefresh() 
        {
            UpdateInv();

            GridSlots();

            return true;
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
                compKey.GetSlotGrid("slotgrid")
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
            StorageVirtualInv?.Clear();
            return true;
        }

        //find a fix
        private bool OnClickLinkAllChests()
        {
            StorageControllerMaster.LinkAll(StorageControllerMaster.enLinkTargets.ALL, capi.World.Player);
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

        public override void OnGuiClosed()
        {
            //StorageControllerMaster?.OnPlayerExitStorageInterface();

            capi.Gui.PlaySound(CloseSound, randomizePitch: true);

            base.OnGuiClosed();
        }

        public override void OnGuiOpened()
        {
            //StorageControllerMaster?.OnPlayerEnterStorageInterface();

            capi.Gui.PlaySound(OpenSound, randomizePitch: true);

            base.OnGuiOpened();
        }

    }
}
