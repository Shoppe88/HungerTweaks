HungerTweaks

Server-side mod that lets you customize hunger/satiety drain for specific actions and (optionally) scale hunger to match your server’s real-time calendar settings.
SHORT VERSION FOR GENERAL PLAYERS AND SMALL SERVERS
Installing:

    Download the .zip file
    Put it in your mods folder
    Start your world. This generates your .json file
    Exit your game
    Open: VintagestoryData/ModConfig/hungertweaks.json

    Adjust settings as desired.

    Restart the game/server.

Updating:

    Move your HungerTweaks.json file out of the Modconfig directory (so you don't lose your custom settings)

        This mod will not automatically overwrite an existing .json file, so there may be errors or ignored behavior if you don't generate a new .json with the necessary fields

2. Start the game, then exit the game to generate a new HungerTweaks.json file
3. Go into the Modconfig\HungerTweaks.json directory and update to your desired settings

 

NEXT UPDATE: (1.0.8) The "MonthLengthScaling" will be an Opt-In feature. It will be defaulted to off, so no need to calculate your "real life hours per month" unless you want to, so any scaling will be done through GlobalMultiplier.

 

UPDATE INFO: I plan to release 1.0.8 after 1.22 drops so I can make sure the APIs and new functions work properly. Sorry for the long delay.
What This Mod Does (by default)

 

    Less hunger drain while:

        sitting

        sleeping

        walking on paths/roads

        panning

 

    More hunger drain while:

        sprinting

        mining

        chopping

        digging

        fighting/hunting

        sneaking

        swimming
        starting a fire
        using the quern

        temporal storms

 
Settings:

This mod runs on a simple set of multiplication scalars, so changes to both GlobalMultipler and MonthLengthScaling will likely mess you up; keep it simple and focus on just the GlobalMultiplier.

Set MonthLengthScaling to false; this will ignore the feature entirely and you can ignore it also.

 

Tweak your overall hunger scaling using GlobalMultiplier.

These are common starting points (using 7.2 hours/month as a “vanilla-ish” baseline)

recommended (real life hours per in game month → globalmultiplier value):

    3 hours → 2.40

    6 h → 1.20

    9 h → 0.80

    12 h → 0.60

    15 h → 0.48

    18 h → 0.40

    20 h → 0.36

    30 h → 0.24

    48 h → ~0.15–0.16

see below for custom values

 

Adjust the .json file as desired for individual action multipliers. You can reference my .json file at the bottom of this description

 

GAMEPLAY NOTE: The in-game hunger rate is considered in part of the calculations, so things that increase the in-game base hunger rate (like winter or wearing armor) will cause your hunger to drop rather rapidly. I recommend using the suggested global values above; not to exceed 2x your recommended global modifier value.
Compatibility:

There are some mods that adjust hunger as well, IF the other mod makes changes to the BaseRate (in-game character sheet percentage), that % will be calculated into this mod, and will likely make things askew (for example: Walking Stick mod).
This mod should not have any CTD issues as it simply applies a scalar to your hunger ticks

Will work with Bed Regen mod, as HungerTweaks only checks for if you are laying in a bed as a condition. However, this might make laying in a bed for health regen a little OP. If you do want to use both together, I suggest bringing the "Sleeping" hunger rate closer to 1 for balance.
 
 
 
 
In-Depth version
Installation

    Place the mod .zip into your server’s Mods folder.

    Start the game/server once to generate the config file.

    Exit the game/server.

    Open: VintagestoryData/ModConfig/hungertweaks.json

    Adjust settings as desired.

    Restart the game/server.

 
Updating

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

 

    INCREASE drain while:

        sprinting

        mining

        chopping
        digging

        fighting/hunting
        sneaking
        being in a temporal storm
        swimming

Month-length scaling (optional)

If your server uses non-default “real-life hours per in-game month”, hunger can feel too punishing (or too easy). This mod can automatically scale hunger to better match your time settings—so you don’t end up eating multiple meals per in-game day unless you’re constantly sprinting.
Important notes / limitations

    Server-only mod. The in-game character sheet “Hunger rate %” may not change and can still display the vanilla values.

    How to verify it’s working: set debugmode: true in .json. Read debug messages in Logs/server-main.log

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
For example: BaseDrain *       .7         *     1      * 0.65(sleeping)   *      1      = BaseDrain * 0.455; This means when sleeping your hunger reduces about 54.5% slower than vanilla.
             BaseDrain *       .7         *    .48     * 2.75(sprinting)  *      1      = BaseDrain * 0.924; This means when sprinting your hunger reduces about 7.6% slower than vanilla.
                                     (but in this case your MonthScale means your hunger rate is about 52% slower than expected, so while sprinting you are nearly doubling your hunger rate). 
 WinterBaseDrain(1.45) *       .7         *     1      * 1.5 (HammerUse)  * 6 (temporal storm) = BaseDrain * 9.135; This means Hammering during the winter AND a temporal storm will use a little over 9x the hunger rate of vanilla.
 
GAMEPLAY NOTE: The in-game hunger rate is considered the BaseDrain for game play calculations, so things that increase the in-game base hunger rate (like winter or wearing armor) will cause your hunger to drop rather rapidly. I recommend using the suggested GlobalMultiplier values below; not to exceed 2x your recommended global modifier value.
 
If you use the mod that forces you to fight something to end a temporal strom, I recommend reducing the temporal storm hunger multiplier.
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

How to find your time scales:

    Check Hours Per Day: Type /time hoursperday (or /time hoursperday [amount]) to see how many in-game hours are in a day (default is 24). (X value)
    Check Days Per Month: Use /wc daysPerMonth (requires admin) to see the in-game month length (default is 9). (Y value)

 

    Check Time Speed: Type /time speed (or /time speed [amount]) to see how fast time passes; default is 60 (meaning 1 game hour = 1 real-life minute). (Z value)
    Check Calender speed multiplier: type /time calendarspeedmul (requires admin) to see the ingame calenderspeedmultplier (default is .5). (W value)

 

Formula is:

     (X * Y) / (Z * W) = Real life hours per month 

 

        So default looks like: (24 * 9) / (60 * .5) = 216 / 30 = 7.2

MonthLengthScaling settings explained

    ReferenceRealHoursPerMonth

        The month length (in real-life hours) you consider “baseline”.

        If your server is set to 20 hours/month and you set this to 20, the MonthScale becomes neutral (≈1.0).
            If your server is set to 20 hours/month and you set this to 10, the MonthScale becomes 10/20 = .5 , hunger decreased by 50% (basically half)
            If your server is set to 20 hours/month and you set this to 30, the MonthScale becomes 30/20 = 1.5 , hunger increases by 50%

 

author's note: I recommend keeping this value equating to 1 and dialing in your scaling through the GlobalMultiplier value above, this can have some significant run on effects if there is even a small miscalculation on your RL Hours per month. 

Because the calculation is a simple series of multiplication, an equation like basedrain x .5 x 4 x 1 x 1 = basedrain x 1 x 2 x 1 x 1 ; so this is to say, adjust either globalmultiplier or monthlengthscale, not both, for simplicity

If you are comfortable managing the month scaling modifer and global modifier, then you can finetune your hunger rates to a higher degree.

 

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
"GlobalMultiplier": 0.3,
  "MonthLengthScaling": {
    "Enabled": true,
    "ReferenceRealHoursPerMonth": 48.5,
           //This equates to roughly 97 minute days, so I have about 46.5 minutes for daylight and 46.5 for nighttime
   
    "UseWorldCalendar": true,
   
    "OverrideActualRealHoursPerMonth": 7.2,
   
    "MinMultiplier": 0.1,
    "MaxMultiplier": 10.0
  },
 
  "ActionMultipliers": {
    "Standing": 1.05,
    "Sprinting": 3.5,
    "Sneaking": 1.75,
    "Sitting": 0.65,
    "SittingMount": 0.8,
    "SittingFurniture": 0.6,
    "Sleeping": 0.50,
    "Swimming": 3,
    "Digging": 1.3,
    "Mining": 1.45,
    "Chopping": 1.35,
    "HammerUse": 1.5,
    "BowUse": 1.25,
    "FireStarting": 9.0,
    "QuernGrinding": 3.75,
    "QuernSpinHoldMs": 2000,
    "QuernFlagRefreshMs": 500,
    "WeaponSwing": 1.2,
    "WeaponSwingClickWindowMs": 250,
    "Panning": 0.85,
    "PanningPendingMaxMs": 30000

  "EnvironmentMultipliers": {
    "PathBonusEnabled": true,
    "PathBlockHungerMultiplier": 0.84,
    "PathBonusRequiresMoving": true,
    "PathCodeKeywords": [
      "path",
      "road"
       ],
 
    "TemporalStormMultiplierEnabled": true,
    "TemporalStormMultiplier": 6.0,                        
    "TemporalStormStrengthThreshold": 0.0001
  },
  "DebugLogging": false
}
 
Future update ideas

    skinning an animal
    digging with hands
    currently, only 1 action counts during hunger tick, (most recent action taken); make actions stack (sitting + panning, sprinting + weapon swing)
    wetness factor
    see if its possble to make a patch for the carryon mod
    while spinnig the quern
    starting a fire

 

(feel free to add suggestions in the comments)
Free Use notification
Anyone is free to use this in mod packs, just give credit please
