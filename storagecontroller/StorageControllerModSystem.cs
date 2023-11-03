using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using storagecontroller;

namespace StorageController
{
    
    public class StorageControllerModSystem : ModSystem
    {
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            if (api is ICoreClientAPI) { capi=api as ICoreClientAPI; }
            else if (api is ICoreServerAPI) { sapi=api as ICoreServerAPI; }
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("StorageControllerMaster", typeof(StorageControllerMaster));
            api.RegisterItemClass("ItemStorageLinker",typeof(ItemStorageLinker));
        }
    }
}
