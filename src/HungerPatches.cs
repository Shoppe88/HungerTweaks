using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace HungerTweaks
{
    [HarmonyPatch]
    public static class HungerPatches
    {
        private static readonly ConcurrentDictionary<long, InputState> Input = new();
        private static readonly ConcurrentDictionary<long, long> LastPlayerDebugMs = new();
        private static long lastStormDebugMs = 0;

        private sealed class InputState
        {
            // RMB click detection
            public bool LastRightDown;

            // LMB click window fallback (weapon swing)
            public bool LastLeftDown;
            public long LastLeftPressMs;

            // Panning one-shot flag
            public bool PanningPending;
            public long PanningPendingSetMs;

            // Weapon swing one-shot flag
            public bool WeaponSwingPending;
            public long WeaponSwingPendingSetMs;
        }

        private const long WeaponSwingPendingMaxMs = 3000;

        // Called by ModSystem on a frequent interval (20ms) to catch quick RMB clicks.
        public static void SampleAllPlayersForInputFlags(ICoreServerAPI sapi, HungerTweaksConfig cfg)
        {
            try
            {
                foreach (var plr in sapi.World.AllOnlinePlayers)
                {
                    if (plr?.Entity is EntityPlayer ep)
                        SamplePlayerInputsForFlags(ep, sapi, cfg);
                }
            }
            catch
            {
                // never break server tick
            }
        }

        private static void SamplePlayerInputsForFlags(EntityPlayer plr, ICoreServerAPI sapi, HungerTweaksConfig cfg)
        {
            try
            {
                var c = plr.Controls;
                if (c == null) return;

                long nowMs = sapi.World.ElapsedMilliseconds;
                var st = Input.GetOrAdd(plr.EntityId, _ => new InputState());

                // RMB rising edge
                bool rmb = c.RightMouseDown;
                bool rmbClick = rmb && !st.LastRightDown;
                st.LastRightDown = rmb;
                if (!rmbClick) return;

                // Read held item
                string itemCode = "";
                try
                {
                    itemCode = plr.ActiveHandItemSlot?.Itemstack?.Collectible?.Code?.ToString() ?? "";
                }
                catch { }

                // Conditions:
                // 1) Holding pan
                if (!IsWoodenPan(itemCode)) return;

                // 2) Standing in water (wet + water/seawater at/under feet) AND not swimming
                if (plr.Swimming) return;
                if (!plr.FeetInLiquid) return;
                if (!IsWaterOrSeawaterAtOrBelowFeet(plr, sapi)) return;

                // 3) Not moving
                if (c.TriesToMove) return;

                // 4) RMB click (rising edge) -> set one-shot flag
                st.PanningPending = true;
                st.PanningPendingSetMs = nowMs;

                if (cfg.DebugLogging)
                {
                    sapi.Logger.Notification($"[HungerTweaks] Pan flag SET (eid={plr.EntityId})");
                }
            }
            catch
            {
                // never break server tick
            }
        }

        // --- Weapon swing flag: set on actual server attack start ---
        [HarmonyPatch(typeof(CollectibleObject), "OnHeldAttackStart")]
        private static class WeaponSwingAttackStartPatch
        {
            static void Prefix(CollectibleObject __instance, object[] __args)
            {
                try
                {
                    var cfg = HungerTweaksModSystem.Config;
                    var sapi = HungerTweaksModSystem.Sapi;
                    if (cfg == null || sapi == null) return;

                    EntityPlayer? plr = null;
                    foreach (var a in __args)
                    {
                        if (a is EntityPlayer ep) { plr = ep; break; }
                        if (a is EntityAgent ea && ea is EntityPlayer ep2) { plr = ep2; break; }
                        if (a is IPlayer ip && ip.Entity is EntityPlayer ep3) { plr = ep3; break; }
                    }
                    if (plr == null) return;

                    var tool = __instance.Tool;
                    string itemCode = __instance.Code?.ToString() ?? "";

                    bool isWorkTool =
                        tool == EnumTool.Pickaxe ||
                        tool == EnumTool.Axe ||
                        tool == EnumTool.Hammer ||
                        tool == EnumTool.Bow ||
                        IsProPick(itemCode, tool) ||
                        IsShovel(itemCode, tool);

                    if (isWorkTool) return;

                    long nowMs = sapi.World.ElapsedMilliseconds;
                    var st = Input.GetOrAdd(plr.EntityId, _ => new InputState());
                    st.WeaponSwingPending = true;
                    st.WeaponSwingPendingSetMs = nowMs;
                }
                catch
                {
                    // never break attacks
                }
            }
        }

        // Patch ReduceSaturation(float ...) via reflection
        private static MethodBase? TargetMethod()
        {
            var t = AccessTools.TypeByName("Vintagestory.GameContent.EntityBehaviorHunger");
            if (t == null) return null;

            return t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "ReduceSaturation") return false;
                    var ps = m.GetParameters();
                    return ps.Length >= 1 && ps[0].ParameterType == typeof(float);
                });
        }

        private static void Prefix(object __instance, ref float __0)
        {
            try
            {
                var cfg = HungerTweaksModSystem.Config;
                var sapi = HungerTweaksModSystem.Sapi;
                if (cfg == null || sapi == null) return;

                var entField = AccessTools.Field(__instance.GetType(), "entity");
                if (entField == null) return;

                if (entField.GetValue(__instance) is not EntityPlayer plr) return;

                HungerAction action = DetectAction(plr, sapi, cfg);

                double monthMul = GetMonthScalingMultiplier(cfg, sapi, action);
                double actionMul = cfg.ActionMultipliers.Get(action);

                double envMul = 1.0;
                envMul *= GetPathWalkingMultiplier(plr, sapi, cfg, action);
                envMul *= GetTemporalStormMultiplier(sapi, cfg);

                double totalMul = cfg.GlobalMultiplier * monthMul * envMul * actionMul;
                if (totalMul <= 0) return;

                __0 = (float)(__0 * totalMul);

                if (cfg.DebugLogging)
                {
                    long now = sapi.World.ElapsedMilliseconds;
                    long last = LastPlayerDebugMs.GetOrAdd(plr.EntityId, 0);

                    if (now - last >= 2000)
                    {
                        LastPlayerDebugMs[plr.EntityId] = now;

                        string name = plr.Player?.PlayerName ?? "unknown";
                        string uid = plr.PlayerUID ?? "no-uid";

                        sapi.Logger.Notification(
                            $"[HungerTweaks] plr={name} uid={uid} eid={plr.EntityId} action={action} baseLoss*={totalMul:0.###} " +
                            $"(month={monthMul:0.###}, env={envMul:0.###}, act={actionMul:0.###}, global={cfg.GlobalMultiplier:0.###})"
                        );
                    }
                }
            }
            catch
            {
                // never break hunger ticks
            }
        }

        private static double cachedNormalActualHoursPerMonth = double.NaN;

        private static double GetMonthScalingMultiplier(HungerTweaksConfig cfg, ICoreServerAPI sapi, HungerAction action)
        {
            var sc = cfg.MonthLengthScaling;
            if (!sc.Enabled) return 1.0;

            double actualCandidate = sc.OverrideActualRealHoursPerMonth;
            bool accelerated = false;

            if (sc.UseWorldCalendar && sapi.World?.Calendar != null)
            {
                var cal = sapi.World.Calendar;

                if (action == HungerAction.Sleeping) accelerated = true;

                double denom = cal.SpeedOfTime * cal.CalendarSpeedMul;
                if (denom > 0.000001)
                {
                    actualCandidate = cal.DaysPerMonth * cal.HoursPerDay / denom;
                    if (cal.SpeedOfTime > 120) accelerated = true;
                }
            }

            double actualUsed = actualCandidate;

            if (accelerated)
            {
                if (!double.IsNaN(cachedNormalActualHoursPerMonth) && cachedNormalActualHoursPerMonth > 0.000001)
                    actualUsed = cachedNormalActualHoursPerMonth;
                else
                    actualUsed = sc.OverrideActualRealHoursPerMonth > 0.000001 ? sc.OverrideActualRealHoursPerMonth : actualCandidate;
            }
            else
            {
                if (actualCandidate > 0.000001) cachedNormalActualHoursPerMonth = actualCandidate;
            }

            if (actualUsed <= 0.000001) return 1.0;

            double mul = sc.ReferenceRealHoursPerMonth / actualUsed;
            if (mul < sc.MinMultiplier) mul = sc.MinMultiplier;
            if (mul > sc.MaxMultiplier) mul = sc.MaxMultiplier;
            return mul;
        }

        private static HungerAction DetectAction(EntityPlayer plr, ICoreServerAPI sapi, HungerTweaksConfig cfg)
        {
            var c = plr.Controls;
            if (c == null) return HungerAction.Standing;

            // Mounted checks
            if (plr.MountedOn != null)
            {
                var seat = plr.MountedOn;
                string seatTypeName = seat.GetType().Name;

                if (seatTypeName.Contains("Bed", StringComparison.OrdinalIgnoreCase))
                    return HungerAction.Sleeping;

                Entity? mountedEntity = TryGetMountedEntity(seat);

                if (LooksLikeFurnitureSeat(seat, mountedEntity))
                    return HungerAction.SittingFurniture;

                if (mountedEntity is EntityAgent)
                    return HungerAction.SittingMount;

                return HungerAction.SittingFurniture;
            }

            if (c.FloorSitting) return HungerAction.Sitting;

            // Held tool + item code
            EnumTool? tool = null;
            string itemCode = "";
            try
            {
                var slot = plr.ActiveHandItemSlot;
                var coll = slot?.Itemstack?.Collectible;
                if (coll != null)
                {
                    tool = coll.Tool;
                    itemCode = coll.Code?.ToString() ?? "";
                }
            }
            catch { }

            long nowMs = sapi.World.ElapsedMilliseconds;
            var st = Input.GetOrAdd(plr.EntityId, _ => new InputState());

            // LMB click window fallback tracking
            if (c.LeftMouseDown && !st.LastLeftDown)
                st.LastLeftPressMs = nowMs;
            st.LastLeftDown = c.LeftMouseDown;

            // 1) Panning pending (one-shot) - consumes once
            if (st.PanningPending)
            {
                if (nowMs - st.PanningPendingSetMs <= cfg.ActionMultipliers.PanningPendingMaxMs)
                {
                    st.PanningPending = false;
                    return HungerAction.Panning;
                }
                st.PanningPending = false;
            }

            // 2) Weapon swing pending (one-shot) - consumes once
            if (st.WeaponSwingPending)
            {
                if (nowMs - st.WeaponSwingPendingSetMs <= WeaponSwingPendingMaxMs)
                {
                    st.WeaponSwingPending = false;
                    return HungerAction.WeaponSwing;
                }
                st.WeaponSwingPending = false;
            }

            // 3) Swimming heuristic
            bool inLiquid = plr.Swimming || plr.FeetInLiquid;
            if (inLiquid && IsWaterOrSeawaterAtOrBelowFeet(plr, sapi))
            {
                if (plr.Swimming || !IsStandingOnSolidBlock(plr, sapi))
                    return HungerAction.Swimming;
            }

            // 4) Bow
            if (tool == EnumTool.Bow || (tool != null && tool.ToString().IndexOf("Crossbow", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                if (c.IsAiming || c.RightMouseDown) return HungerAction.BowUse;
            }

            // 5) Work tools while holding LMB
            if (c.LeftMouseDown)
            {
                if (tool == EnumTool.Pickaxe || IsProPick(itemCode, tool)) return HungerAction.Mining;
                if (tool == EnumTool.Axe) return HungerAction.Chopping;
                if (tool == EnumTool.Shovel || IsShovel(itemCode, tool)) return HungerAction.Digging;
                if (tool == EnumTool.Hammer) return HungerAction.HammerUse;
            }

            // 6) Optional fallback weapon swing click window (exclude work tools)
            if (nowMs - st.LastLeftPressMs <= cfg.ActionMultipliers.WeaponSwingClickWindowMs)
            {
                bool toolIsWorkTool =
                    tool == EnumTool.Pickaxe ||
                    tool == EnumTool.Axe ||
                    tool == EnumTool.Hammer ||
                    tool == EnumTool.Bow ||
                    IsProPick(itemCode, tool) ||
                    (tool == EnumTool.Shovel || IsShovel(itemCode, tool));

                if (!toolIsWorkTool) return HungerAction.WeaponSwing;
            }

            // 7) Movement
            if (c.TriesToMove && c.Sprint) return HungerAction.Sprinting;
            if (c.TriesToMove && c.Sneak) return HungerAction.Sneaking;

            return HungerAction.Standing;
        }

        private static double GetPathWalkingMultiplier(EntityPlayer plr, ICoreServerAPI sapi, HungerTweaksConfig cfg, HungerAction action)
        {
            var env = cfg.EnvironmentMultipliers;
            if (!env.PathBonusEnabled) return 1.0;

            if (action != HungerAction.Standing && action != HungerAction.Sprinting && action != HungerAction.Sneaking)
                return 1.0;

            if (env.PathBonusRequiresMoving && (plr.Controls == null || !plr.Controls.TriesToMove))
                return 1.0;

            try
            {
                int x = (int)Math.Floor(plr.Pos.X);
                int y = (int)Math.Floor(plr.Pos.Y);
                int z = (int)Math.Floor(plr.Pos.Z);

                var ba = sapi.World.BlockAccessor;
                var below = ba.GetBlock(new BlockPos(x, y - 1, z));
                var at = ba.GetBlock(new BlockPos(x, y, z));
                var b = below ?? at;
                if (b?.Code == null) return 1.0;

                string code = b.Code.ToString().ToLowerInvariant();
                foreach (var kw in env.PathCodeKeywords ?? Array.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(kw) && code.Contains(kw.ToLowerInvariant()))
                        return env.PathBlockHungerMultiplier;
                }

                return 1.0;
            }
            catch
            {
                return 1.0;
            }
        }

        private static double GetTemporalStormMultiplier(ICoreServerAPI sapi, HungerTweaksConfig cfg)
        {
            var env = cfg.EnvironmentMultipliers;
            if (!env.TemporalStormMultiplierEnabled) return 1.0;

            long now = sapi.World.ElapsedMilliseconds;

            try
            {
                Type? t =
                    AccessTools.TypeByName("Vintagestory.GameContent.SystemTemporalStability") ??
                    AccessTools.TypeByName("Vintagestory.ServerMods.SystemTemporalStability");

                if (t == null)
                {
                    if (cfg.DebugLogging && now - lastStormDebugMs > 5000)
                    {
                        lastStormDebugMs = now;
                        sapi.Logger.Notification("[HungerTweaks] Storm detect: FAILED (SystemTemporalStability type not found)");
                    }
                    return 1.0;
                }

                object? sys = TryGetModSystemByType(sapi, t);
                if (sys == null)
                {
                    if (cfg.DebugLogging && now - lastStormDebugMs > 5000)
                    {
                        lastStormDebugMs = now;
                        sapi.Logger.Notification($"[HungerTweaks] Storm detect: FAILED (ModSystem instance not found for {t.FullName})");
                    }
                    return 1.0;
                }

                bool activeFlag = TryIsTemporalStormActive(sys);

                bool hasStrength = TryGetTemporalStormStrength(sys, out double strength);
                bool activeByStrength = hasStrength && strength > env.TemporalStormStrengthThreshold;

                bool active = activeFlag || activeByStrength;

                if (cfg.DebugLogging && now - lastStormDebugMs > 5000)
                {
                    lastStormDebugMs = now;
                    sapi.Logger.Notification(
                        $"[HungerTweaks] Storm detect: active={active} flag={activeFlag} " +
                        $"strength={(hasStrength ? strength.ToString("0.###") : "n/a")} thr={env.TemporalStormStrengthThreshold:0.###}"
                    );
                }

                return active ? env.TemporalStormMultiplier : 1.0;
            }
            catch (Exception e)
            {
                if (cfg.DebugLogging && now - lastStormDebugMs > 5000)
                {
                    lastStormDebugMs = now;
                    sapi.Logger.Notification($"[HungerTweaks] Storm detect: EXCEPTION {e.GetType().Name}: {e.Message}");
                }
                return 1.0;
            }
        }

        private static bool TryGetTemporalStormStrength(object sys, out double strength)
        {
            strength = 0;

            static object? GetMember(object obj, string name)
            {
                var t = obj.GetType();
                return AccessTools.Property(t, name)?.GetValue(obj)
                    ?? AccessTools.Field(t, name)?.GetValue(obj);
            }

            string[] names =
            {
                "StormStrength", "stormStrength",
                "TemporalStormStrength", "temporalStormStrength",
                "NowStormStrength", "nowStormStrength",
                "StormIntensity", "stormIntensity",
                "TemporalStormIntensity", "temporalStormIntensity",
                "StormLevel", "stormLevel",
                "TemporalStormLevel", "temporalStormLevel"
            };

            foreach (var n in names)
            {
                object? v = GetMember(sys, n);
                if (v == null) continue;

                try { strength = Convert.ToDouble(v); return true; }
                catch { }
            }

            var st = sys.GetType();
            object? data =
                AccessTools.Field(st, "data")?.GetValue(sys) ??
                AccessTools.Property(st, "data")?.GetValue(sys) ??
                AccessTools.Field(st, "Data")?.GetValue(sys) ??
                AccessTools.Property(st, "Data")?.GetValue(sys);

            if (data != null)
            {
                foreach (var n in names)
                {
                    object? v = GetMember(data, n);
                    if (v == null) continue;

                    try { strength = Convert.ToDouble(v); return true; }
                    catch { }
                }
            }

            return false;
        }

        private static object? TryGetModSystemByType(ICoreServerAPI sapi, Type modSystemType)
        {
            var ml = sapi.ModLoader;

            try
            {
                var m2 = ml.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(mi =>
                    {
                        if (mi.Name != "GetModSystem") return false;
                        var ps = mi.GetParameters();
                        return ps.Length == 2 && ps[0].ParameterType == typeof(Type) && ps[1].ParameterType == typeof(bool);
                    });

                if (m2 != null)
                {
                    var got = m2.Invoke(ml, new object[] { modSystemType, true });
                    if (got != null) return got;

                    got = m2.Invoke(ml, new object[] { modSystemType, false });
                    if (got != null) return got;
                }
            }
            catch { }

            try
            {
                var m1 = ml.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                    .FirstOrDefault(mi =>
                    {
                        if (mi.Name != "GetModSystem") return false;
                        var ps = mi.GetParameters();
                        return ps.Length == 1 && ps[0].ParameterType == typeof(Type);
                    });

                if (m1 != null)
                {
                    var got = m1.Invoke(ml, new object[] { modSystemType });
                    if (got != null) return got;
                }
            }
            catch { }

            try
            {
                var fields = ml.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var f in fields)
                {
                    if (f.FieldType == typeof(string)) continue;
                    if (!typeof(IEnumerable).IsAssignableFrom(f.FieldType)) continue;

                    object? container;
                    try { container = f.GetValue(ml); } catch { continue; }
                    if (container is not IEnumerable enumerable) continue;

                    foreach (var sys in enumerable)
                    {
                        if (sys == null) continue;

                        var st = sys.GetType();
                        if (modSystemType.IsAssignableFrom(st)) return sys;
                        if (st.Name == modSystemType.Name) return sys;
                        if (st.FullName != null && st.FullName.EndsWith("." + modSystemType.Name, StringComparison.Ordinal))
                            return sys;
                    }
                }
            }
            catch { }

            return null;
        }

        private static bool IsProPick(string itemCode, EnumTool? tool)
        {
            if (!string.IsNullOrWhiteSpace(itemCode))
            {
                var s = itemCode.ToLowerInvariant();
                if (s.Contains("propick") || s.Contains("prospectingpick")) return true;
            }

            if (tool != null)
            {
                var tn = tool.ToString().ToLowerInvariant();
                if (tn.Contains("propick") || tn.Contains("prospect")) return true;
            }

            return false;
        }

        private static bool IsShovel(string itemCode, EnumTool? tool)
        {
            if (tool != null && tool.ToString().Equals("Shovel", StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(itemCode))
            {
                var s = itemCode.ToLowerInvariant();
                if (s.Contains("shovel")) return true;
            }

            return false;
        }

        private static bool IsWoodenPan(string itemCode)
        {
            if (string.IsNullOrWhiteSpace(itemCode)) return false;
            var s = itemCode.ToLowerInvariant();

            return s.Contains("pan-wooden")
                || s.Contains("woodenpan")
                || (s.Contains("pan") && s.Contains("wood"));
        }

        // ---- Mounted seat helpers ----
        private static Entity? TryGetMountedEntity(IMountableSeat seat)
        {
            try
            {
                var t = seat.GetType();

                var prop =
                    t.GetProperty("Entity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    t.GetProperty("MountableEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    t.GetProperty("MountedEntity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (prop != null)
                    return prop.GetValue(seat) as Entity;

                var field =
                    t.GetField("Entity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
                    t.GetField("entity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (field != null)
                    return field.GetValue(seat) as Entity;
            }
            catch { }

            return null;
        }

        private static bool LooksLikeFurnitureSeat(IMountableSeat seat, Entity? mountedEntity)
        {
            try
            {
                string name = seat.GetType().Name.ToLowerInvariant();
                if (name.Contains("seat") || name.Contains("chair") || name.Contains("bench") || name.Contains("stool"))
                    return true;

                if (mountedEntity != null)
                {
                    string entName = mountedEntity.GetType().Name.ToLowerInvariant();
                    if (entName.Contains("seat") || entName.Contains("chair") || entName.Contains("bench") || entName.Contains("stool"))
                        return true;

                    var codeStr = mountedEntity.Code?.ToString().ToLowerInvariant() ?? "";
                    if (codeStr.Contains("seat") || codeStr.Contains("chair") || codeStr.Contains("bench") || codeStr.Contains("stool"))
                        return true;
                }
            }
            catch { }

            return false;
        }

        private static bool TryIsTemporalStormActive(object sys)
        {
            var st = sys.GetType();

            object? direct =
                AccessTools.Property(st, "NowStormActive")?.GetValue(sys) ??
                AccessTools.Property(st, "nowStormActive")?.GetValue(sys) ??
                AccessTools.Property(st, "TemporalStormActive")?.GetValue(sys) ??
                AccessTools.Property(st, "temporalStormActive")?.GetValue(sys) ??
                AccessTools.Field(st, "NowStormActive")?.GetValue(sys) ??
                AccessTools.Field(st, "nowStormActive")?.GetValue(sys) ??
                AccessTools.Field(st, "TemporalStormActive")?.GetValue(sys) ??
                AccessTools.Field(st, "temporalStormActive")?.GetValue(sys);

            if (direct is bool b0) return b0;

            object? data =
                AccessTools.Field(st, "data")?.GetValue(sys) ??
                AccessTools.Property(st, "data")?.GetValue(sys) ??
                AccessTools.Field(st, "Data")?.GetValue(sys) ??
                AccessTools.Property(st, "Data")?.GetValue(sys);

            if (data != null)
            {
                var dt = data.GetType();
                object? flag =
                    AccessTools.Field(dt, "nowStormActive")?.GetValue(data) ??
                    AccessTools.Property(dt, "nowStormActive")?.GetValue(data) ??
                    AccessTools.Field(dt, "NowStormActive")?.GetValue(data) ??
                    AccessTools.Property(dt, "NowStormActive")?.GetValue(data);

                if (flag is bool b1) return b1;
            }

            return false;
        }

        // --- Water helpers ---
        private static bool IsWaterLike(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string s = code.ToLowerInvariant();
            return s.Contains("seawater") || (s.Contains("water") && !s.Contains("wattle"));
        }

        private static bool IsWaterOrSeawaterAtOrBelowFeet(EntityPlayer plr, ICoreServerAPI sapi)
        {
            try
            {
                var ba = sapi.World.BlockAccessor;

                int x = (int)Math.Floor(plr.Pos.X);
                int y = (int)Math.Floor(plr.Pos.Y);
                int z = (int)Math.Floor(plr.Pos.Z);

                var fluidAt = ba.GetBlock(new BlockPos(x, y, z), BlockLayersAccess.Fluid);
                if (fluidAt != null && fluidAt.IsLiquid() && IsWaterLike(fluidAt.Code?.ToString()))
                    return true;

                var fluidBelow = ba.GetBlock(new BlockPos(x, y - 1, z), BlockLayersAccess.Fluid);
                if (fluidBelow != null && fluidBelow.IsLiquid() && IsWaterLike(fluidBelow.Code?.ToString()))
                    return true;
            }
            catch { }

            return false;
        }

        private static bool IsStandingOnSolidBlock(EntityPlayer plr, ICoreServerAPI sapi)
        {
            try
            {
                var ba = sapi.World.BlockAccessor;

                int x = (int)Math.Floor(plr.Pos.X);
                int y = (int)Math.Floor(plr.Pos.Y);
                int z = (int)Math.Floor(plr.Pos.Z);

                var solidBelow = ba.GetBlock(new BlockPos(x, y - 1, z), BlockLayersAccess.Solid);
                if (solidBelow == null) return false;

                if (solidBelow.Id == 0) return false;
                if (solidBelow.Replaceable >= 6000) return false;

                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
