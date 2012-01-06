using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class RiverJunction : Junction
    {
        public RiverJunction(Node parent, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            Phase = BettingPhase.River;
            Initialize();
        }

        public override void CreateBranches()
        {
            Branches = new List<Node>(Card.N - 4);
            BranchesByIndex = new List<Node>(Card.N);

            for (int river = 0; river < Card.N; river++)
            {
                Node NewBranch;
                Assert.That(MyPhaseRoot is TurnRoot);
                if (!MyPhaseRoot.Contains(river))
                //if (!MyFlop.Contains(river) && river != MyTurn)
                {
                    NewBranch = new RiverRoot(this, river, Spent, Pot);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override string ToString()
        {
            return base.ToString() + ", into the River";
        }
    }
}
