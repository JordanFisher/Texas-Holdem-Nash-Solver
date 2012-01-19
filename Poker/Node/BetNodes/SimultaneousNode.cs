using System;
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
            if (MakeHold) Hold = new PocketData();

            CreateBranches();
        }

        protected override void CreateBranches()
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

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            Update(PocketP, S, RaiseBranch.PocketP);

            base.UpdateChildrensPDFs(Opponent);
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

#if NAIVE
            /* Naive implementation. O(N^4) */
            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (number.IsNaN(PocketP[p1])) { B[p1] = float.NaN; continue; }
                //if (!MyCommunity.AvailablePocket[p1]) { B[p1] = float.NaN; continue; }
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Calculate the chance the opponent will raise/fold
                number RaiseChance = TotalChance(PocketP, S, p1);
                number FoldChance = 1 - RaiseChance;
                Assert.IsNum(RaiseChance);

                // Calculate EV for raising and folding.
                number RaiseEV = FoldChance * Pot + RaiseChance * RaiseBranch.EV[p1];
                number FoldEV = RaiseChance * (-Spent);

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
#else
            /* Optimal implementation. O(N^2) */
            // For each pocket we might have, calculate what we should do.
            Optimize.ChanceToActPrecomputation(PocketP, S, MyCommunity);
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (number.IsNaN(PocketP[p1])) { B[p1] = float.NaN; continue; }
                //if (!MyCommunity.AvailablePocket[p1]) { B[p1] = float.NaN; continue; }
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Calculate the chance the opponent will raise/fold
                number RaiseChance = Optimize.ChanceToActWithExclusion(PocketP, S, p1);
                number FoldChance = 1 - RaiseChance;
                Assert.IsNum(RaiseChance);

                // Calculate EV for raising and folding.
                number RaiseEV = FoldChance * Pot + RaiseChance * RaiseBranch.EV[p1];
                number FoldEV = RaiseChance * (-Spent);

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
#endif
        }

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data1 = S1(this), Data2 = S2(this);

            number BranchEV = RaiseBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);

            number EV =
                Data1[p1] * (Data2[p2] * BranchEV + (1 - Data2[p2]) * Pot) +
                (1 - Data1[p1]) * (Data2[p2] * (-Spent) + (1 - Data2[p2]) * 0);
            Assert.IsNum(EV);

            return EV;
        }

        public override Node AdvanceHead(PlayerAction action)
        {
            Assert.That(action == PlayerAction.Raise || action == PlayerAction.Fold);

            return RaiseBranch;
        }
    }
}
