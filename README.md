HungerTweaks

Server-side mod that lets you customize hunger/satiety drain for specific actions and (optionally) scale hunger to match your server’s real-time calendar settings.
Installation

    Place the mod .zip into your server’s Mods folder.

    Start the game/server once to generate the config file.

    Exit the game/server.

    Open: VintagestoryData/ModConfig/hungertweaks.json

    Adjust settings as desired.

    Restart the game/server.

 
Updateing

    Move your HungerTweaks.json file out of the Modconfig directory (so you don't lose your custom settings)

        This mod will not automatically overwrite an existing .json file, so there may be errors or ignored behavior if you don't generate a new .json with the necessary fields

2. Start the game, then exit the game to generate a new HungerTweaks.json file
3. Go into the Modconfig\HungerTweaks.json directory and update to your desired settings
What this mod does (by default)
Action-based hunger multipliers

Adjust hunger drain per action so you can:

    DECREASE drain while:

        sitting

        sleeping

        walking on path/road blocks

        panning
        walking on path blocks

 

    INCREASE drain while:

        sprinting

        mining

        chopping

        fighting/hunting
        sneaking
        being in a temporal storm

Month-length scaling (optional)

If your server uses non-default “real-life hours per in-game month”, hunger can feel too punishing (or too easy). This mod can automatically scale hunger to better match your time settings—so you don’t end up eating multiple meals per in-game day unless you’re constantly sprinting.
Important notes / limitations

    Server-only mod. The in-game character sheet “Hunger rate %” may not change and can still display the vanilla values.

    How to verify it’s working: set an action multiplier to something extreme (example: "Sitting": 25.0), then compare hunger drain while standing vs sitting. You should see a clear difference.

    Detection limitations: the mod uses best-effort checks based on player state and held tool type. Edge cases may not be detected correctly, including:

        Custom beds not recognized as “beds”

        Modded tools/weapons that don’t report standard tool types

        Mods that change sleep/time-skip rules (e.g., one-person sleep): If you use a one-person sleep mod, a player that stays up all night will take a huge hunger hit while the sleeper will be generally fine, so coordinate your sleeping with server mates, or at least give them a warning.
        modded animal mounts, furniature, and boats may not register properly
    These values are only recommendations, you can adjust them to your liking to fit your play style or needs.
        the change in food necessity will alter your gameplay a bit, making it so food security isn't so stressful.
        If you reduce food consuption requirements, it could make mods that increase food production over powered
        server owners:
            you could create events where something makes food consumption go crazy and you increase food needs for a period of time.

How the final drain is calculated

 

BaseDrain is the % in your character sheet's "Hunger Rate %", convert it to decimal, so 100% = 1, 120% = 1.2, and so on.
 
FinalDrain = BaseDrain * GlobalMultiplier * MonthScale * ActionMultiplier * environment
For example: BaseDrain *       .7         *     1      * 0.65(sleeping)   *      1      = BaseDrain * 0.455; this means when sleeping your hunger reduces about 54.5% slower than expected.
             BaseDrain *       .7         *    .48     * 2.75(sprinting)  *      1      = BaseDrain * 0.924; This means when sprinting your hunger reduces about 7.6% slower than expected
                                     (but in this case your MonthScale means your hunger rate is about 52% slower than expected, so while sprinting you are nearly doubling your hunger rate). 
GlobalMultiplier recommendations (real-life hours per in-game month)

These are common starting points (using 7.2 hours/month as a “vanilla-ish” baseline)

If your real life hours per in game month equal:

    3 hours → 2.40

    6 h → 1.20

    9 h → 0.80

    12 h → 0.60

    15 h → 0.48

    18 h → 0.40

    20 h → 0.36

    30 h → 0.24

    48 h → ~0.15–0.16'

 

set your global multiplier to one of these values. If you use custom X hours per ingame month, use the formula below
Custom formula

Choose:

    BML = Baseline Month Length is what you want hunger balanced around (example: vanilla-ish is 7.2)

    CML = Custom Month Length is your server’s real-life hours per in-game month

 
 
GlobalMultiplier = BML / CML

Examples (BML = 7.2):

    CML = 11.5 → GM = 7.2 / 11.5 = 0.626

    CML = 23.75 → GM = 7.2 / 23.75 = 0.303

    CML = 48.5 → GM = 7.2 / 48.5 = 0.154

MonthLengthScaling settings explained

    ReferenceRealHoursPerMonth

        The month length (in real-life hours) you consider “baseline”.

        If your server is set to 20 hours/month and you set this to 20, the MonthScale becomes neutral (≈1.0).
            If your server is set to 20 hours/month and you set this to 10, the MonthScale becomes 20/10 = 2.0, hunger increases by 100% (basically double)
            If your server is set to 20 hours/month and you set this to 30, the MonthScale becomes 20/30 = .667, hunger decreases by 33.3%

 

author's note: I recommend keeping this value equating to 1 and dialing in your scaling through the GlobalMultiplier value above, this can have some significant run on effects if there is even a small miscalculation on your RL Hours per month. 

Because the calculation is a simple series of multiplication, an equation like basedrain x .5 x 4 x 1 x 1 = basedrain x 1 x 2 x 1 x 1 ; so this is to say, adjust either globalmultiplier or monthlengthscale, not both, for simplicity.

 

    UseWorldCalendar

        If true, the mod computes actual hours/month from the server’s world calendar settings automatically.

        Recommended if you may adjust time settings later.

    OverrideActualRealHoursPerMonth

        Only used when UseWorldCalendar is false.

        Lets you hard-code the “actual” month length the mod should use (ignoring world settings).

    MinMultiplier / MaxMultiplier

        Clamps MonthScale to prevent extreme values.

Example based on my config settings **I'm still tweaking my settings**
 {
"GlobalMultiplier": 0.35,
  "MonthLengthScaling": {
    "Enabled": true,
    "ReferenceRealHoursPerMonth": 48.5, \\This is equates to roughly 97 minute days, so I have about 46.5 minutes for daylight and 46.5 for nighttime
    "UseWorldCalendar": true,
    "OverrideActualRealHoursPerMonth": 7.2,
    "MinMultiplier": 0.1,
    "MaxMultiplier": 10.0
  },
  "ActionMultipliers": {
    "Standing": 1.15,
    "Sprinting": 3.0,
    "Sneaking": 1.5,
    "Sitting": 0.65,
    "SittingMount": 0.8,
    "SittingFurniture": 0.6,
    "Sleeping": 0.50,
    "Mining": 1.25,
    "Chopping": 1.35,
    "HammerUse": 1.4,
    "BowUse": 1.25,
    "WeaponSwing": 1.2,
    "WeaponSwingClickWindowMs": 250,
\\how long a the modifer lasts after mouse click
 
    "Panning": 0.85,
    "PanningClickWindowMs": 2000,
\\ this is how long the panning modifier lasts after a mouse click
    "RequirePanStillHeldDuringWindow": true,
    "RequireFeetInLiquidAtClick": true
  },
  "EnvironmentMultipliers": {
    "PathBonusEnabled": true,
    "PathBlockHungerMultiplier": 0.87,
    "PathBonusRequiresMoving": true,
    "PathCodeKeywords": [
      "path",
      "road"
    ],
    "TemporalStormMultiplierEnabled": true,
    "TemporalStormMultiplier": 4.0,                        
    "TemporalStormStrengthThreshold": 0.0001
  },
  "DebugLogging": false
}
 
Future update ideas

swimming
skinning an animal
digging with hands
currently, only 1 action counts during hunger tick, (most recent action taken); make actions stack (sitting + panning, sprinting + weapon swing)
wetness factor
see if its possble to make a patch for the carryon mod

(feel free to add suggestions in the comments)
Free Use notification
Anyone is free to use this in mod packs, just give credit please
