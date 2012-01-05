using System;
using System.Collections.Generic;
using System.Linq;


namespace Poker
{
    delegate PocketData Var(Node node);

    class Node
    {
        public Node Parent;
        public double Weight;

        public PhaseRoot MyPhaseRoot;

        public BettingPhase Phase = BettingPhase.NotSet;

        protected int Spent, Pot;

        public PocketData S, PreRaiseP, PostRaiseP, EV, B;
        public PocketData Hold;
        public double ChanceToRaise;

        public Node(Node parent, int Spent, int Pot)
        {
            Parent = parent;
            if (Parent != null)
                MyPhaseRoot = Parent.MyPhaseRoot;
            else
                MyPhaseRoot = null;

            this.Spent = Spent;
            this.Pot = Pot;
        }

        protected virtual void Initialize()
        {
            S = new PocketData();
            PreRaiseP = new PocketData();
            PostRaiseP = new PocketData();
            EV = new PocketData();
            B = new PocketData();

            Hold = new PocketData();

            CreateBranches();
        }

        public void ClearWorkVariables()
        {
            if (B != null) B.Reset();

            if (PreRaiseP != null) PreRaiseP.Reset();
            if (PostRaiseP != null) PostRaiseP.Reset();
            EV.Reset();

            if (Branches != null) foreach (Node node in Branches)
                node.ClearWorkVariables();
        }

        public void CopyTo(Var Source, Var Destination)
        {
            PocketData SourceData = Source(this), DestinationData = Destination(this);

            if (SourceData != null && DestinationData != null)
                DestinationData.CopyFrom(SourceData);

            if (Branches != null) foreach (Node node in Branches) node.CopyTo(Source, Destination);
        }

        public void SToHold()
        {
            Hold.CopyFrom(S);
            if (Branches != null) foreach (Node node in Branches) node.SToHold();
        }

        public void HoldToS()
        {
            S.CopyFrom(Hold);
            if (Branches != null) foreach (Node node in Branches) node.HoldToS();
        }

        public void BToS()
        {
            S.CopyFrom(B);
            if (Branches != null) foreach (Node node in Branches) node.BToS();
        }

        public void BiHarmonicAlg(int n, double ev1, double ev2)
        {
            n++;
            double t = 1f / n;
            double s = 1f - t;

            double _ev1 = n - 1, _ev2 = 1f, _ev3 = .5f * _ev2;
            //double _ev1 = 1f / ev1 + n, _ev2 = 1f / ev2, _ev3 = .5f * _ev2;

            double power = 1; //1.2f;
            _ev1 = (double)Math.Pow(_ev1, power);
            _ev2 = (double)Math.Pow(_ev2, power);
            _ev3 = (double)Math.Pow(_ev3, power);

            double total = _ev1 + _ev2 + _ev3;
            CombineStrats(_ev1 / total, _ev2 / total, _ev3 / total);
            //CombineStrats(s, t);
        }

        public void HarmonicAlg(int n)
        {
            n++;
            double t = 1f / n;
            double s = 1f - t;

            CombineStrats(s, t);
        }

        public void Switch(Var S1, Var S2)
        {
            PocketData Data1 = S1(this), Data2 = S2(this);

            if (Data1 != null && Data2 != null)
                Data1.SwitchWith(Data2);

            if (Branches != null) foreach (Node node in Branches)
                node.Switch(S1, S2);
        }

        public void Process(Action<int, Node> PocketMod)
        {
            for (int i = 0; i < Pocket.N; i++)
                PocketMod(i, this);

            if (Branches == null) foreach (Node node in Branches)
                node.Process(PocketMod);
        }

        public void Process(Func<int, double> PocketMod)
        {
            if (S != null)
            {
                for (int i = 0; i < Pocket.N; i++)
                    S[i] = PocketMod(i);
            }

            if (Branches != null) foreach (Node node in Branches)
                node.Process(PocketMod);
        }

        public void Process(Var Variable, Func<Node, int, double> PocketMod)
        {
            PocketData data = Variable(this);

            if (data != null)
            {
                for (int i = 0; i < Pocket.N; i++)
                    data[i] = Tools.Restrict(PocketMod(this, i));
            }

            if (Branches != null) foreach (Node node in Branches)
                node.Process(Variable, PocketMod);
        }

        /// <summary>
        /// Calculate the best possible strategy B against S.
        /// Should recursively calculate B for all branches as well.
        /// This is a purely virtual function. All node subclasses must override.
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

        protected virtual void UpdateChildrensPDFs()
        {
            if (Branches != null) foreach (Node branch in Branches) branch.UpdateChildrensPDFs();
        }

