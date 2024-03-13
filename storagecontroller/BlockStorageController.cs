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
                ItemStack mystack = new ItemStack(world.BlockAccessor.GetBlock(pos),1);
                byte[] data = SerializerUtil.Serialize<List<BlockPos>>(forstm.ContainerList);
                if (data != null)
                {
                    mystack.Attributes.SetBytes(StorageControllerMaster.containerlistkey, data);
                    return new ItemStack[] { mystack };
                }
            }
            
            return base.GetDrops(world,pos,byPlayer, dropQuantityMultiplier);
        }
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            StorageControllerMaster forstm = world.BlockAccessor.GetBlockEntity(pos) as StorageControllerMaster;
            if (forstm != null)
            {
                ItemStack mystack = new ItemStack(world.BlockAccessor.GetBlock(pos), 1);
                byte[] data = SerializerUtil.Serialize<List<BlockPos>>(forstm.ContainerList);
                if (data != null)
                {
                    mystack.Attributes.SetBytes(StorageControllerMaster.containerlistkey, data);
                    return mystack;
                }
            }
            return base.OnPickBlock(world, pos);
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
