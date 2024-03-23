using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace RealPop
{

    [FileLocation(nameof(RealPop))]
    [SettingsUIGroupOrder(kLifecycleGroup, kBirthGroup, kDeathGroup, kSchoolsGroup, kGraduationGroup, kNewCimsGroup, kResetGroup)]
    [SettingsUIShowGroupName(kLifecycleGroup, kBirthGroup, kDeathGroup, kSchoolsGroup, kGraduationGroup, kNewCimsGroup, kResetGroup)]
    public class Setting : ModSetting
    {
        public const string kSection1 = "Population";
        public const string kSection2 = "Education";
        public const string kSection3 = "Misc";

        public const string kLifecycleGroup = "Lifecycle";
        public const string kBirthGroup = "Birth";
        public const string kDeathGroup = "Death";
        public const string kSchoolsGroup = "Schools";
        public const string kGraduationGroup = "Graduation";
        public const string kNewCimsGroup = "NewCims";
        public const string kResetGroup = "Reset";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        // POPULATION

        // [Lifecycle]

        [SettingsUISection(kSection1, kLifecycleGroup)]
        [SettingsUISlider(min = 0, max = 25)]
        public int TeenAgeLimitInDays { get; set; }

        [SettingsUISection(kSection1, kLifecycleGroup)]
        [SettingsUISlider(min = 15, max = 40)]
        public int AdultAgeLimitInDays { get; set; }

        [SettingsUISection(kSection1, kLifecycleGroup)]
        [SettingsUISlider(min = 50, max = 100)]
        public int ElderAgeLimitInDays { get; set; }

        // [Birth]

        [SettingsUISection(kSection1, kBirthGroup)]
        [SettingsUISlider(min = 10, max = 50)]
        public int BirthChanceSingle { get; set; }

        [SettingsUISection(kSection1, kBirthGroup)]
        [SettingsUISlider(min = 50, max = 200, step = 5)]
        public int BirthChanceFamily { get; set; }

        [SettingsUISection(kSection1, kBirthGroup)]
        [SettingsUISlider(min = 80, max = 100, unit = Unit.kPercentage)]
        public int NextBirthChance { get; set; }

        // [Death]

        [SettingsUISection(kSection1, kDeathGroup)]
        [SettingsUISlider(min = 0, max = 10)]
        public int DeathChanceIncrease { get; set; }

        [SettingsUISection(kSection1, kDeathGroup)]
        [SettingsUISlider(min = 0, max = 100, step = 5, unit = Unit.kPercentage)]
        public int CorpseVanishChance { get; set; }

        // EDUCATION

        // [Schools]

        [SettingsUISection(kSection2, kSchoolsGroup)]
        [SettingsUISlider(min = 0, max = 10)]
        public int Education2InDays { get; set; } // high school

        [SettingsUISection(kSection2, kSchoolsGroup)]
        [SettingsUISlider(min = 0, max = 10)]
        public int Education3InDays { get; set; } // college

        [SettingsUISection(kSection2, kSchoolsGroup)]
        [SettingsUISlider(min = 0, max = 10)]
        public int Education4InDays { get; set; } // university

        // [Graduation]

        [SettingsUISection(kSection2, kGraduationGroup)]
        [SettingsUISlider(min = 0, max = 120, step = 5)]
        public int GraduationLevel1 { get; set; } // elementary school

        [SettingsUISection(kSection2, kGraduationGroup)]
        [SettingsUISlider(min = 0, max = 120, step = 5)]
        public int GraduationLevel2 { get; set; } // high school

        [SettingsUISection(kSection2, kGraduationGroup)]
        [SettingsUISlider(min = 0, max = 120, step = 5)]
        public int GraduationLevel3 { get; set; } // college

        [SettingsUISection(kSection2, kGraduationGroup)]
        [SettingsUISlider(min = 0, max = 120, step = 5)]
        public int GraduationLevel4 { get; set; } // university

        // MISC

        // [NewCims]

        [SettingsUISection(kSection3, kNewCimsGroup)]
        public bool NewAdultsAnyEducation { get; set; }

        [SettingsUISection(kSection3, kNewCimsGroup)]
        public bool NoChildrenWhenTooOld { get; set; }

        [SettingsUISection(kSection3, kNewCimsGroup)]
        public bool AllowTeenStudents { get; set; }

        [SettingsUISection(kSection3, kNewCimsGroup)]
        [SettingsUISlider(min = -1, max = 30)]
        public int FreeRatioTreshold { get; set; }

        [SettingsUISection(kSection3, kNewCimsGroup)]
        [SettingsUISlider(min = 0, max = 100)]
        public int FreeRatioFullSpeed { get; set; }

        [SettingsUISection(kSection3, kResetGroup)]
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ButtonDefault { set { SetDefaults(); } }

        [SettingsUISection(kSection3, kResetGroup)]
        [SettingsUIButton]
        [SettingsUIConfirmation]
        public bool ButtonVanilla { set { SetVanilla(); } }

        public override void SetDefaults()
        {
            // Lifecycle
            TeenAgeLimitInDays = 12; // Vanilla 21
            AdultAgeLimitInDays = 20; // Vanilla 36
            ElderAgeLimitInDays = 75; // Vanilla 84
            BirthChanceSingle = 35; // Base birth chance for a Single, rolled against 16000, 16x per day; Vanilla 20
            BirthChanceFamily = 120; // Base birth chance for a Family, rolled against 16000, 16x per day; Vanilla 100
            NextBirthChance = 97; // Set to less than 100 to lower the birth chance for each consecutive child; Vanilla 100
            DeathChanceIncrease = 3; // Increase in death chance per mille per year; set to 0 to turn off and use Vanilla process
            CorpseVanishChance = 50; // Percent chance for a corpse to vanish after death; Vanilla has no such feature, set to 0 to turn off
            // Education
            Education2InDays = 3; // How long High School should typically last (only for Teens)
            Education3InDays = 4; // How long College should typically last (for both Teens and Adults)
            Education4InDays = 5; // How long University should typically last (only for Adults)
            GraduationLevel1 = 90; // Elementary School; Vanilla 100
            GraduationLevel2 = 80; // High School; Vanilla 60
            GraduationLevel3 = 70; // College; Vanilla 90
            GraduationLevel4 = 60; // University; Vanilla 70
            // New Cims
            NewAdultsAnyEducation = true; // Allow for newly spawned Adults and Seniors to have any education level; Vanilla allows only Educated
            NoChildrenWhenTooOld = true; // Does not allow for Adults to have Children when they cannot raise them before becoming Senior; Vanilla doesn't have such a restriction
            AllowTeenStudents = true; // Allow for Teens ready for College to be spawned as Students; Vanilla spawns always Adults
            FreeRatioTreshold = 15; // Treshold for free properties ratio to start spawning new households (in 1/1000); Vanilla has no restrictions, set to -1 to turn off
            FreeRatioFullSpeed = 60; // Treshold for free properties ratio to spawn new households at full speed (in 1/1000); Vanilla has no restrictions
        }

        public void SetVanilla()
        {
            // Lifecycle
            TeenAgeLimitInDays = 21; // Vanilla 21
            AdultAgeLimitInDays = 36; // Vanilla 36
            ElderAgeLimitInDays = 84; // Vanilla 84
            BirthChanceSingle = 20; // Base birth chance for a Single, rolled against 16000, 16x per day; Vanilla 20
            BirthChanceFamily = 100; // Base birth chance for a Family, rolled against 16000, 16x per day; Vanilla 100
            NextBirthChance = 100; // Set to less than 100 to lower the birth chance for each consecutive child; Vanilla 100
            DeathChanceIncrease = 0; // Increase in death chance per mille per year; set to 0 to turn off and use Vanilla process
            CorpseVanishChance = 0; // Percent chance for a corpse to vanish after death; Vanilla has no such feature, set to 0 to turn off
            // Education
            Education2InDays = 0; // How long High School should typically last (only for Teens)
            Education3InDays = 0; // How long College should typically last (for both Teens and Adults)
            Education4InDays = 0; // How long University should typically last (only for Adults)
            GraduationLevel1 = 100; // Elementary School; Vanilla 100
            GraduationLevel2 = 60; // High School; Vanilla 60
            GraduationLevel3 = 90; // College; Vanilla 90
            GraduationLevel4 = 70; // University; Vanilla 70
            // New Cims
            NewAdultsAnyEducation = false; // Allow for newly spawned Adults and Seniors to have any education level; Vanilla allows only Educated
            NoChildrenWhenTooOld = false; // Does not allow for Adults to have Children when they cannot raise them before becoming Senior; Vanilla doesn't have such a restriction
            AllowTeenStudents = false; // Allow for Teens ready for College to be spawned as Students; Vanilla spawns always Adults
            FreeRatioTreshold = -1; // Treshold for free properties ratio to start spawning new households (in 1/1000); Vanilla has no restrictions, set to -1 to turn off
            FreeRatioFullSpeed = 0; // Treshold for free properties ratio to spawn new households at full speed (in 1/1000); Vanilla has no restrictions
        }

    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "Population Rebalance v0.9" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection1), "Population" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection2), "Education" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection3), "Misc" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kLifecycleGroup), "Lifecycle" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kBirthGroup), "Birth" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kDeathGroup), "Death" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSchoolsGroup), "Schools" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGraduationGroup), "Graduation" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kNewCimsGroup), "New Cims" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kResetGroup), "Reset" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TeenAgeLimitInDays)), "Teen age limit (in days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TeenAgeLimitInDays)), "When Children become Teens; Vanilla 21." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AdultAgeLimitInDays)), "Adult age limit (in days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AdultAgeLimitInDays)), "When Teens become Adults; Vanilla 36." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ElderAgeLimitInDays)), "Elder age limit (in days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ElderAgeLimitInDays)), "When Adults become Seniors; Vanilla 84." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BirthChanceSingle)), "Birth chance - Single" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BirthChanceSingle)), "Base birth chance for a Single, rolled against 16000, 16x per day; Vanilla 20." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BirthChanceFamily)), "Birth chance - Family" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.BirthChanceFamily)), "Base birth chance for a Family, rolled against 16000, 16x per day; Vanilla 100." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NextBirthChance)), "Next birth chance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NextBirthChance)), "Set to less than 100 to lower the birth chance for each consecutive child; Vanilla 100%." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DeathChanceIncrease)), "Death chance increase" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DeathChanceIncrease)), "Increase in death chance per mille per year; set to 0 to turn off and use Vanilla process." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorpseVanishChance)), "Corpse vanish chance" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorpseVanishChance)), "Percent chance for a corpse to vanish after death; Vanilla has no such feature, set to 0 to turn off." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Education2InDays)), "High School (in days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Education2InDays)), "How long High School should typically last (only for Teens)." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Education3InDays)), "College (in days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Education3InDays)), "How long College should typically last (for both Teens and Adults)." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Education4InDays)), "University (in days)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.Education4InDays)), "How long University should typically last (only for Adults)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GraduationLevel1)), "Elementary School" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GraduationLevel1)), "Elementary School graduation factor; Vanilla 100." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GraduationLevel2)), "High School" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GraduationLevel2)), "High School graduation factor; Vanilla 60." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GraduationLevel3)), "College" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GraduationLevel3)), "College graduation factor; Vanilla 90." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GraduationLevel4)), "University" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GraduationLevel4)), "University graduation factor; Vanilla 70." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NewAdultsAnyEducation)), "New Adults with any education" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NewAdultsAnyEducation)), "Allow for newly spawned Adults and Seniors to have any education level; Vanilla allows only Educated." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NoChildrenWhenTooOld)), "No Children when too old" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NoChildrenWhenTooOld)), "Does not allow for Adults to have Children when they cannot raise them before becoming Senior; Vanilla doesn't have such a restriction." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AllowTeenStudents)), "Allow Teen students" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AllowTeenStudents)), "Allow for Teens ready for College to be spawned as Students; Vanilla spawns always Adults." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FreeRatioTreshold)), "Free Ratio - treshold" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FreeRatioTreshold)), "Treshold for free properties ratio to start spawning new households (in 1/1000); Vanilla has no restrictions, set to -1 to turn off." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FreeRatioFullSpeed)), "Free Ratio - full speed" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FreeRatioFullSpeed)), "Treshold for free properties ratio to spawn new households at full speed (in 1/1000); Vanilla has no restrictions." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonDefault)), "Default Setting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonDefault)), "Resets setting to Default values." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonDefault)), "Confirm Default setting" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ButtonVanilla)), "Vanilla Setting" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ButtonVanilla)), "Resets setting to Vanilla values." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ButtonVanilla)), "Confirm Vanilla setting" },

                /*
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.)), "" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.)), "" },
                */
            };
        }

        public void Unload()
        {

        }
    }
}
