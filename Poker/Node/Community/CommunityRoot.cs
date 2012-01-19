﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
#if SINGLE
	using number = Single;
#elif DOUBLE
	using number = Double;
#elif DECIMAL
	using number = Decimal;
#endif

    class CommunityRoot : CommunityNode
    {
        public CommunityRoot()
            : base()
        {
            ClassifyAvailability();

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
