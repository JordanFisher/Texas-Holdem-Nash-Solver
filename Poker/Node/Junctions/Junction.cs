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

    class Junction : Node
    {
        public Junction(Node parent, CommunityNode Community, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            Weight = 0;

            this.Spent = Spent;
            this.Pot = Pot;
            Assert.That(Spent == Pot);

            MyCommunity = Community;
            Phase = MyCommunity.Phase;

            Initialize();
        }

        protected override void CreateBranches()
        {
            Branches = new List<Node>(MyCommunity.Branches.Count);
            BranchesByIndex = new List<Node>(MyCommunity.BranchesByIndex.Count);

            int RootCount = 0;
            foreach (CommunityNode community in MyCommunity.BranchesByIndex)
            {
                PhaseRoot NewRoot = null;

                if (community != null)
                {
                    if (Phase == BettingPhase.PreFlop)
                        NewRoot = new FlopRoot(this, community, Spent, Pot, RootCount);
                    else
                        NewRoot = new PhaseRoot(this, community, Spent, Pot, RootCount);
                    Branches.Add(NewRoot);
                }

                BranchesByIndex.Add(NewRoot);
                RootCount++;
            }
        }

        //public static Node GetJunction(BettingPhase Phase, Node parent, int Spent, int Pot)
        //{
        //    return new PocketShowdownNode(parent, Pot);
        //}
        public static Junction GetJunction(BettingPhase Phase, Node Parent, int Spent, int Pot)
        {
            return new Junction(Parent, Parent.MyCommunity, Spent, Pot);
        }

        protected override void Initialize()
        {
            //PocketP = new PocketData();
            //EV = new PocketData();

            CreateBranches();
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            base.UpdateChildrensPDFs(Opponent);
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
#if NAIVE
            CalculateBestAgainst_Naive(Opponent);
#else
            if (Phase == BettingPhase.Turn || Phase == BettingPhase.Flop)
                CalculateBestAgainst_SingleCardOptimized(Opponent);
            else
#if SUIT_REDUCE
                CalculateBestAgainst_FlopSuitReduced(Opponent);
#else
                CalculateBestAgainst_Naive(Opponent);
#endif
#endif
        }

        static number[] IntersectP = new number[Card.N];
        void CalculateBestAgainst_SingleCardOptimized(Player Opponent)
        {
            RecursiveBest(Opponent);

            // Calculate P(Opponent pocket intersects a given card)
            for (int c = 0; c < Card.N; c++)
            {
                if (BranchesByIndex[c] == null) continue;

                IntersectP[c] = 0;
                for (int c2 = 0; c2 < Card.N; c2++)
                {
                    if (c == c2) continue;

                    int p = Game.PocketLookup[c, c2];
                    if (!MyCommunity.AvailablePocket[p]) continue;

                    IntersectP[c] += PocketP[p];
                }
            }

            Optimize.Data.ProbabilityPrecomputation(PocketP, MyCommunity);

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (!MyCommunity.AvailablePocket[p1]) continue;

                number BranchEV = 0;
                for (int c = 0; c < Card.N; c++)
                {
                    Node Branch = BranchesByIndex[c];
                    if (Branch == null) continue;
                    if (!Branch.MyCommunity.AvailablePocket[p1]) continue;

                    // Find opponent pockets that intersect our pocket and the community card c
                    var Pocket1 = Pocket.Pockets[p1];
                    int p2_1 = Game.PocketLookup[Pocket1.Cards[0], c];
                    int p2_2 = Game.PocketLookup[Pocket1.Cards[1], c];

                    number Correction = Optimize.Data.MassAfterExclusion(PocketP, p1);
                    number Pr = Correction - IntersectP[c] + PocketP[p2_1] + PocketP[p2_2];
                    if (Pr <= Tools.eps) continue;
                    Assert.That(Correction > Tools.eps);
                    Pr /= Correction;

                    Assert.AlmostProbability(Pr);
                    Assert.That(Branch.Weight > 0);
                    Assert.IsNum(Pr);
                    Pr = Tools.Restrict(Pr);
                    BranchEV += Branch.EV[p1] * Pr * Branch.Weight;
                }

                EV[p1] = BranchEV;
                Assert.IsNum(EV[p1]);
            }
        }

