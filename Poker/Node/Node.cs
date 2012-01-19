using System;
using System.Collections.Generic;
using System.Linq;


namespace Poker
{
    delegate PocketData Var(Node node);

    class Node
    {
        public double Weight;

        public PhaseRoot MyPhaseRoot;
        public CommunityNode MyCommunity;
        public Node Parent;

        public BettingPhase Phase = BettingPhase.NotSet;

        protected int Spent, Pot;

        public static bool MakeHold = true;
        public PocketData S, B, Hold;
        public PocketData PocketP, EV;

        protected List<Node> Branches;
        public List<Node> BranchesByIndex;

        public int Depth;

        public Node(Node parent, int Spent, int Pot)
        {
            Parent = parent;

            if (Parent != null)
            {
                MyPhaseRoot = Parent.MyPhaseRoot;
                MyCommunity = Parent.MyCommunity;
                Depth = Parent.Depth + 1;
            }
            else
            {
                MyPhaseRoot = null;
                MyCommunity = null;
                Depth = 0;                
            }

            this.Spent = Spent;
            this.Pot = Pot;
        }

        protected virtual void Initialize() { }

        protected virtual void CreateBranches() { }

        /*
        public void ClearWorkVariables()
        {
            if (B != null) B.Reset();

            if (PocketP != null) PocketP.Reset();
            EV.Reset();

            if (Branches != null) foreach (Node node in Branches)
                node.ClearWorkVariables();
        }*/

        public void CopyTo(Var Source, Var Destination)
        {
            PocketData SourceData = Source(this), DestinationData = Destination(this);

            if (SourceData != null && DestinationData != null)
                DestinationData.CopyFrom(SourceData);

            if (Branches != null) foreach (Node node in Branches) node.CopyTo(Source, Destination);
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
        }

        public void HarmonicAlg(int n)
        {
            n++;
            double t = 1f / n;
            double s = 1f - t;

            //CombineStrats(s, t);
            NaiveCombine(Node.VarS, s, Node.VarB, t, Node.VarS);
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
                    S[i] = Tools.Restrict(PocketMod(i));
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
        public virtual void CalculateBestAgainst(Player Opponent)
        {
            Assert.NotReached();
        }

        protected virtual void UpdateChildrensPDFs(Player Opponent)
        {
            if (Branches != null) foreach (Node branch in Branches) branch.UpdateChildrensPDFs(Opponent);
        }

        public void Update(PocketData PreviousPDF, PocketData Strategy, PocketData Destination)
        {
            double ChanceToProceed = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (double.IsNaN(PreviousPDF[p]) || double.IsNaN(Strategy[p]))
                //if (PreviousPDF[p] < 0 || Strategy[p] < 0)
                //{
                //    Destination[p] = double.NaN;
                //    continue;
                //}
                //ChanceToProceed += PreviousPDF[p] * Strategy[p];

                if (MyCommunity.AvailablePocket[p])
                    ChanceToProceed += PreviousPDF[p] * Strategy[p];
                //else
                //    Destination[p] = Tools.NaN;
            }

            Assert.AlmostPos(ChanceToProceed);
            if (ChanceToProceed <= 0) ChanceToProceed = 1;

            // Calculate the Pocket PDF assuming opponent proceeded.
            for (int p = 0; p < Pocket.N; p++)
                Destination[p] = PreviousPDF[p] * Strategy[p] / ChanceToProceed;
        }

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
                //if (double.IsNaN(pdf[p])) continue;
                //if (double.IsNaN(chance[p])) continue;

                if (!MyCommunity.AvailablePocket[p]) continue;
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
                //if (double.IsNaN(P[p])) { UpdatedP[p] = P[p]; continue; }
                if (!MyCommunity.AvailablePocket[p]) continue;

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

        /*
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
        */
        public virtual double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            //return double.NaN;
            return 0;
        }

        public void CombineStrats(double t1, double t2)
        {
            for (int p = 0; p < Pocket.N; p++)
                _CombineStrats(p, t1, t2);
        }
        void _CombineStrats(int p, double t1, double t2)
        {
            double _t1, _t2;

            //if (S != null && B != null && !double.IsNaN(S[p]) && !double.IsNaN(B[p]))
            if (S != null && B != null &&
                MyCommunity.AvailablePocket[p])
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

            //if (S1 != null && S2 != null && S3 != null && !double.IsNaN(S1[p]) && !double.IsNaN(S2[p]) && !double.IsNaN(S3[p]))
            if (S1 != null && S2 != null && S3 != null &&
                MyCommunity.AvailablePocket[p])
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
                for (int p = 0; p < Pocket.N; p++)
                    destination.Linear(p, t1, s1, t2, s2);
                    //destination[i] = t1 * s1[i] + t2 * s2[i];
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

        public void PrintOut(Var v)
        {
            for (int p = 0; p < Pocket.N; p++)
                    Console.WriteLine("{0} -> {1}", p.ToString("00"), _PrintOut(p, v));
        }
        public string _PrintOut(int p, Var v)
        {
            string s = "";

            PocketData d = v(this);
            if (d != null && this is BetNode)
                s = d.ShortFormat(p) + "  ";

            if (Branches != null) foreach (Node branch in Branches)
                s += branch._PrintOut(p, v);

            return s;
        }

        public virtual Node AdvanceHead(PlayerAction action)
        {
            return null;
        }
        public virtual Node AdvanceHead(int index)
        {
            return null;
        }
    }
}
