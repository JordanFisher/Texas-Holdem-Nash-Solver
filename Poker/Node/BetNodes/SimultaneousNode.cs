using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class SimultaneousNode : Node
    {
        protected Node RaiseBranch;

        public SimultaneousNode(Node parent, int Spent, int Pot, BettingPhase Phase)
            : base(parent, Spent, Pot)
        {
            this.Phase = Phase;
            Initialize();
        }

        protected override void Initialize()
        {
            PocketP = new PocketData();
            EV = new PocketData();

            S = new PocketData();
            B = new PocketData();
            Hold = new PocketData();

            CreateBranches();
        }

        public override void CreateBranches()
        {
            int NewPot = Pot;
            switch (Phase)
            {
                case BettingPhase.PreFlop: NewPot += Ante.PreFlop; break;
                case BettingPhase.Flop: NewPot += Ante.Flop; break;
                case BettingPhase.Turn: NewPot += Ante.Turn; break;
                case BettingPhase.River: NewPot += Ante.River; break;
            }

            if (Phase == BettingPhase.River)
                RaiseBranch = new ShowdownNode(this, NewPot);
            else
                RaiseBranch = Junction.GetJunction(Phase, this, NewPot, NewPot);

            Branches = new List<Node>(1);
            Branches.Add(RaiseBranch);
        }

        protected override void UpdateChildrensPDFs()
        {
            Update(PocketP, S, RaiseBranch.PocketP);

            base.UpdateChildrensPDFs();
        }

        public override void CalculateBest()
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBest();

            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) { B[p1] = float.NaN; continue; }

                // Calculate the chance the opponent will raise/fold
                double RaiseChance = TotalChance(PocketP, S, p1);
                double FoldChance = 1 - RaiseChance;
                Assert.IsNum(RaiseChance);

                // Calculate EV for raising and folding.
                double RaiseEV = FoldChance * Pot + RaiseChance * RaiseBranch.EV[p1];
                double FoldEV = RaiseChance * (-Spent);

                // Decide strategy based on which action is better.
                if (RaiseEV >= FoldEV)
                {
                    B[p1] = 1;
                    EV[p1] = RaiseEV;
                }
                else
                {
                    B[p1] = 0;
                    EV[p1] = FoldEV;
                }
                Assert.IsNum(EV[p1]);
            }
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data1 = S1(this), Data2 = S2(this);

            double BranchEV = RaiseBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);

            double EV =
                Data1[p1] * (Data2[p2] * BranchEV + (1 - Data2[p2]) * Pot) +
                (1 - Data1[p1]) * (Data2[p2] * (-Spent) + (1 - Data2[p2]) * 0);
            Assert.IsNum(EV);

            return EV;
        }
    }
}
