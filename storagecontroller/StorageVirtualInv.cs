using Vintagestory.API.Common;

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

            if (op.MouseButton == EnumMouseButton.Left) {

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

                return base.ActivateSlot(slotId, sourceSlot, ref op);
            }

            return false;
        }
    }
}
