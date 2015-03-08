﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace NetTally
{
    class Tally : INotifyPropertyChanged
    {
        const string SVPostURL = "http://forums.sufficientvelocity.com/posts/";

        IPageProvider pageProvider;
        IVoteCounter voteCounter;

        public Tally(IPageProvider pageProvider, IVoteCounter voteCounter)
        {
            this.pageProvider = pageProvider;
            this.voteCounter = voteCounter;

            this.pageProvider.StatusChanged += PageProvider_StatusChanged;
        }

        private void PageProvider_StatusChanged(object sender, MessageEventArgs e)
        {
            TallyResults = TallyResults + e.Message;
        }


        #region Property update notifications
        /// <summary>
        /// Event for INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Function to raise events when a property has been changed.
        /// </summary>
        /// <param name="propertyName">The name of the property that was modified.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Functions for resetting stuff
        /// <summary>
        /// Allow manual clearing of the page cache.
        /// </summary>
        public void ClearPageCache()
        {
            pageProvider.ClearPageCache();
        }
        #endregion

        #region Behavior properties
        string results = string.Empty;
        /// <summary>
        /// Property for the string containing the current tally progress or results.
        /// </summary>
        public string TallyResults
        {
            get { return results; }
            set
            {
                results = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag for whether to use vote partitioning when tallying votes.
        /// Does not need to call OnPropertyChanged for binding.
        /// </summary>
        public bool UseVotePartitions
        {
            get
            {
                return voteCounter.UseVotePartitions;
            }
            set
            {
                voteCounter.UseVotePartitions = value;
            }
        }

        /// <summary>
        /// Flag for whether to use by-line or by-block partitioning, if
        /// partitioning votes during the tally.
        /// </summary>
        public bool PartitionByLine
        {
            get
            {
                return voteCounter.PartitionByLine;
            }
            set
            {
                voteCounter.PartitionByLine = value;
            }
        }
        #endregion

        /// <summary>
        /// Run the actual tally.
        /// </summary>
        /// <param name="questTitle">The name of the quest thread to scan.</param>
        /// <param name="startPost">The starting post number.</param>
        /// <param name="endPost">The ending post number.</param>
        /// <returns></returns>
        public async Task Run(string questTitle, int startPost, int endPost)
        {
            TallyResults = string.Empty;

            // Load pages from the website
            var pages = await pageProvider.LoadPages(questTitle, startPost, endPost).ConfigureAwait(false);

            // Tally the votes from the loaded pages.
            voteCounter.TallyVotes(pages, startPost, endPost);

            // Compose the final result string from the compiled votes.
            ConstructResults();
        }

        /// <summary>
        /// Compose the stored results into a string to put in the Results property.
        /// </summary>
        private void ConstructResults()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("[b]Vote Tally[/b]");
            sb.AppendFormat("[color=transparent]##### {0} {1}[/color]",
                System.Windows.Forms.Application.ProductName,
                System.Windows.Forms.Application.ProductVersion);
            sb.AppendLine("");

            foreach (var vote in voteCounter.VotesWithSupporters)
            {
                sb.Append(vote.Key);

                sb.Append("[b]No. of Votes: ");
                sb.Append(vote.Value.Count);
                sb.AppendLine("[/b]");


                foreach (var supporter in vote.Value)
                {
                    sb.Append("[url=\"");
                    sb.Append(SVPostURL);
                    sb.Append(voteCounter.VoterMessageId[supporter]);
                    sb.Append("/\"]");
                    sb.Append(supporter);
                    sb.AppendLine("[/url]");
                }

                sb.AppendLine("");
            }

            TallyResults = sb.ToString();
        }
    }        
}
