using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class FlopJunction : Junction
    {
        public FlopJunction(Node parent, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            Phase = BettingPhase.Flop;
            Initialize();
        }

        protected override void CreateBranches()
        {
            Branches = new List<Node>(Flop.N);

            foreach (Flop flop in Flop.Flops)
                Branches.Add(new FlopRoot(this, flop, Spent, Pot));
            BranchesByIndex = Branches;
        }

        public override string ToString()
        {
            return base.ToString() + ", into the flop";
        }
    }
}
