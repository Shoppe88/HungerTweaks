using System;

namespace HungerTweaks
{
    public class HungerTweaksConfig
    {
        public float GlobalMultiplier { get; set; } = 1.0f;
        public MonthLengthScalingCfg MonthLengthScaling { get; set; } = new();
        public ActionMultipliersCfg ActionMultipliers { get; set; } = new();
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

    public class ActionMultipliersCfg
    {
        public double Standing { get; set; } = 1.0;
        public double Sprinting { get; set; } = 1.5;
        public double Sneaking { get; set; } = 1.1;
        public double Sitting { get; set; } = 0.85;
        public double Sleeping { get; set; } = 0.7;

        public double Mining { get; set; } = 1.25;
        public double Chopping { get; set; } = 1.2;
        public double WeaponSwing { get; set; } = 1.15;
        public double BowUse { get; set; } = 1.2;
        public double HammerUse { get; set; } = 1.15;

        public double Get(HungerAction action) => action switch
        {
            HungerAction.Sprinting => Sprinting,
            HungerAction.Sneaking  => Sneaking,
            HungerAction.Sitting   => Sitting,
            HungerAction.Sleeping  => Sleeping,
            HungerAction.Mining    => Mining,
            HungerAction.Chopping  => Chopping,
            HungerAction.WeaponSwing => WeaponSwing,
            HungerAction.BowUse    => BowUse,
            HungerAction.HammerUse => HammerUse,
            _ => Standing
        };
    }

    public enum HungerAction
    {
        Standing,
        Sprinting,
        Sneaking,
        Sitting,
        Sleeping,
        Mining,
        Chopping,
        WeaponSwing,
        BowUse,
        HammerUse
    }
}
