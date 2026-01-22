In the Modcofig file you can make adjustments to the hunger rates as well as the
global modifiers if you use non-default real-world hours/in-game month.

Below is the pregenerated .json file along with recommendaitons for changes

INSTALLATION:

After placing the .zip file into your mods fold, you will start the game to generate the
.json file. Exit the game. Then go into the mod config file, find the hungertweaks.json file
and make desired changes.


Functions of this mod:
You can adjust the hunger rates of actions in game so sitting gives a slight benefit of saving hunger, and sleeping won't lose as much hunger while sleeping.
While doing things makes your hungrier faster.

It also allows server owners to adjust the hunger rate to better match the time scale of the server setting. No more needing to eat 6 means in a single day
(unless you are sprinting everywhere).

THINGS TO CONSIDER:

This is a server only mod. It will not show any changes to the player charater sheet's "Hunger rate" %. It will read as normal, 
but if you want to verify the mod it is working, like I did. You can adjust one of the actions like, sitting to 25.0
which is 25x normal rate, go ingame, stand and watch your hunger, then sit and then watch your hunger again, and
you will see a noticable difference.

This mod uses some codeing workarounds to get the desired effects, as such, edge cases like
custom bed mods, or custom weapon/tool mods will not register them as those things and you may
not get the desired effects. For example, a custom bed that isn't modded with the "bed" tag will
not give the reduced hunger rates, I also am not sure how this would work with mods that make it
where only 1 person needs to sleep to pass time. I also don't know how the game tags players that
are sitting on animals, so am unsure if (at all) the hunger is altered. Another consideration for
possible future updates is reduced hunger when traveling on roads.

I may check some of these cases in the future.

{
  // Multiplies EVERYTHING after action + month scaling
  // Set to 1.0 for normal, <1.0 for less hunger, >1.0 for more hunger
  //Recommended: hours/month =
  /*
  3h = 2.4
  6h = 1.2
  9h = 0.8
  12h = 0.6
  15h = 0.48
  18h = 0.4
  20h = 0.36
  30h = 0.24
  */ 
  "GlobalMultiplier": 0.36,

  "MonthLengthScaling": {
    "Enabled": true,

    // “Vanilla-ish” reference: 9 days/month, 24 h/day, SpeedOfTime=60, CalendarSpeedMul=0.5
    // => real hours per month = 9*24/(60*0.5) = 7.2
    // Setting this to what you picked your server timescale to be will make it's multiplier 1.
    // So if your server setting is 20 hours per month then the scaling multiplier is 1 (nuetral).
    // If your server setting is 20 hours per month, but you set this to 10, (20/10), your hunger multiplier is doubled, you get hungry faster.
    // If your server setting is 20 hours per month, but you set this to 30, (20/30), your hunger multiplier is .667%, so you get hungry slower.
    "ReferenceRealHoursPerMonth": 20,

    // If true, compute actual month hours from the world calendar.
    // this basically means it matches the in-game hours instead of hard coding the hours.
    "UseWorldCalendar": true,

    // If UseWorldCalendar is false, this value is used (e.g. 3, 6, 30).
    // This will override what your real hours permonth server setting is. Your server hours per month is ignored and this is the new numerator.
    // This will allow server owners to further fine tune hunger rates.
    // If UseWorldCalender is true, this function is ignored.
    "OverrideActualRealHoursPerMonth": 7.2,

    // Clamp to avoid extreme values
    "MinMultiplier": 0.1,
    "MaxMultiplier": 10.0
  },

  "ActionMultipliers": {
    "Standing": 1.0,
    "Sprinting": 1.6,
    "Sneaking": 1.1,
    "Sitting": 0.75,
    "Sleeping": 0.4,

    "Mining": 1.3,        // pickaxe left-click usage
    "Chopping": 1.25,     // axe left-click usage
    "WeaponSwing": 1.15,  // other left-click attacks
    "BowUse": 1.2,         // bow/crossbow aiming/usage
    "HammerUse": 1.3,      // hammer left-click usage
    "Panning": .85,
    "WeaponSwingClickWindowMs": 250,
    "PanningClickWindowMs": 2000,
    "RequirePanStillHeldDuringWindow": true,
    "RequireFeetInLiquidAtClick": true
  },

  "EnvironmentMultipliers": {
    "PathBonusEnabled": true,
    "PathBlockHungerMultiplier": 0.9,
    "PathBonusRequiresMoving": true,
    "PathCodeKeywords": [
      "path",
      "road"
    ],
    "TemporalStormMultiplierEnabled": true,
    "TemporalStormMultiplier": 4.0,
    "TemporalStormStrengthThreshold": 0.0001
  },
  "DebugLogging": true
}
