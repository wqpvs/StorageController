using Vintagestory.API.Client;
using Vintagestory.API.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Vintagestory.API.Server;
using System.Drawing;
using Vintagestory.API.MathTools;
using System.Net.Sockets;
using System.Reflection;
using System;

namespace storagecontroller
{
    public class StorageVirtualInv : DummyInventory
    {
        private int VirtualInvID = 1;

        public StorageVirtualInv(ICoreAPI api, int quantitySlots = 0) : base(api, quantitySlots)
        {
            VirtualInvID++;

            className = "StorageVirtualInv-" + VirtualInvID;
          
        }

        public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            ItemSlot mouseSlot = op.ActingPlayer.InventoryManager.MouseItemSlot;

            if (op.MouseButton == EnumMouseButton.Left)
            {
                if (op.ShiftDown)
                {
                    return false;
                }

                if (!mouseSlot.Empty)
                {
                    if (sourceSlot.CanHold(mouseSlot))
                    {
                        return false;
                    }
                }


                if (mouseSlot.Empty && !sourceSlot.CanTake())
                {
                    return false;
                }
            }

            return false;
        }
    }
}