        public void Update(PocketData PreviousPDF, PocketData Strategy, PocketData Destination)
        {
            double ChanceToProceed = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(PreviousPDF[p]) || double.IsNaN(Strategy[p]))
                {
                    Destination[p] = double.NaN;
                    continue;
                }
                ChanceToProceed += PreviousPDF[p] * Strategy[p];
            }

            Assert.AlmostPos(ChanceToProceed);
            if (ChanceToProceed <= 0) ChanceToProceed = 1;

            // Calculate the Pocket PDF assuming opponent proceeded.
            for (int p = 0; p < Pocket.N; p++)
                Destination[p] = PreviousPDF[p] * Strategy[p] / ChanceToProceed;
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
            if (Branches != null) foreach (Node node in Branches)
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
                if (double.IsNaN(S[p]) || double.IsNaN(PostRaiseP[p]) || Collision(p)) continue;
                //if (Collision(p)) continue;
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
        public virtual bool Contains(int card) { return false; }

        public double Simulate(Var S1, Var S2, int p1, int p2, params int[] BranchIndex)
        {
            return _Simulate(S1, S2, p1, p2, ref BranchIndex, 0);
        }
    
        public virtual double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data1 = S1(this), Data2 = S2(this);

            Node NextNode = BranchesByIndex[BranchIndex[IndexOffset]];
            double BranchEV = NextNode._Simulate(S1, S2, p1, p2, ref BranchIndex, ++IndexOffset);

            double EV = 
                Data1[p1]       * (Data2[p2] * BranchEV + (1 - Data2[p2]) * Pot) +
                (1 - Data1[p1]) * (Data2[p2] * (-Spent) + (1 - Data2[p2]) * 0);
            Assert.IsNum(EV);

            return EV;
        }

        public void CombineStrats(double t1, double t2)
        {
            for (int p = 0; p < Pocket.N; p++)
                _CombineStrats(p, t1, t2);
        }
        void _CombineStrats(int p, double t1, double t2)
        {
            double _t1, _t2;

            if (S != null && B != null && !double.IsNaN(S[p]) && !double.IsNaN(B[p]))
            {
                double normalize = (t1 * S[p] + t2 * B[p]);
                _t1 = t1 * S[p] / normalize;
                _t2 = t2 * B[p] / normalize;

                S.Linear(p, t1, S, t2, B);
            }
            else
            {
                _t1 = t1; _t2 = t2;
            }

            if (Branches != null) foreach (Node node in Branches)
                    node._CombineStrats(p, _t1, _t2);
        }

        public void CombineStrats(double t1, double t2, double t3)
        {
            for (int p = 0; p < Pocket.N; p++)
                _CombineStrats(p, t1, t2, t3);
        }
        void _CombineStrats(int p, double t1, double t2, double t3)
        {
            PocketData S1 = Hold, S2 = S, S3 = B;

            double _t1, _t2, _t3;

            if (S1 != null && S2 != null && S3 != null && !double.IsNaN(S1[p]) && !double.IsNaN(S2[p]) && !double.IsNaN(S3[p]))
            {
                double normalize = (t1 * S1[p] + t2 * S2[p] + t3 * S3[p]);
                Assert.That(normalize != 0);
                _t1 = t1 * S1[p] / normalize;
                _t2 = t2 * S2[p] / normalize;
                _t3 = t3 * S3[p] / normalize;

                S.Linear(p, t1, S1, t2, S2, t3, S3);
            }
            else
            {
                _t1 = t1; _t2 = t2; _t3 = t3;
            }

            if (Branches != null) foreach (Node node in Branches)
                    node._CombineStrats(p, _t1, _t2, _t3);
        }

        void NaiveCombine(Var S1, double t1, Var S2, double t2, Var Destination)
        {
            PocketData s1 = S1(this), s2 = S2(this), destination = Destination(this);

            if (s1 != null && s2 != null && destination != null)
            {
                for (int i = 0; i < Pocket.N; i++)
                    destination[i] = t1 * s1[i] + t2 * s2[i];
            }

            if (Branches != null) foreach (Node node in Branches)
                    node.NaiveCombine(S1, t1, S2, t2, Destination);
        }

        public double Hash(Var v)
        {
            PocketData data = v(this);
            double hash;

            if (data != null && !(this is SimultaneousNode))
                Tools.Nothing();

            if (data != null)
                hash = data.Hash();
            else
                hash = 0;

            if (Branches != null) foreach (Node node in Branches)
                    hash += node.Hash(v);
            return hash;
        }

        public static Var
            VarS = n => n.S,
            VarB = n => n.B,
            VarHold = n => n.Hold;
    }
}
