﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NetTally.Utility;

namespace NetTally.VoteCounting
{
    // List of preference results ordered by winner
    using RankResults = List<string>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;


    public class CoombsRankVoteCounter : BaseRankVoteCounter
    {
        /// <summary>
        /// Local class to store a choice/count combo of fields for LINQ.
        /// </summary>
        protected class ChoiceCount
        {
            public string Choice { get; set; }
            public int Count { get; set; }

            public override string ToString() => $"{Choice}: {Count}";
        }

        /// <summary>
        /// Implementation to generate the ranking list for the provided set
        /// of votes for a specific task.
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list of winning votes.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            List<string> winningChoices = new List<string>();

            if (task.Any())
            {
                Debug.WriteLine(">>Coombs Runoff<<");

                var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
                var allChoices = GetAllChoices(voterRankings);

                for (int i = 1; i <= 9; i++)
                {
                    string winner = GetWinningVote(voterRankings, winningChoices, allChoices);
   
                    if (winner == null)
                        break;

                    winningChoices.Add(winner);
                    allChoices.Remove(winner);

                    Debug.WriteLine($"- {winner}");

                    if (!allChoices.Any())
                        break;
                }
            }

            return winningChoices;
        }

        /// <summary>
        /// Gets the winning vote.
        /// Excludes any already chosen votes from the process.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="chosenChoices">The already chosen choices.</param>
        /// <param name="allChoices">All remaining choices.</param>
        /// <returns>Returns the winning vote.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        private string GetWinningVote(IEnumerable<VoterRankings> voterRankings, RankResults chosenChoices, RankResults allChoices)
        {
            if (voterRankings == null)
                throw new ArgumentNullException(nameof(voterRankings));
            if (chosenChoices == null)
                throw new ArgumentNullException(nameof(chosenChoices));

            // Initial conversion from enumerable to list
            List<VoterRankings> localRankings = RemoveChoicesFromVotes(voterRankings, chosenChoices);

            AddUnselectedRankings(localRankings, allChoices);

            int voterCount = localRankings.Count();
            int winCount = voterCount / 2 + 1;

            try
            {
                Debug.Write("Eliminations: [");
                bool eliminateOne = false;

                while (true)
                {
                    var preferredVotes = GetPreferredCounts(localRankings);

                    if (!preferredVotes.Any())
                        break;

                    ChoiceCount best = preferredVotes.MaxObject(a => a.Count);

                    if (best.Count >= winCount)
                        return best.Choice;

                    // If no more choice removals will bump up lower prefs to higher prefs, return the best of what's left.
                    if (!localRankings.Any(r => r.RankedVotes.Count() > 1))
                        return best.Choice;

                    Debug.Write($"{Comma(eliminateOne)}");

                    string leastPreferredChoice = GetLeastPreferredChoice(localRankings);

                    Debug.Write($"{leastPreferredChoice}");

                    RemoveChoiceFromVotes(localRankings, leastPreferredChoice);
                    eliminateOne = true;
                }
            }
            finally
            {
                Debug.WriteLine("]");
            }

            return null;
        }

        private string Comma(bool addComma)
        {
            if (addComma)
                return ", ";
            else
                return "";
        }


        /// <summary>
        /// Removes a list of choices from voter rankings.
        /// These are the choices that have already won a rank spot.
        /// </summary>
        /// <param name="voterRankings">The voter rankings.</param>
        /// <param name="chosenChoices">The already chosen choices.</param>
        /// <returns>Returns the results as a list.</returns>
        private List<VoterRankings> RemoveChoicesFromVotes(IEnumerable<VoterRankings> voterRankings, List<string> chosenChoices)
        {
            var res = from voter in voterRankings
                      select new VoterRankings
                      {
                          Voter = voter.Voter,
                          RankedVotes = voter.RankedVotes.Where(v => chosenChoices.Contains(v.Vote) == false).OrderBy(v => v.Rank).ToList()
                      };

            return res.ToList();
        }

        /// <summary>
        /// Adds ranking entries for any choices that users did not explictly rank.
        /// Modifies the provided list.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <param name="allChoices">All available choices.</param>
        private void AddUnselectedRankings(List<VoterRankings> localRankings, RankResults allChoices)
        {
            foreach (var ranker in localRankings)
            {
                if (ranker.RankedVotes.Count == allChoices.Count)
                    continue;

                var extras = allChoices.Except(ranker.RankedVotes.Select(v => v.Vote));

                foreach (var extra in extras)
                {
                    ranker.RankedVotes.Add(new RankedVote { Vote = extra, Rank = 10 });
                }
            }
        }

        /// <summary>
        /// Filter the provided list of voter rankings to remove any instances of the specified choice.
        /// Modifies the provided list.
        /// </summary>
        /// <param name="voterRankings">The votes to filter.</param>
        /// <param name="choice">The choice to remove.</param>
        private void RemoveChoiceFromVotes(List<VoterRankings> voterRankings, string choice)
        {
            foreach (var ranker in voterRankings)
            {
                ranker.RankedVotes.RemoveAll(v => v.Vote == choice);
            }
        }

        /// <summary>
        /// Gets the least preferred choice.
        /// </summary>
        /// <param name="localRankings">The vote rankings.</param>
        /// <returns>Returns the vote string for the least preferred vote.</returns>
        private string GetLeastPreferredChoice(List<VoterRankings> localRankings)
        {
            Dictionary<string, double> rankCount = new Dictionary<string, double>();

            foreach (var voter in localRankings)
            {
                if (voter.RankedVotes.Any())
                {
                    int lowestRank = voter.RankedVotes.Max(a => a.Rank);

                    var lowestRankedOptions = voter.RankedVotes.Where(v => v.Rank == lowestRank);

                    int lowestRankCount = lowestRankedOptions.Count();

                    foreach (var lowest in lowestRankedOptions)
                    {
                        if (!rankCount.ContainsKey(lowest.Vote))
                            rankCount[lowest.Vote] = 0;

                        // Unranked options get fractional additions to the effective rank count.
                        if (lowestRank == 10)
                        {
                            rankCount[lowest.Vote] += 1.0 / lowestRankCount;
                        }
                        else
                        {
                            rankCount[lowest.Vote] += 1.0;
                        }
                    }
                }
            }

            var maxRank = rankCount.MaxObject(a => a.Value);

            Debug.Write($"({maxRank.Value}) ");

            return maxRank.Key;
        }

        /// <summary>
        /// Gets the count of the number of times a given vote is the most preferred option
        /// among the provided voters.
        /// </summary>
        /// <param name="voterRankings">The list of voters and their rankings of each option.</param>
        /// <returns>Returns a collection of Choice/Count objects.</returns>
        private IEnumerable<ChoiceCount> GetPreferredCounts(IEnumerable<VoterRankings> voterRankings)
        {
            var preferredVotes = from voter in voterRankings
                                 let preferred = voter.RankedVotes.FirstOrDefault()?.Vote
                                 where preferred != null
                                 group voter by preferred into preffed
                                 select new ChoiceCount { Choice = preffed.Key, Count = preffed.Count() };

            return preferredVotes;
        }

        /// <summary>
        /// Gets all choices from all user votes.
        /// </summary>
        /// <param name="rankings">The collection of user votes.</param>
        /// <returns>Returns a list of all the choices in the task.</returns>
        private List<string> GetAllChoices(IEnumerable<VoterRankings> rankings)
        {
            var res = rankings.SelectMany(r => r.RankedVotes).Select(r => r.Vote).Distinct();

            return res.ToList();
        }
    }
}
