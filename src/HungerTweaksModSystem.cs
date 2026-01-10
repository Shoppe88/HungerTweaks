using Vintagestory.API.Common;
using Vintagestory.API.Server;
using HarmonyLib;

namespace HungerTweaks
{
    public class HungerTweaksModSystem : ModSystem
    {
        internal static ICoreServerAPI Sapi;
        internal static HungerTweaksConfig Config;

        private Harmony? harmony;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            Sapi = api;

            // Load or create config
            Config = api.LoadModConfig<HungerTweaksConfig>("hungertweaks.json");
            if (Config == null)
            {
                Config = new HungerTweaksConfig();
                api.StoreModConfig(Config, "hungertweaks.json");
            }

            harmony = new Harmony("hungertweaks.patches");
            harmony.PatchAll(typeof(HungerPatches).Assembly);

            api.Logger.Notification("[HungerTweaks] Loaded. Config: VintagestoryData/ModConfig/hungertweaks.json");
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll("hungertweaks.patches");
            harmony = null;
        }
    }
}
