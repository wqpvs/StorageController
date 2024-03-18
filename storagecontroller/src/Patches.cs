using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace storagecontroller
{
    public class Patches
    {
        
        [HarmonyPatch(typeof(BlockEntityGenericTypedContainer), "OnPlayerRightClick")]
        public class OnPlayerRightClick_Patch
        {

            [HarmonyPrefix]

            public static bool Prefix(IPlayer byPlayer, BlockSelection blockSel)
            {
                if (blockSel != null)
                {
                    if (byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
                    {
                        return true;
                    }

                    if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible is ItemStorageLinker)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
