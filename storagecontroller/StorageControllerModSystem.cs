using Vintagestory.API.Common;
using storagecontroller;

namespace StorageController
{
    public class StorageControllerModSystem : ModSystem
    {
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("StorageControllerMaster", typeof(StorageControllerMaster));
            api.RegisterItemClass("ItemStorageLinker",typeof(ItemStorageLinker));
            api.RegisterBlockClass("BlockStorageController", typeof(BlockStorageController));
        }

    }
}
