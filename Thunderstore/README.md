# Cities Skylines 2 : Population and Education Rebalance Mod
The goal of this mod is to rebalance population structure and education needs, to be more realistic. It will reduce the number of Children and increase the number or Teens, thus balancing the needs for Elementary and High Schools.

## Features

### Lifecycle adjustments
  - Default thresholds for licycle stages are changed from 21/36/84 to 12/24/77. As a result, population structure should be more realistic i.e. 15% Children, 10% Teens, 60% Adults and 15% Seniors. These are approximate numbers ofc, may differ in your cities.
  - The thresholds can be individually set in the config file.
### Graduation probability
  - Graduation probability is tweaked so the average time spent in schools is more aligned to the cim's lifcycle. For fully staffed and efficient schools, it will be ~9 years for Elementary School, ~6 years for High School, ~6 years for College and ~9 years for University. The numbers will vary, depending on how well your city is developed.
  - The graduation params can be individually set in the config file.
### Education needs
  - As a result of the above changes, you should need less Elementary Schools, approx. 1 per 10000 citizens and a bit more High Schools, approx. 1 per 30000. College and University needs are not changed much, 1 College per 35000 and 1 University per 50-60 thousands cims.

## Requirements and Compatibility
- Cities Skylines II version 1.0.15f1
- BepInEx 5

## Installation
1. Place the `RealPopMod.dll` file in your BepInEx `Plugins` folder.
2. The config file is automatically created when the game is run once.

## Known Issues
- Nothing atm.

## Changelog
- v0.2.0 (2023-12-06)
  - Added config file in BepInEx/config folder.
  - Newly created citizens also follow the updated lifecycle thresholds.
- v0.1.1 (2023-12-05)
  - Changed icon to be more readable on Thunderstore.
- v0.1.0 (2023-12-05)
  - Initial build.

## Planned Features
- Waiting to see what CO will do about this topic.
- Fix graduation logic, making sure that students will stay more than 1-2 day in schools.
- Correct Average Time Spent in school calculations.

## Disclaimers and Notes
> [!IMPORTANT]
It will take one full in-game day for the population to adjust to the new thresholds. Education changes need more time, at least 3-5 days. You may wanna build a few extra High Schools until the levels will adjust.

> [!IMPORTANT]
Cims cannot go back to the previous phase of their lives, so changes done by this mod are irreversible. Make sure to have a savefile. Once the mod is deactivated, cims will follow default logic, so eventually the city will return to the vanilla state, but it will take several in-game days.

> [!NOTE]
> The graduation logic implemented in the game is flawed, imho. Most students stay in schools for 1-2 days usually. The average time shown in the UI is totally incorrect (on many levels, both calculations and UI presentation).

> [!CAUTION]
> You are downloading, installing, and running this mod on your computer at your own risk.  I do not accept any responsibility for damage caused to your game, your computer, or your Cim's lives.

> [!NOTE]
> The timeline in CS2 is measured in days. Each in-game day also represents 1 month (e.g. when looking at average time spent in schools) but also can be treated as 1 year of cim life, which is much more reasonable than using months. Cims live around 100 days, give or take few, so it is pretty close to average fuman lifespan (80+ years).
> The population structure is based on EU statistics data, population by age. 0-14 years is ~15%, which we can consider Children in CS2 reality. 15-24 years is 11%, which we can consider Teens. And 65+ years is 21%, which we can consider Seniors (65 is usual retirement age in EU). The rest are Adults.