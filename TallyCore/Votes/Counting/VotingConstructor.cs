﻿using System;
using System.Collections.Generic;

namespace NetTally.Votes.Experiment
{
    public static class VotingConstructor
    {
        public static List<(string planName, PlanType planType, string plan)> GetPlansFromPost(PostComponents post, IQuest quest)
        {
            throw new NotImplementedException();
        }

        internal static List<string> GetWorkingVote(PostComponents post)
        {
            throw new NotImplementedException();
        }

        internal static bool ProcessPost(PostComponents post, IQuest currentQuest)
        {
            throw new NotImplementedException();
        }
    }
}
