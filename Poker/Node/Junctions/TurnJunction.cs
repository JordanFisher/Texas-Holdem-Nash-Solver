using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class TurnJunction : Junction
    {
        public TurnJunction(Node parent, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            Phase = BettingPhase.Turn;
            Initialize();
        }

        public override void CreateBranches()
        {
            Branches = new List<Node>(Card.N - 3);
            BranchesByIndex = new List<Node>(Card.N);

            for (int turn = 0; turn < Card.N; turn++)
            {
                Node NewBranch;
                Assert.That(MyPhaseRoot is FlopRoot);
                //if (!MyFlop.Contains(turn))
                if (!MyPhaseRoot.Contains(turn))
                {
                    NewBranch = new TurnRoot(this, turn, Spent, Pot);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override string ToString()
        {
            return base.ToString() + ", into the turn";
        }
    }
}
