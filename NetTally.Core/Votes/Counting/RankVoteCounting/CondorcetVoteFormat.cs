using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NetTally.Utility;
using NetTally.VoteCounting.RankVoteCounting.Utility;
using System.Text.RegularExpressions;

namespace NetTally.VoteCounting.RankVoteCounting
{
    // Vote (string), collection of voters
    using SupportedVotes = Dictionary<string, HashSet<string>>;
    // Task (string group), collection of votes (string vote, hashset of voters)
    using GroupedVotesByTask = IGrouping<string, KeyValuePair<string, HashSet<string>>>;

    /// <summary>
    /// Formats votes for http://democratix.dbai.tuwien.ac.at/
    /// </summary>
    /// <seealso cref="NetTally.VoteCounting.BaseRankVoteCounter" />
    public class CondorcetVoteFormat : BaseRankVoteCounter, IVoteFormat
    {
        /// <summary>
        /// Implementation to generate input for Condorcet.Vote
        /// </summary>
        /// <param name="task">The task that the votes are grouped under.</param>
        /// <returns>Returns a ranking list where the first entry is the input for Condorcet.Vote.</returns>
        protected override RankResults RankTask(GroupedVotesByTask task)
        {
            // The voterRankings are used for the runoff
            var voterRankings = GroupRankVotes.GroupByVoterAndRank(task);
            // The full choices list is just to keep track of how many we have left.
            var allChoices = GroupRankVotes.GetAllChoices(voterRankings);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Warning: Candidate names are stripped of non-alphanumeric characters and reduced to 30 characters. If this results in multiple idendical candidates, Condorcet.Vote might not work.");
            sb.AppendLine("Copy the following into the \"Add Candidates\" field:\n");
            
            foreach(String s in allChoices)
            {
                String app = FormatVote(s);
                sb.Append(app);
                sb.Append(" ; ");
            }
            sb.Length -= 3;
            sb.AppendLine("\n");


            sb.AppendLine("\nCopy the following into the \"Add Vote(s)\" field:\n");

            Dictionary<String, Tuple<int, String>> collect = new Dictionary<String, Tuple<int, String>>();
            foreach (VoterRankings vr in voterRankings)
            {
                StringBuilder temp = new StringBuilder();
                vr.RankedVotes.Sort((x, y) => x.Rank - y.Rank);
                RankedVote first = vr.RankedVotes.First();
                int r = first.Rank;
                String app = FormatVote(first.Vote);
                temp.Append(app);
                vr.RankedVotes.RemoveAt(0);

                foreach (RankedVote rv in vr.RankedVotes)
                {
                    if (rv.Rank > r)
                        temp.Append(" > ");
                    else
                        temp.Append(" = ");
                    r = rv.Rank;

                    app = FormatVote(rv.Vote);
                    temp.Append(app);
                }
                String s = temp.ToString();
                Tuple<int, String> data;
                if (collect.TryGetValue(s, out data))
                {
                    Tuple<int, String> ndata = new Tuple<int, string>(data.Item1 + 1, String.Concat(data.Item2, ",", FormatVoter(vr.Voter)));
                    collect[s] = ndata;
                }
                else
                {
                    data = new Tuple<int, string>(1, FormatVoter(vr.Voter));
                    collect.Add(s, data);
                }
            }
            foreach (KeyValuePair<String, Tuple<int, String>> kv in collect)
            {
                sb.Append(kv.Value.Item2);
                sb.Append(" || ");
                sb.Append(kv.Key);
                sb.Append(" * ");
                sb.Append(kv.Value.Item1);
                sb.AppendLine("");
            }

            RankResults output = new RankResults();
            output.Add(new RankResult(sb.ToString(), "CondorcetVote Format Output"));
            return output;
        }

        private String FormatVote(String Vote)
        {
            String tr = Regex.Replace(Vote, @"[^A-Za-z0-9\ ]+", "");
            if (tr.Length > 30)
                tr = tr.Substring(0, 30);
            return tr;
        }

        private String FormatVoter(String Voter)
        {
            return Regex.Replace(Voter, @"[^A-Za-z0-9\ ]+", "");
        }
    }
}
