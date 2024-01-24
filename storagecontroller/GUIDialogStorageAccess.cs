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
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace storagecontroller
{
    public class GUIDialogStorageAccess : GuiDialogBlockEntity
    {
        ICoreClientAPI clientAPI;
        InventoryBase virtualinventory;
        public GUIDialogStorageAccess(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (IsDuplicate)
            {
                return;
            }
            clientAPI = capi;
            virtualinventory= inventory;
            //capi.World.Player.InventoryManager.OpenInventory((IInventory)inventory);
            SetupDialog();
        }
        public void SetVirtualInventory(InventoryBase newinventory)
        {
            virtualinventory= newinventory;
            SetupDialog();
        }

        public virtual string storageCompKey => "storageCompo";
        public virtual string optionCompKey => "storageOptionCompo";

        protected virtual void SetupDialog()
        {
            ClearComposers();
            double SSB = (GuiElementPassiveItemSlot.unscaledSlotSize);
            double SSP = (GuiElementItemSlotGridBase.unscaledSlotPadding);
            int itemsincolumn = 15;
            int columns = (this.Inventory.Count - 2) / itemsincolumn;
            double inventoryWindowWidth = 50 + (columns < 10 ? 10 : columns) * (SSB + SSP);
            double inventoryWindowHeight = 50 + itemsincolumn * (SSB + SSP);

            //This is the option inventory screen
            double elemToDlgPad = GuiStyle.ElementToDialogPadding;

            ElementBounds button = ElementBounds.Fixed(32, 40, 125, 48);//;.WithFixedPadding(5, 5);
            //ElementBounds textbounds = ElementBounds.Fixed(15 + 64 + 5, 50, 128, 64).WithFixedPadding(5, 5);
            ElementBounds dialogBounds =
                ElementBounds.Fixed(EnumDialogArea.LeftMiddle, 30, 0, 150, inventoryWindowHeight)
                .ForkBoundingParent(elemToDlgPad, elemToDlgPad, elemToDlgPad, elemToDlgPad)
            ;

            Composers[optionCompKey] =
                capi.Gui
                .CreateCompo(optionCompKey, dialogBounds)
                .AddShadedDialogBG(ElementBounds.Fill)
                .AddDialogTitleBar("Controls", CloseIconPressed)
            ;

            Composers[optionCompKey].AddButton("Clear All", () => { return OnClickClearAll(); }, button, CairoFont.WhiteSmallText(), EnumButtonStyle.Normal, "clearbutton");
            Composers[optionCompKey].AddAutoSizeHoverText("Clears all connections from controller", CairoFont.WhiteSmallText(), 300, button);
            button = button.BelowCopy();

            Composers[optionCompKey].Compose();

            

            

            //This is the storage system inventory screen
            
            dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds tradeSlotsBounds = ElementBounds.FixedPos(EnumDialogArea.LeftBottom, 0, 0)
                .WithFixedWidth(inventoryWindowWidth)
                .WithFixedHeight(inventoryWindowHeight);
            bgBounds.WithChildren(tradeSlotsBounds);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            // Lastly, create the dialog
            Composers[storageCompKey] = capi.Gui.CreateCompo(storageCompKey, dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("storagecontroller:storageinventory"), OnTitleBarCloseClicked);

            int maxRows = itemsincolumn;
            int curColumn = 0;
            int columnsAISG = 3;
            var elementBounds= ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + curColumn * (SSP + SSB), 50  * (SSP + SSB))
                    .WithFixedWidth(100)
                 .WithFixedHeight(48);
            
            
            for (int i = 0; i < (virtualinventory.Count); i++)
            {
                if (i != 0 && i % maxRows == 0)
                {
                    curColumn++;
                }
                
                var boundsAISG = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX +  curColumn * (SSP + SSB), 50+ (i % maxRows) * (SSP + SSB))
                    .WithFixedWidth(48)
                 .WithFixedHeight(48);
                
                tradeSlotsBounds.WithChild(boundsAISG);
                int slotno = i;
                Composers[storageCompKey].AddButton("", () => {
                    return GetClick(slotno);
                }, boundsAISG, CairoFont.WhiteSmallText(), EnumButtonStyle.None, "button-" + i);
                Composers[storageCompKey].AddPassiveItemSlot(boundsAISG, virtualinventory, virtualinventory[i],false);
                
                //SingleComposer.AddAutoSizeHoverText(this.Inventory[i].Itemstack.GetName(), CairoFont.WhiteSmallText(), 300, boundsAISG);
                
                ElementBounds tmpEB = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + 30 + curColumn * 200 + 165, (i % maxRows) * 60 + 25).WithFixedHeight(GuiElement.scaled((200.0))).WithFixedWidth(35);
                tradeSlotsBounds.WithChild(tmpEB);
                

            }
            Composers[storageCompKey].Compose();
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        
        private bool GetClick(int slotno)
        {
            if (slotno >= virtualinventory.Count) { return true; }
            ItemSlot transferslot = virtualinventory[slotno];
            if (transferslot == null || transferslot.Empty) return true;

            byte[] newdata = transferslot.Itemstack.ToBytes();
            clientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.inventoryPacket, newdata);
            clientAPI.World.Player.InventoryManager.DropMouseSlotItems(true);
            TryClose();
            return true;
        }

        private bool OnClickClearAll()
        {
            clientAPI.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, StorageControllerMaster.clearInventoryPacket, null);
            StorageControllerMaster scm = clientAPI.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as StorageControllerMaster;
            if (scm != null) { scm.ClearHighlighted(clientAPI.World.Player); }
            TryClose();
            return true;
        }
      
        
    }
}
