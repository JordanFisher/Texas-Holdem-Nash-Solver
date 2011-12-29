using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class Node
    {
        public Node Parent;
        public double Weight;

        protected int Spent, Pot;

        public PocketData S, PreRaiseP, PostRaiseP, EV, B;
        public double ChanceToRaise;

        public Node(Node parent) { Parent = parent; Spent = Pot = 0; }

        protected void Initialize()
        {
            S = new PocketData();
            PreRaiseP = new PocketData();
            PostRaiseP = new PocketData();
            EV = new PocketData();
            B = new PocketData();

            CreateBranches();
        }

        public void BToS()
        {
            for (int i = 0; i < Pocket.N; i++)
            {
                S[i] = B[i];
                B[i] = PostRaiseP[i] = EV[i] = 0;
            }

            if (Branches == null) return;

            foreach (Node node in Branches)
                node.BToS();
        }

        public void HarmonicAlg(int n)
        {
            n++;
            double t = 1f / n;
            double s = 1f - t;

            for (int i = 0; i < Pocket.N; i++)
            {
                S[i] = t * B[i] + s * S[i];
                B[i] = PostRaiseP[i] = EV[i] = 0;
            }

            if (Branches == null) return;

            foreach (Node node in Branches)
                node.HarmonicAlg(n);
        }

        public void Switch()
        {
            for (int i = 0; i < Pocket.N; i++)
            {
                double temp = B[i];
                B[i] = S[i];
                S[i] = temp;
            }

            if (Branches == null) return;

            foreach (Node node in Branches)
                node.Switch();
        }

        public void Process(Func<int, double> PocketMod)
        {
            for (int i = 0; i < Pocket.N; i++)
                S[i] = PocketMod(i);

            if (Branches == null) return;

            foreach (Node node in Branches)
                node.Process(PocketMod);
        }

        public void Process(Func<Node, int, double> PocketMod)
        {
            for (int i = 0; i < Pocket.N; i++)
                S[i] = PocketMod(this, i);

            if (Branches == null) return;

            foreach (Node node in Branches)
                node.Process(PocketMod);
        }

        /// <summary>
        /// Calculate the best possible strategy B against S.
        /// Should recursively calculate B for all branches as well.
        /// This is a purely virtual function. All node subclasses 
        /// </summary>
        public virtual void CalculateBest()
        {
            Assert.NotReached();
        }

        /// <summary>
        /// Calculate the best possible strategy B against S.
        /// This is a generic function calculating B assuming this node
        /// has a branching structure introducing new community information.
        /// </summary>
        /// <param name="BranchWeight"></param>
        public void CalculateBest_AccountForOverlaps(double BranchWeight)
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBest();

            // Ignore pockets that collide with community
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p)) { S[p] = PreRaiseP[p] = PostRaiseP[p] = EV[p] = B[p] = double.NaN; continue; }
            }

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PostRaiseP[p1])) continue;

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PostRaiseP, UpdatedP, p1);

                // Calculate the EV assuming we proceed to a branch.
                // Loop through each possible opponent pocket and then each possible branch,
                // summing up the EV of each branch times the probability of arriving there.
                double TotalWeight = 0, BranchEV = 0;
                double[] branchp = new double[Branches.Count];
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (double.IsNaN(UpdatedP[p2])) continue;

                    // All branches not overlapping our pocket or the opponent's pocket are equally likely.
                    int b = 0;
                    foreach (Node Branch in Branches)
                    {
                        b++;
                        if (Branch.NewCollision(p1) || Branch.NewCollision(p2)) continue;

                        if (UpdatedP[p2] * BranchWeight > 0) { int x = 1; x += 1; }

                        BranchEV += UpdatedP[p2] * BranchWeight * Branch.EV[p1];
                        TotalWeight += UpdatedP[p2] * BranchWeight;
                        branchp[b-1] += UpdatedP[p2] * BranchWeight;
                    }
                }
                Assert.ZeroOrOne(branchp.Sum());
                Assert.ZeroOrOne(TotalWeight);
                Assert.That(BranchEV >= -Ante.MaxPot -Tools.eps && BranchEV <= Ante.MaxPot + Tools.eps);

                // Optimize: the above loop is always the same, except for a different constant
                // multiplicative factor, and except for a few exclusions of some branches.
                // Do the full sum, no exclusions, and then go back and subtract the ones we don't need,
                // then correct for the multiplicative factor (which comes from renormalizing P
                // conditioned on knowing our pocket).

                // Calculate the chance the opponent will raise/fold
                double RaiseChance = TotalChance(PreRaiseP, S, p1);
                double FoldChance = 1 - RaiseChance;
                Assert.IsNum(RaiseChance);

                // Calculate EV for raising and folding.
                double RaiseEV = FoldChance * Pot + RaiseChance * BranchEV;
                double FoldEV = RaiseChance * (-Spent);

                if (this is PocketNode) Tools.Nothing();

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

        /// <summary>
        /// Calculate the PDF of the opponent's possible pockets,
        /// assuming both we and the opponent have decided to raise.
        /// Take into account new information such as community cards,
        /// and do this recursively for all branches.
        /// </summary>
        public virtual void CalculatePostRaisePDF()
        {
            // Modify the Pocket PDF assuming opponent raises.
            PreRaiseP.CopyFrom(PostRaiseP);
            RaiseUpdate();

            // Have all branches do this recursively.
            if (Branches == null) return;

            foreach (Node node in Branches)
                node.CalculatePostRaisePDF();
        }

        /// <summary>
        /// Addtional processing for calculating the post raise PDF of the opponent's pocket,
        /// taking into account that a newly introduced community card
        /// may preclude the possibility of certain pockets.
        /// </summary>
        public void CalculatePostRaisePDF_AccountForOverlaps()
        {
            // Copy the parent node's post-raise pocket PDF
            for (int i = 0; i < Pocket.N; i++)
                PostRaiseP[i] = Parent.PostRaiseP[i];

            // Pocket can't have cards that are in this flop
            double NewTotalMass = 0f;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(PostRaiseP[p])) continue;

                if (NewCollision(p))
                    PostRaiseP[p] = double.NaN;
                else
                    NewTotalMass += PostRaiseP[p];
                   
                /*if (NewCollision(p))
                {
                    NewTotalMass -= P[p];
                    P[p] = 0;
                }*/
            }
            Assert.AlmostPos(NewTotalMass);

            if (NewTotalMass <= 0) NewTotalMass = 1;

            // Normalize Pocket PDF 
            for (int i = 0; i < Pocket.N; i++)
                PostRaiseP[i] = PostRaiseP[i] / NewTotalMass;
        }

        /// <summary>
        /// Calculate the PDF of the opponent's possible pockets,
        /// assuming both we and the opponent have decided to raise.
        /// Does NOT take into account new information such as community cards,
        /// and does NOT recursively calculate for all branches.
        /// </summary>
        public void RaiseUpdate()
        {
            // Calculate chance opponent will raise, not knowing what pocket they have.
            ChanceToRaise = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p)) continue;
                ChanceToRaise += PostRaiseP[p] * S[p];
            }

            Assert.AlmostPos(ChanceToRaise);
            if (ChanceToRaise <= 0) ChanceToRaise = 1;

            // Calculate the Pocket PDF assuming opponent raised.
            for (int i = 0; i < Pocket.N; i++)
                PostRaiseP[i] = PostRaiseP[i] * S[i] / ChanceToRaise;
        }

        protected List<Node> Branches;
        protected List<Node> BranchesByIndex;
        public virtual void CreateBranches() { }

        /// <summary>
        /// Given a strategy to act or not act for each pocket,
        /// and assuming we know the PDF of the pockets, calculate
        /// the chance action is taken.
        /// </summary>
        /// <param name="pdf">The PDF of pockets.</param>
        /// <param name="chance">The chance to act for each pocket.</param>
        /// <returns>The overall chance to act.</returns>
        public double TotalChance(PocketData pdf, PocketData chance)
        {
            double RaiseChance = 0;
            for (int p = 0; p < Pocket.N; p++)
                RaiseChance += pdf[p] * chance[p];

            return RaiseChance;
        }

        /// <summary>
        /// Given a strategy to act or not act for each pocket,
        /// and assuming we know the PDF of the pockets, calculate
        /// the chance action is taken.
        /// </summary>
        /// <param name="pdf">The PDF of pockets.</param>
        /// <param name="chance">The chance to act for each pocket.</param>
        /// <param name="ExcludePocketIndex">Index of a pre-existing pocket. Exclude all pockets overlapping this pocket.</param>
        /// <returns>The overall chance to act.</returns>
        public double TotalChance(PocketData pdf, PocketData chance, int ExcludePocketIndex)
        {
            double RaiseChance = 0;
            double weight = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(pdf[p])) continue;
                if (double.IsNaN(chance[p])) continue;
                if (PocketPocketOverlap(p, ExcludePocketIndex)) continue;

                weight += pdf[p];
                RaiseChance += pdf[p] * chance[p];
            }

            if (weight == 0)
            {
                Assert.That(RaiseChance == 0);
                return 0;
            }

            return RaiseChance / weight;
        }

        /// <summary>
        /// Takes in a pocket PDF and updated it based on new information,
        /// the new information being that a particular pocket is already excluded.
        /// </summary>
        /// <param name="P">The prior PDF</param>
        /// <param name="UpdatedP">The posterior PDF</param>
        /// <param name="ExcludePocketIndex">The index of the excluded pocket.</param>
        public void UpdateOnExclusion(PocketData P, PocketData UpdatedP, int ExcludePocketIndex)
        {
            double TotalWeight = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(P[p])) { UpdatedP[p] = P[p]; continue; }

                // If this pocket overlaps with the excluded pocket,
                // then the probability of seeing it is 0.
                if (PocketPocketOverlap(p, ExcludePocketIndex))
                {
                    UpdatedP[p] = 0;
                    continue;
                }

                UpdatedP[p] = P[p];
                TotalWeight += P[p];
            }

            if (TotalWeight == 0) return;

            for (int p = 0; p < Pocket.N; p++)
                UpdatedP[p] /= TotalWeight;
        }

        protected bool PocketPocketOverlap(int p1, int p2)
        {
            return Pocket.Pockets[p1].Overlaps(Pocket.Pockets[p2]);
        }

        /// <summary>
        /// Checks if the pocket's cards overlap with any of the new community cards of this node.
        /// </summary>
        public virtual bool NewCollision(Pocket p)
        {
            return false;
        }
        public bool NewCollision(int p) { return NewCollision(Pocket.Pockets[p]); }

        /// <summary>
        /// Checks if the pocket's cards overlap with any of the node's community cards.
        /// </summary>
        public virtual bool Collision(Pocket p)
        {
            return false;
        }
        public bool Collision(int p) { return Collision(Pocket.Pockets[p]); }


        public double Simulate(int p1, int p2, params int[] BranchIndex)
        {
            return Simulate(p1, p2, ref BranchIndex, 0);
        }
    
        protected virtual double Simulate(int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            Node NextNode = BranchesByIndex[BranchIndex[IndexOffset]];
            double BranchEV = NextNode.Simulate(p1, p2, ref BranchIndex, ++IndexOffset);

            //if (this is TurnNode || this is RiverNode) BranchEV = 0;
            //if (this is RiverNode) BranchEV = 0;
            //if (this is RiverNode) BranchEV = p1 < p2 ? 1 : -1;

            double EV = 
                B[p1]       * (S[p2] * BranchEV + (1 - S[p2]) * Pot) +
                (1 - B[p1]) * (S[p2] * (-Spent) + (1 - S[p2]) * 0);

            return EV;
        }
    }
}
