using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace HungerTweaks
{
    [HarmonyPatch]
    public static class HungerPatches
    {
        // Patch the game-content hunger behavior via reflection (server-side).
        static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("Vintagestory.GameContent.EntityBehaviorHunger");
            if (t == null) return null;

            // Find ReduceSaturation(float ...) (exact signature may vary by version)
            return t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "ReduceSaturation") return false;
                    var ps = m.GetParameters();
                    return ps.Length >= 1 && ps[0].ParameterType == typeof(float);
                });
        }

        // Prefix: scale the first float argument (amount).
        static void Prefix(object __instance, ref float __0)
        {
            try
            {
                var cfg = HungerTweaksModSystem.Config;
                var sapi = HungerTweaksModSystem.Sapi;
                if (cfg == null || sapi == null) return;

                // Get 'entity' field (common on EntityBehavior-derived classes)
                var entField = AccessTools.Field(__instance.GetType(), "entity");
                if (entField == null) return;

                var entity = entField.GetValue(__instance) as Entity;
                if (entity == null) return;

                // Only affect players
                if (entity is not EntityPlayer plr) return;

                double monthMul = GetMonthScalingMultiplier(cfg, sapi);
                var action = DetectAction(plr);
                double actionMul = cfg.ActionMultipliers.Get(action);

                double totalMul = cfg.GlobalMultiplier * monthMul * actionMul;
                if (totalMul <= 0) return;

                __0 = (float)(__0 * totalMul);

                if (cfg.DebugLogging)
                {
                    sapi.Logger.Notification($"[HungerTweaks] action={action} baseLoss*={totalMul:0.###} (month={monthMul:0.###}, act={actionMul:0.###}, global={cfg.GlobalMultiplier:0.###})");
                }
            }
            catch
            {
                // Intentionally swallow to avoid breaking hunger ticks
            }
        }

        private static double GetMonthScalingMultiplier(HungerTweaksConfig cfg, ICoreServerAPI sapi)
        {
            var sc = cfg.MonthLengthScaling;
            if (!sc.Enabled) return 1.0;

            double actual = sc.OverrideActualRealHoursPerMonth;

            if (sc.UseWorldCalendar && sapi.World?.Calendar != null)
            {
                var cal = sapi.World.Calendar;

                // realHoursPerMonth = DaysPerMonth * HoursPerDay / (SpeedOfTime * CalendarSpeedMul)
                // (SpeedOfTime includes modifiers; CalendarSpeedMul slows/speeds calendar progression)
                double denom = cal.SpeedOfTime * cal.CalendarSpeedMul;
                if (denom > 0.000001)
                {
                    actual = cal.DaysPerMonth * cal.HoursPerDay / denom;
                }
            }

            if (actual <= 0.000001) return 1.0;

            double mul = sc.ReferenceRealHoursPerMonth / actual;
            if (mul < sc.MinMultiplier) mul = sc.MinMultiplier;
            if (mul > sc.MaxMultiplier) mul = sc.MaxMultiplier;
            return mul;
        }

        private static HungerAction DetectAction(EntityPlayer plr)
        {
            var c = plr.Controls;
            if (c == null) return HungerAction.Standing;

            // Sleeping: player mounted on bed (best-effort: check type name contains "Bed")
            if (plr.MountedOn != null)
            {
                var n = plr.MountedOn.GetType().Name;
                if (n.Contains("Bed", StringComparison.OrdinalIgnoreCase))
                    return HungerAction.Sleeping;
            }

            if (c.FloorSitting) return HungerAction.Sitting;

            // Determine held tool type (best-effort)
            EnumTool? tool = null;
            try
            {
                var slot = plr.ActiveHandItemSlot;
                var coll = slot?.Itemstack?.Collectible;
                if (coll != null) tool = coll.Tool;
            }
            catch { }

            // Bow use
            if (tool == EnumTool.Bow || tool == EnumTool.Crossbow)
            {
                if (c.IsAiming || c.RightMouseDown) return HungerAction.BowUse;
            }

            // Left-click actions (attack/mining)
            if (c.LeftMouseDown)
            {
                if (tool == EnumTool.Pickaxe) return HungerAction.Mining;
                if (tool == EnumTool.Axe) return HungerAction.Chopping;
                if (tool == EnumTool.Hammer) return HungerAction.HammerUse;
                return HungerAction.WeaponSwing;
            }

            // Movement states
            bool moving = c.TriesToMove;
            if (moving && c.Sprint) return HungerAction.Sprinting;
            if (moving && c.Sneak) return HungerAction.Sneaking;

            

            return HungerAction.Standing;
        }
    }
}
