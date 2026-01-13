using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HungerTweaks
{
    public class HungerTweaksModSystem : ModSystem
    {
        internal static ICoreServerAPI? Sapi;
        internal static HungerTweaksConfig? Config;

        private Harmony? harmony;
        private long inputSampleListenerId;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            Sapi = api;

            Config = api.LoadModConfig<HungerTweaksConfig>("hungertweaks.json");
            if (Config == null)
            {
                Config = new HungerTweaksConfig();
                api.StoreModConfig(Config, "hungertweaks.json");
            }

            harmony = new Harmony("hungertweaks.patches");
            harmony.PatchAll(typeof(HungerPatches).Assembly);

            // Sample player inputs frequently so RMB clicks for panning don't need to align with hunger ticks.
            inputSampleListenerId = api.Event.RegisterGameTickListener(_ =>
            {
                if (Sapi == null || Config == null) return;
                HungerPatches.SampleAllPlayersForInputFlags(Sapi, Config);
            }, 20);

            api.Logger.Notification("[HungerTweaks] Loaded. Config: VintagestoryData/ModConfig/hungertweaks.json");
        }

        public override void Dispose()
        {
            if (Sapi != null && inputSampleListenerId != 0)
            {
                Sapi.Event.UnregisterGameTickListener(inputSampleListenerId);
                inputSampleListenerId = 0;
            }

            harmony?.UnpatchAll("hungertweaks.patches");
            harmony = null;
        }
    }
}
