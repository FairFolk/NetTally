﻿using System;
using System.Configuration;
using System.Linq;
using NetTally.Collections;
using NetTally.Output;
using NetTally.Votes;

namespace NetTally
{
    /// <summary>
    /// Class to handle the section for storing quests in the user config file.
    /// Also stores the 'current' quest value, and global config options.
    /// </summary>
    public class QuestsSection : ConfigurationSection
    {
        #region Meta info
        /// <summary>
        /// Defined name of the config section, as saved in the config file.
        /// </summary>
        public const string SectionName = "NetTally.Quests";

        /// <summary>
        /// A list of all deprecated attributes.
        /// </summary>
        string[] deprecatedAttributes = new string[] { "IgnoreSymbols", "AllowVoteLabelPlanNames" };

        /// <summary>
        /// Gets a value indicating whether an unknown attribute is encountered during deserialization.
        /// Strict mode will throw an exception on unknown, non-deprecated attributes.
        /// Non-strict mode will ignore all unknown attributes.
        /// </summary>
        /// <param name="name">The name of the unrecognized attribute.</param>
        /// <param name="value">The value of the unrecognized attribute.</param>
        /// <returns>
        /// true when an unknown attribute is encountered while deserializing, and we don't want to throw
        /// an exception. Otherwise, false.
        /// </returns>
        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value)
        {
            if (deprecatedAttributes.Contains(name))
            {
                return true;
            }

            if (ConfigPrefs.Strict)
                return base.OnDeserializeUnrecognizedAttribute(name, value);
            else
                return true;
        }
        #endregion

        #region Properties
        [ConfigurationProperty("Quests", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(QuestElement))]
        public QuestElementCollection Quests
        {
            get { return (QuestElementCollection)this["Quests"]; }
            set { this["Quests"] = value; }
        }

        [ConfigurationProperty("CurrentQuest", DefaultValue = null)]
        public string CurrentQuest
        {
            get { return (string)this["CurrentQuest"]; }
            set { this["CurrentQuest"] = value; }
        }

        [ConfigurationProperty("AllowRankedVotes", DefaultValue = true)]
        public bool AllowRankedVotes
        {
            get { return (bool)this["AllowRankedVotes"]; }
            set { this["AllowRankedVotes"] = value; }
        }

        [ConfigurationProperty("WhitespaceAndPunctuationIsSignificant", DefaultValue = false)]
        public bool WhitespaceAndPunctuationIsSignificant
        {
            get { return (bool)this["WhitespaceAndPunctuationIsSignificant"]; }
            set { this["WhitespaceAndPunctuationIsSignificant"] = value; }
        }

        [ConfigurationProperty("ForbidVoteLabelPlanNames", DefaultValue = false)]
        public bool ForbidVoteLabelPlanNames
        {
            get { return (bool)this["ForbidVoteLabelPlanNames"]; }
            set { this["ForbidVoteLabelPlanNames"] = value; }
        }

        [ConfigurationProperty("DisableProxyVotes", DefaultValue = false)]
        public bool DisableProxyVotes
        {
            get { return (bool)this["DisableProxyVotes"]; }
            set { this["DisableProxyVotes"] = value; }
        }

        [ConfigurationProperty("ForcePinnedProxyVotes", DefaultValue = false)]
        public bool ForcePinnedProxyVotes
        {
            get { return (bool)this["ForcePinnedProxyVotes"]; }
            set { this["ForcePinnedProxyVotes"] = value; }
        }

        [ConfigurationProperty("TrimExtendedText", DefaultValue = false)]
        public bool TrimExtendedText
        {
            get { return (bool)this["TrimExtendedText"]; }
            set { this["TrimExtendedText"] = value; }
        }
        [ConfigurationProperty("IgnoreSpoilers", DefaultValue = false)]
        public bool IgnoreSpoilers
        {
            get { return (bool)this["IgnoreSpoilers"]; }
            set { this["IgnoreSpoilers"] = value; }
        }

        [ConfigurationProperty("GlobalSpoilers", DefaultValue = false)]
        public bool GlobalSpoilers
        {
            get { return (bool)this["GlobalSpoilers"]; }
            set { this["GlobalSpoilers"] = value; }
        }

        [ConfigurationProperty("DisplayPlansWithNoVotes", DefaultValue = false)]
        public bool DisplayPlansWithNoVotes
        {
            get { return (bool)this["DisplayPlansWithNoVotes"]; }
            set { this["DisplayPlansWithNoVotes"] = value; }
        }

