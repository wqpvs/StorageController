using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.GameContent;


namespace storagecontroller
{
    public class StorageControllerMaster:BlockEntityGenericTypedContainer
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (Api is ICoreServerAPI) { RegisterGameTickListener(OnServerTick, 100); }
        }

        public void OnServerTick(float dt)
        {
            //Manage linked container list
            // - only check so many blocks per tick

            //Need to populate containers if this container has inventory
            // - from list of container first find a stack or locked container with inventory
            // - then push in inventory if possible
            // - otherwise look for empty slots we can put our inventory into

        }
    }
}
