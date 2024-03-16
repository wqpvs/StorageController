using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace storagecontroller
{
    internal class BlockStorageController: BlockGenericTypedContainer
    {
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            
            StorageControllerMaster forstm = world.BlockAccessor.GetBlockEntity(pos) as StorageControllerMaster;
            if (forstm != null)
            {
                ItemStack itemStack = new ItemStack(world.BlockAccessor.GetBlock(pos),1);
                byte[] data = SerializerUtil.Serialize(forstm.ContainerList);
                if (data != null)
                {
                    itemStack.Attributes.SetBytes(StorageControllerMaster.containerlistkey, data);
                    return new ItemStack[] { itemStack };
                }
            }
            
            return base.GetDrops(world,pos,byPlayer, dropQuantityMultiplier);
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            StorageControllerMaster forstm = world.BlockAccessor.GetBlockEntity(pos) as StorageControllerMaster;
            if (forstm != null)
            {
                ItemStack itemStack = new ItemStack(world.BlockAccessor.GetBlock(pos), 1);
                byte[] data = SerializerUtil.Serialize(forstm.ContainerList);
                if (data != null)
                {
                    itemStack.Attributes.SetBytes(StorageControllerMaster.containerlistkey, data);
                    return itemStack;
                }
            }
            return base.OnPickBlock(world, pos);
        }

        //check to see if we are in range of the linked blocks.
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is StorageControllerMaster storageControllerMaster)
            {
                storageControllerMaster.OnPlayerRightClick(byPlayer, blockSel);
            }

            return true;
        }
    }
}
