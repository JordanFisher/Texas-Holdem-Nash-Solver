using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class CommunityRoot : CommunityNode
    {
        public CommunityRoot()
            : base()
        {
            Phase = BettingPhase.PreFlop;
            InitiallyActivePlayer = Player.Button;

            CreateBranches();
        }

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Flop.N);

            foreach (Flop flop in Flop.Flops)
                Branches.Add(new FlopCommunity(flop));
            BranchesByIndex = Branches;
        }
    }
}
