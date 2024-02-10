# Population and Education Rebalance Mod
The goal of this mod is to rebalance population structure and education needs, to be more realistic. It will reduce the number of Children and increase the number or Teens, thus balancing the needs for Elementary and High Schools.

Version 0.5 introduces lots of fixes and changes to Birth and New Households processes. Please read the description. You may configure most of them or even turned off, if you like so.
Version 0.6 changes the assembly name - please remove the old config file RealPopMod.cfg.


## Features

### Lifecycle adjustments
  - Default thresholds for licycle stages are changed from 21/36/84 to 12/21/75. As a result, population structure should be more realistic i.e. 15% Children, 10% Teens, 60% Adults and 15% Seniors. These are approximate numbers ofc, may differ in your cities.
  - The thresholds can be individually set in the config file.
  - At the end you should see the changes in population structure like this.

![Population](https://raw.githubusercontent.com/infixo/cs2-realpop/master/docs/pop_change.png)
  
### Graduation logic
  - Graduation process is more restrictive. Cims following typical education path will spend more time in High School, College and University. In Vanilla game they usually graduated after 1-2 days, rarely more. With this mod, they will spend a minimum number of days which is defined in the config file.
  - By default: 3 days/years in High School, 4 days/years in College and 5 days/years in University.
  - Graduation probability is also configurable. However, it doesn't affect time spent in schools much (give or take 1-2 days/years for some unlucky students).
### Education needs
  - As a result of the above changes, you should need less Elementary Schools, approx. 1 per 10000 citizens and a bit more High Schools, approx. 1 per 30000. College and University needs are not changed much, 1 College per 35000 and 1 University per 50-60 thousands cims.
  - Also, cims will stay longer in schools. Please note that "Average time to graduate" will now show the correct value in years, however year here is the same as day or month. So, don't be alarmed e.g. when you see 4 years. It means that cims on average will spend 4 in-game days or months in this school.
### Fixes for ApplyToSchool logic
  - Teens will no longer go to University.
  - College cannot be skipped. Vanilla game allows for direct jump from High School to University, even for Teens (famous genius Teens).
  - At the end you should see the changes in education groups ratios like this.
  
![Education](https://raw.githubusercontent.com/infixo/cs2-realpop/master/docs/edu_change.png)
  
### New households (v0.5)
  - Fixes CoupleHousehold having only 1 Adult. This bug is the main reason why so many singles move into the city. The mod makes CoupleHousehold as having 2 Adults.
  - Since there is no typical 2+1 family defined (sic!), the mod changes 2+5 family into 2+1 family. Also boosts a bit chances of 2+2 families. This further improves the structure of incoming families.
  - Fixes the bug where Children and Teens, all are spawned with age 0.
  - Fixes the bug where all StudentHousehold are single Adults at fixed age 36.
  - Allows for Adults to be at any education level. This can be turned on/off in the config file, option NewAdultsAnyEducation.
  - Allows for Teens to be spawned as StudentHousehold. They are College-ready. This can be turned on/off in the config file, option AllowTeenStudents.
  
### Birth process (v0.5)
  - Adults cannot have children if they won't be able to raise them before becoming Seniors. This should prevent from having families like 1 Senior with 2 Children, that usually trigger High Rent warning. This can be turned on/off in the config file, option NoChildrenWhenTooOld.
  - Introduces configurable params to control birth process, BirthChanceSingle, BirthChanceFamily and NextBirthChance. You can define a base chance to have a baby for a single mother, a family and decrease in chance with each consecutive baby.
  - The chance of having a baby is halved also when a father is a student.
  - The below picture shows Vanilla and modded distribution of families in the game. The vanilla curve is rather flat and symmetrical, produces on average 3.8 children in a family and allows for huge families like 6+ children. Modded curve is tilted towards lower number of children and chances of getting bigger families drop with every new child. On average, it results in 2.3 children per family.
  - Please note that as a result of all the above changes, the birth rate will be lower that in the vanilla game, at approx. 2/3.

![Children](https://raw.githubusercontent.com/infixo/cs2-realpop/master/docs/children.png)

### New households limiter (v0.6)
  - Limits spawning of new households when the number of empty properties falls below a configrable treshold (by default 3%). This allows for Teens becoming new Adults have a chance to actually find a property. In Vanilla game, new households spawn so fast that they occupy all available buildings and new adults are forced to leave the city.
  - As a result it **heavily reduces the number of cims Moving Away**.
  - The feature can be turned off by setting the option FreeRatioTreshold to -1.

## Technical

### Requirements and Compatibility
- Cities Skylines II version 1.0.19f1 or later; check GitHub or Discord if the mod is compatible with the latest game version.
- BepInEx 5.
- Modified systems: AgingSystem, ApplyToSchoolSystem, BirthSystem, CitizenInitializeSystem, GraduationSystem, SchoolAISystem.
- Cim Behavior Improvements is not compatible (both modify ApplyToSchoolSystem and BirthSystem).

### Installation
1. Place the `RealPop.dll` file in your BepInEx `Plugins` folder.
2. The config file is automatically created (in BepInEx\config) when the game is run once.

### Known Issues
- Nothing atm.

### Changelog
- v0.6.0 (2024-02-10)
  - New households limiter.
  - Updated default config values and new assembly name.
- v0.5.0 (2024-02-05)
  - Fixes and tweaks to Birth process and New households.
- v0.4.0 (2024-01-31)
  - Compatibility with patch 1.0.19f1
- v0.3.0 (2023-12-07)
  - Graduation logic revamped.
  - 2 fixes for ApplyToSchool system.
- v0.2.0 (2023-12-06)
  - Added config file in BepInEx/config folder.
  - Newly created citizens also follow the updated lifecycle thresholds.
- v0.1.1 (2023-12-05)
  - Changed icon to be more readable on Thunderstore.
- v0.1.0 (2023-12-05)
  - Initial build.

### Support
- Please report bugs and issues on [GitHub](https://github.com/Infixo/CS2-RealPop).
- You may also leave comments on [Discord1](https://discord.com/channels/1169011184557637825/1198628199207292929) or [Discord2](https://discord.com/channels/1024242828114673724/1183857600098480237).

## Disclaimers and Notes

> [!NOTE]
The mod uses Cities: Skylines 2 Mod Template by Captain-Of-Coit.

> [!IMPORTANT]
It will take one full in-game day for the population to adjust to the new thresholds. Education changes need more time, at least 3-5 days. You may wanna build a few extra High Schools until the levels will adjust.

> [!IMPORTANT]
Cims cannot go back to the previous phase of their lives, so changes done by this mod are irreversible. Make sure to have a savefile. Once the mod is deactivated, cims will follow default logic, so eventually the city will return to the vanilla state, but it will take several in-game days.

> [!NOTE]
The graduation logic implemented in the game is flawed, imho. Most students stay in schools for 1-2 days usually. The average time shown in the UI is totally incorrect (on many levels, both calculations and UI presentation).

> [!NOTE]
The timeline in CS2 is measured in days. Each in-game day also represents 1 month (e.g. when looking at average time spent in schools) but also can be treated as 1 year of cim life, which is much more reasonable than using months. Cims live around 100 days, give or take few, so it is pretty close to average fuman lifespan (80+ years).
The population structure is based on EU statistics data, population by age. 0-14 years is ~15%, which we can consider Children in CS2 reality. 15-24 years is 11%, which we can consider Teens. And 65+ years is 21%, which we can consider Seniors (65 is usual retirement age in EU). The rest are Adults.
