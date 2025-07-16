using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using HarmonyLib;

namespace MiniShipFix;

public class MiniShipFixModSystem : ModSystem
{

    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        var harmony = new Harmony("minishipfix");
        harmony.PatchAll();
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        api.Logger.Notification("Hello from template mod server side: " + Lang.Get("minishipfix:hello"));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Logger.Notification("Hello from template mod client side: " + Lang.Get("minishipfix:hello"));
    }

}
