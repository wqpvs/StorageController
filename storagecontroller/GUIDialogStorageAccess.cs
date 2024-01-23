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
        protected virtual void SetupDialog()
        {
            double SSB = (GuiElementPassiveItemSlot.unscaledSlotSize);
            double SSP = (GuiElementItemSlotGridBase.unscaledSlotPadding);
            int itemsincolumn = 15;
            int columns = (this.Inventory.Count - 2) / itemsincolumn;
            double mainWindowWidth = 50+ (columns<10?10:columns) * (SSB  + SSP );
            double mainWindowHeight =50+ itemsincolumn * (SSB+SSP);
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds tradeSlotsBounds = ElementBounds.FixedPos(EnumDialogArea.LeftBottom, 0, 0)
                .WithFixedWidth(mainWindowWidth)
                .WithFixedHeight(mainWindowHeight);
            bgBounds.WithChildren(tradeSlotsBounds);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            // Lastly, create the dialog
            SingleComposer = capi.Gui.CreateCompo("storagecontrollercompo", dialogBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("storagecontroller:storageinventory"), OnTitleBarCloseClicked);

            int maxRows = itemsincolumn;
            int curColumn = 0;
            int columnsAISG = 3;
            
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
                SingleComposer.AddButton("", () => {
                    return GetClick(slotno);
                }, boundsAISG, CairoFont.WhiteSmallText(), EnumButtonStyle.None, "button-" + i);
                SingleComposer.AddPassiveItemSlot(boundsAISG, virtualinventory, virtualinventory[i],false);
                
                //SingleComposer.AddAutoSizeHoverText(this.Inventory[i].Itemstack.GetName(), CairoFont.WhiteSmallText(), 300, boundsAISG);
                
                ElementBounds tmpEB = ElementBounds.FixedPos(EnumDialogArea.LeftTop, tradeSlotsBounds.fixedX + 30 + curColumn * 200 + 165, (i % maxRows) * 60 + 25).WithFixedHeight(GuiElement.scaled((200.0))).WithFixedWidth(35);
                tradeSlotsBounds.WithChild(tmpEB);
                

            }
            SingleComposer.Compose();
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

      
        
    }
}
