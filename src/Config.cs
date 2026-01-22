namespace HungerTweaks
{
    public class HungerTweaksConfig
    {
        public float GlobalMultiplier { get; set; } = 1.0f;
        public MonthLengthScalingCfg MonthLengthScaling { get; set; } = new();
        public ActionMultipliersCfg ActionMultipliers { get; set; } = new();
        public EnvironmentMultipliersCfg EnvironmentMultipliers { get; set; } = new();
        public bool DebugLogging { get; set; } = false;
    }

    public class MonthLengthScalingCfg
    {
        public bool Enabled { get; set; } = true;
        public double ReferenceRealHoursPerMonth { get; set; } = 7.2;
        public bool UseWorldCalendar { get; set; } = true;
        public double OverrideActualRealHoursPerMonth { get; set; } = 7.2;
        public double MinMultiplier { get; set; } = 0.1;
        public double MaxMultiplier { get; set; } = 10.0;
    }

    public class EnvironmentMultipliersCfg
    {
        // Path/road blocks reduce hunger slightly while moving
        public bool PathBonusEnabled { get; set; } = true;
        public double PathBlockHungerMultiplier { get; set; } = 0.90;
        public bool PathBonusRequiresMoving { get; set; } = true;

        // Keywords checked against block code path (e.g. "game:path-wood")
        public string[] PathCodeKeywords { get; set; } = new[] { "path", "road" };

        // Temporal storm multiplier (stress)
        public bool TemporalStormMultiplierEnabled { get; set; } = true;
        public double TemporalStormMultiplier { get; set; } = 4.0;
        public double TemporalStormStrengthThreshold { get; set; } = 0.0001;
    }

    public class ActionMultipliersCfg
    {
        // Movement states
        public double Standing { get; set; } = 1.0;
        public double Sprinting { get; set; } = 1.5;
        public double Sneaking { get; set; } = 1.1;
        public double Swimming { get; set; } = 2.5;

        // Sitting variants
        public double Sitting { get; set; } = 0.75;           // Floor sitting
        public double SittingMount { get; set; } = 0.80;      // Mounted on animal
        public double SittingFurniture { get; set; } = 0.70;  // Mounted on chair/bench/etc.
        public double Sleeping { get; set; } = 0.70;

        // Tool/interaction states (LMB hold)
        public double Mining { get; set; } = 1.25;       // pickaxe + propick
        public double Chopping { get; set; } = 1.20;     // axe
        public double Digging { get; set; } = 1.15;      // shovel
        public double HammerUse { get; set; } = 1.15;
        public double BowUse { get; set; } = 1.20;

// Fire starter (one-shot flag on use; consumed on next hunger tick)
public double FireStarting { get; set; } = 8.00;

// Quern spinning (block interaction over time; refreshed while spinning)
public double QuernGrinding { get; set; } = 2.25;
public int QuernSpinHoldMs { get; set; } = 2000;     // must hold RMB this long before counting as "spinning"
public int QuernFlagRefreshMs { get; set; } = 500;   // while spinning, refresh pending flag about twice per second

        // Weapon swing (flag + optional click-window fallback)
        public double WeaponSwing { get; set; } = 1.15;
        public int WeaponSwingClickWindowMs { get; set; } = 250;

        // Panning (flag-based: set on RMB click when conditions met; consumed on next hunger tick)
        public double Panning { get; set; } = 0.85;

        // Max time a panning flag may wait for the next hunger tick before expiring.
        // Your hunger ticks can be ~10s apart, so 30000ms is safe.
        public int PanningPendingMaxMs { get; set; } = 30000;

        // Generic max time an action flag may wait for the next hunger tick before expiring.
        // Use this for actions that are flagged on server events (mining/chopping/digging/hammer/bow-use).
        public int ActionPendingMaxMs { get; set; } = 30000;

        public double Get(HungerAction action) => action switch
        {
            HungerAction.Sprinting => Sprinting,
            HungerAction.Sneaking => Sneaking,
            HungerAction.Swimming => Swimming,

            HungerAction.Sitting => Sitting,
            HungerAction.SittingMount => SittingMount,
            HungerAction.SittingFurniture => SittingFurniture,
            HungerAction.Sleeping => Sleeping,

            HungerAction.Mining => Mining,
            HungerAction.Chopping => Chopping,
            HungerAction.Digging => Digging,
            HungerAction.HammerUse => HammerUse,
            HungerAction.BowUse => BowUse,
HungerAction.FireStarting => FireStarting,
HungerAction.QuernGrinding => QuernGrinding,


            HungerAction.WeaponSwing => WeaponSwing,
            HungerAction.Panning => Panning,

            _ => Standing
        };
    }

    public enum HungerAction
    {
        Standing,
        Sprinting,
        Sneaking,
        Swimming,

        Sitting,
        SittingMount,
        SittingFurniture,
        Sleeping,

        Mining,
        Chopping,
        Digging,
        HammerUse,
        BowUse,
        FireStarting,
        QuernGrinding,

        WeaponSwing,
        Panning
    }
}