#if SUIT_REDUCE
        void CalculateBestAgainst_FlopSuitReduced(Player Opponent)
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PocketP, UpdatedP, p1);

                // Calculate the EV assuming we proceed to a branch.
                // Loop through each possible opponent pocket and then each possible branch,
                // summing up the EV of each branch times the probability of arriving there.
                number TotalWeight = 0, BranchEV = 0;
                number[] BranchPDF = new number[Branches.Count];
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (!MyCommunity.AvailablePocket[p2]) continue;

                    // All branches not overlapping our pocket or the opponent's pocket are equally likely.
                    int b = 0;
                    foreach (Node Branch in Branches)
                    {
                        b++;
                        if (Branch.MyCommunity.NewCollision(p1) || Branch.MyCommunity.NewCollision(p2)) continue;

                        FlopRoot FlopBranch = (FlopRoot)Branch;
                        FlopRoot _Branch = FlopBranch.Representative;
                        int _p1 = FlopBranch.MyFlop.PocketMap[p1];

                        //Assert.AlmostEqual(Branch.EV[p1], _Branch.EV[_p1], .05);

                        number Weight = _Branch.Weight;
                        BranchEV += UpdatedP[p2] * Weight * _Branch.EV[_p1];
                        TotalWeight += UpdatedP[p2] * Weight;
                        BranchPDF[b - 1] += UpdatedP[p2] * Weight;
                    }
                }
                Assert.ZeroOrOne(BranchPDF.Sum());
                Assert.ZeroOrOne(TotalWeight);
                Assert.That(BranchEV >= -Ante.MaxPot - Tools.eps && BranchEV <= Ante.MaxPot + Tools.eps);
                
                EV[p1] = BranchEV;
                Assert.IsNum(EV[p1]);
            }
        }
#endif
        void CalculateBestAgainst_Naive(Player Opponent)
        {
            // First decide strategy for children nodes.
            RecursiveBest(Opponent);

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PocketP, UpdatedP, p1);

                // Calculate the EV assuming we proceed to a branch.
                // Loop through each possible opponent pocket and then each possible branch,
                // summing up the EV of each branch times the probability of arriving there.
                number TotalWeight = 0, BranchEV = 0;
                number[] BranchPDF = new number[Branches.Count];
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    //if (number.IsNaN(UpdatedP[p2])) continue;
                    if (!MyCommunity.AvailablePocket[p2]) continue;

                    // All branches not overlapping our pocket or the opponent's pocket are equally likely.
                    int b = 0;
                    foreach (Node Branch in Branches)
                    {
                        b++;
                        //if (Branch.MyCommunity.NewCollision(p1) || Branch.MyCommunity.NewCollision(p2)) continue;
                        if (!Branch.MyCommunity.AvailablePocket[p1] || !Branch.MyCommunity.AvailablePocket[p2]) continue;

                        number Weight = Branch.Weight;
                        BranchEV += UpdatedP[p2] * Weight * Branch.EV[p1];
                        TotalWeight += UpdatedP[p2] * Weight;
                        BranchPDF[b - 1] += UpdatedP[p2] * Weight;
                    }
                }
                Assert.ZeroOrOne(BranchPDF.Sum());
                Assert.ZeroOrOne(TotalWeight);
                Assert.That(BranchEV >= -Ante.MaxPot - Tools.eps && BranchEV <= Ante.MaxPot + Tools.eps);

                EV[p1] = BranchEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            Node Branch = BranchesByIndex[BranchIndex[IndexOffset]];
            return Branch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset + 1);
        }

        public override Node AdvanceHead(int index)
        {
            return BranchesByIndex[index].AdvanceHead(PlayerAction.Nothing);
        }

        public override string ToString()
        {
            Assert.That(Pot == Spent);
            return string.Format("{0} bet", Pot);
        }
    }
}