        [ConfigurationProperty("DisplayMode", DefaultValue = DisplayMode.Normal)]
        public DisplayMode DisplayMode
        {
            get
            {
                try
                {
                    return (DisplayMode)this["DisplayMode"];
                }
                catch (ConfigurationException)
                {
                    return DisplayMode.Normal;
                }
            }
            set { this["DisplayMode"] = value; }
        }
        #endregion

        #region Loading and saving        
        /// <summary>
        /// Loads the configuration information into the provided quest wrapper.
        /// Global information is stored in the advanced options instance.
        /// </summary>
        /// <param name="questWrapper">The quest wrapper.</param>
        public void Load(QuestCollectionWrapper questWrapper)
        {
            if (questWrapper == null)
                throw new ArgumentNullException(nameof(questWrapper));

            AdvancedOptions.Instance.DisplayMode = DisplayMode;
            AdvancedOptions.Instance.AllowRankedVotes = AllowRankedVotes;
            AdvancedOptions.Instance.IgnoreSpoilers = IgnoreSpoilers;
            AdvancedOptions.Instance.TrimExtendedText = TrimExtendedText;
            AdvancedOptions.Instance.GlobalSpoilers = GlobalSpoilers;
            AdvancedOptions.Instance.DisableProxyVotes = DisableProxyVotes;
            AdvancedOptions.Instance.ForcePinnedProxyVotes = ForcePinnedProxyVotes;
            AdvancedOptions.Instance.ForbidVoteLabelPlanNames = ForbidVoteLabelPlanNames;
            AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant = WhitespaceAndPunctuationIsSignificant;
            AdvancedOptions.Instance.DisplayPlansWithNoVotes = DisplayPlansWithNoVotes;

            questWrapper.CurrentQuest = CurrentQuest;

            foreach (QuestElement questElement in Quests)
            {
                try
                {
                    IQuest q = new Quest
                    {
                        DisplayName = questElement.DisplayName,
                        ThreadName = questElement.ThreadName,
                        PostsPerPage = questElement.PostsPerPage,
                        StartPost = questElement.StartPost,
                        EndPost = questElement.EndPost,
                        CheckForLastThreadmark = questElement.CheckForLastThreadmark,
                        PartitionMode = questElement.PartitionMode,
                        UseCustomThreadmarkFilters = questElement.UseCustomThreadmarkFilters,
                        CustomThreadmarkFilters = questElement.CustomThreadmarkFilters,
                        UseCustomUsernameFilters = questElement.UseCustomUsernameFilters,
                        CustomUsernameFilters = questElement.CustomUsernameFilters,
                        UseCustomPostFilters = questElement.UseCustomPostFilters,
                        CustomPostFilters = questElement.CustomPostFilters
                    };

                    questWrapper.QuestCollection.Add(q);
                }
                catch (Exception)
                {
                    continue;
                }
            }
        }

        /// <summary>
        /// Saves the information from the provided quest wrapper into this config object.
        /// Also pulls global advanced options.
        /// </summary>
        /// <param name="questWrapper">The quest wrapper.</param>
        public void Save(QuestCollectionWrapper questWrapper)
        {
            if (questWrapper == null)
                throw new ArgumentNullException(nameof(questWrapper));

            DisplayMode = AdvancedOptions.Instance.DisplayMode;
            AllowRankedVotes = AdvancedOptions.Instance.AllowRankedVotes;
            IgnoreSpoilers = AdvancedOptions.Instance.IgnoreSpoilers;
            TrimExtendedText = AdvancedOptions.Instance.TrimExtendedText;
            GlobalSpoilers = AdvancedOptions.Instance.GlobalSpoilers;
            DisableProxyVotes = AdvancedOptions.Instance.DisableProxyVotes;
            ForcePinnedProxyVotes = AdvancedOptions.Instance.ForcePinnedProxyVotes;
            ForbidVoteLabelPlanNames = AdvancedOptions.Instance.ForbidVoteLabelPlanNames;
            WhitespaceAndPunctuationIsSignificant = AdvancedOptions.Instance.WhitespaceAndPunctuationIsSignificant;
            DisplayPlansWithNoVotes = AdvancedOptions.Instance.DisplayPlansWithNoVotes;

            CurrentQuest = questWrapper.CurrentQuest;

            Quests.Clear();
            foreach (var quest in questWrapper.QuestCollection)
                Quests.Add(quest);
        }
        #endregion
    }
}
