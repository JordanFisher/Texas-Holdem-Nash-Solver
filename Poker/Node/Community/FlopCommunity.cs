using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class FlopCommunity : CommunityNode
    {
        public Flop MyFlop;

        public FlopCommunity(Flop flop)
            : base()
        {
            MyFlop = flop;

            Weight = 1f / Counting.Choose(Card.N - 4, 3);
            Phase = BettingPhase.Flop;
            InitiallyActivePlayer = Player.Dealer;

            CreateBranches();
        }

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Card.N - 3);
            BranchesByIndex = new List<CommunityNode>(Card.N);

            for (int turn = 0; turn < Card.N; turn++)
            {
                CommunityNode NewBranch;
                if (!Contains(turn))
                {
                    NewBranch = new TurnCommunity(MyFlop, turn);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Overlaps(MyFlop);
        }

        public override bool Collision(Pocket p)
        {
            return p.Overlaps(MyFlop);
        }

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card);
        }

        public override string ToString()
        {
            return Card.CommunityToString(MyFlop);
        }
    }
}
