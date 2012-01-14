using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class Junction : Node
    {
        public Junction(Node parent, CommunityNode Community, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            Weight = double.NaN;

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

            foreach (CommunityNode community in MyCommunity.BranchesByIndex)
            {
                PhaseRoot NewRoot = null;

                if (community != null)
                {
                    NewRoot = new PhaseRoot(this, community, Spent, Pot);
                    Branches.Add(NewRoot);
                }

                BranchesByIndex.Add(NewRoot);
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
            PocketP = new PocketData();
            EV = new PocketData();

            CreateBranches();
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            base.UpdateChildrensPDFs(Opponent);
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

            // Ignore pockets that collide with community
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (Collision(p)) { S[p] = PocketP[p] = EV[p] = B[p] = double.NaN; continue; }
            }

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) continue;

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PocketP, UpdatedP, p1);

                // Calculate the EV assuming we proceed to a branch.
                // Loop through each possible opponent pocket and then each possible branch,
                // summing up the EV of each branch times the probability of arriving there.
                double TotalWeight = 0, BranchEV = 0;
                double[] BranchPDF = new double[Branches.Count];
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (double.IsNaN(UpdatedP[p2])) continue;

                    // All branches not overlapping our pocket or the opponent's pocket are equally likely.
                    int b = 0;
                    foreach (Node Branch in Branches)
                    {
                        b++;
                        if (Branch.MyCommunity.NewCollision(p1) || Branch.MyCommunity.NewCollision(p2)) continue;

                        double Weight = Branch.Weight;
                        BranchEV += UpdatedP[p2] * Weight * Branch.EV[p1];
                        TotalWeight += UpdatedP[p2] * Weight;
                        BranchPDF[b - 1] += UpdatedP[p2] * Weight;
                    }
                }
                Assert.ZeroOrOne(BranchPDF.Sum());
                Assert.ZeroOrOne(TotalWeight);
                Assert.That(BranchEV >= -Ante.MaxPot - Tools.eps && BranchEV <= Ante.MaxPot + Tools.eps);

                // Optimize: the above loop is always the same, except for a different constant
                // multiplicative factor, and except for a few exclusions of some branches.
                // Do the full sum, no exclusions, and then go back and subtract the ones we don't need,
                // then correct for the multiplicative factor (which comes from renormalizing P
                // conditioned on knowing our pocket).

                EV[p1] = BranchEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
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
