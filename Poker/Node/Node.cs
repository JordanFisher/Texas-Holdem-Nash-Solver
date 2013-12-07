using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Poker
{
#if SINGLE
	using number = Single;
#elif DOUBLE
	using number = Double;
#elif DECIMAL
	using number = Decimal;
#endif

    delegate PocketData Var(Node node);

    class Node
    {
		/// <summary>
		/// An identifier that encodes this node's betting history (all parent betting nodes).
		/// </summary>
		public string BetCode = null;

        public number Weight;

        public PhaseRoot MyPhaseRoot;
        public CommunityNode MyCommunity;
        public Node Parent;

        public BettingPhase Phase = BettingPhase.NotSet;

        protected int Spent, Pot;

        public static bool MakeHold = false;
        public PocketData S, B, Hold;

		//public Optimize Data  { get { return Optimize.Data; } }
		//public Optimize Data2 { get { return Optimize.Data2; } }

		public Optimize Data	{ get { return MyCommunity.Data; } }
		public Optimize Data2	{ get { return MyCommunity.Data2; } }



		//public PocketData PocketP { get { return HoldPocketP[DataOffset]; } }
		//public PocketData EV		{ get { return HoldEV[DataOffset]; } }

		// Use community scratch space (Doesn't work, trying to compress).
		//public PocketData PocketP { get { return MyCommunity.HoldPocketP[Depth]; } }
		//public PocketData EV { get { return MyCommunity.HoldEV[Depth]; } }

		// Use community scratch space (Works. Too much extra).
		public PocketData PocketP { get { return MyCommunity.HoldPocketP[DataOffset]; } }
		public PocketData EV { get { return MyCommunity.HoldEV[DataOffset]; } }

		//public PocketData PocketP { get { return _HoldPocketP; } }
		//public PocketData EV { get { return _HoldEV; } }
		//PocketData _HoldPocketP = new PocketData();
		//PocketData _HoldEV = new PocketData();

		/* If we are using one single static scratch space, we need this. Not possible if we are using thread-level parallelism.
        static PocketData[] HoldEV, HoldPocketP;
		public static void InitPocketData()
		{
			int NumData = MaxDepth + Flop.N + Card.N + Card.N;
			HoldEV = new PocketData[NumData];
			HoldPocketP = new PocketData[NumData];

			for (int i = 0; i < NumData; i++)
			{
				HoldEV[i] = new PocketData();
				HoldPocketP[i] = new PocketData();
			}
		}
		*/
		protected const int MaxDepth = 100;

		public List<Node> Branches;
		public List<Node> BranchesByIndex;

		public int Depth, DataOffset;

		/// <summary>
		/// Save the entire strategy into a single file.
		/// </summary>
		public string FullSave(int Iteration=0, float EvBest=float.NaN)
		{
			var file = FileString();
			if (file != null)
			{
				Directory.CreateDirectory(file);
				file += Tools.SimFileDescriptor(Iteration, EvBest);

				using (var fs = new FileStream(file, FileMode.Create))
				{
					using (var bw = new BinaryWriter(fs))
					{
						FullSerialize(bw);
					}
				}
			}

			return file;
		}

		/// <summary>
		/// Load the entire strategy into a single file.
		/// </summary>
		public void FullLoad(string file)
		{
			if (file != null)
			{
				using (var fs = new FileStream(file, FileMode.Open))
				{
					using (var br = new BinaryReader(fs))
					{
						FullDeserialize(br);
					}
				}
			}
		}

		/// <summary>
		/// Recursively serialize the pocket data into the single strategy save file.
		/// </summary>
		void FullSerialize(BinaryWriter bw)
		{
			if (S != null)
				S.Serialize(bw);
			
			if (Branches != null) foreach (Node node in Branches) node.FullSerialize(bw);
		}

		/// <summary>
		/// Recursively deserialize the single strategy save file into the strategy's pocket data.
		/// </summary>
		void FullDeserialize(BinaryReader br)
		{
			if (S != null)
				S.Deserialize(br);

			if (Branches != null) foreach (Node node in Branches) node.FullDeserialize(br);
		}


		/// <summary>
		/// More general strategy save function. Will save the strategy data into multiple files and directories.
		/// </summary>
		public virtual void Save()
		{
			if (S != null)
			{
				var file = FileString();
				if (file != null)
				{
					Directory.CreateDirectory(file);
					file += "data";

					using (var fs = new FileStream(file, FileMode.Create))
					{
						using (var bw = new BinaryWriter(fs))
						{
							Serialize(bw);
						}
					}
				}
			}

			if (Branches != null) foreach (Node node in Branches) node.Save();
		}

		public virtual void Load()
		{
			if (S != null)
			{
				var file = FileString();
				if (file != null)
				{
					using (var fs = new FileStream(file, FileMode.Open))
					{
						using (var br = new BinaryReader(fs))
						{
							Deserialize(br);
						}
					}
				}
			}

			if (Branches != null) foreach (Node node in Branches) node.Load();
		}

		protected virtual void Serialize(BinaryWriter bw)
		{
			S.Serialize(bw);
		}

		protected virtual void Deserialize(BinaryReader br)
		{
			S.Deserialize(br);
		}

		string FileString()
		{
			string file = Setup.SaveDir + BetCode;
			
			if (MyCommunity != null)
			{
				file += "/" + MyCommunity.FileString();
			}

			return file;
		}

		/* This code is for dynamically generating file strings at save/load time instead of always keeping the file names in memory.
		protected string FileString()
		{
			var ParentString = Parent.FileString();
			var MyAddtionalString = FileStringBit();

			if (ParentString != null)
			{
				if (MyAddtionalString != null)
					return ParentString + MyAddtionalString;
				else
					return ParentString;
			}
			else
			{
				if (MyAddtionalString != null)
					return MyAddtionalString;
				else
					return null;
			}
		}

		protected virtual string FileStringBit()
		{
			return null;
		}
		 * */

        public Node(Node parent, int Spent, int Pot)
        {
            Parent = parent;

			if (Parent == null)
				BetCode = null;
			else
				BetCode = Parent.BetCode;

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

            DataOffset = Depth;

            this.Spent = Spent;
            this.Pot = Pot;
        }

        protected virtual void Initialize() { }

        protected virtual void CreateBranches() { }

        public void MakeData(Action<Node, PocketData> Set)
        {
            if (S != null)
            {
                Set(this, new PocketData());
            }

            if (Branches != null) foreach (Node node in Branches) node.MakeData(Set);
        }

        public void CopyTo(Var Source, Var Destination)
        {
            PocketData SourceData = Source(this), DestinationData = Destination(this);

            if (SourceData != null && DestinationData != null)
                DestinationData.CopyFrom(SourceData);

            if (Branches != null) foreach (Node node in Branches) node.CopyTo(Source, Destination);
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
			if (S != null)
			{
				for (int i = 0; i < Pocket.N; i++)
					PocketMod(i, this);
			}

            if (Branches != null) foreach (Node node in Branches)
                node.Process(PocketMod);
        }

        public void Process(Func<int, number> PocketMod)
        {
            if (S != null)
            {
                for (int i = 0; i < Pocket.N; i++)
                    S[i] = Tools.Restrict(PocketMod(i));
            }

            if (Branches != null) foreach (Node node in Branches)
                node.Process(PocketMod);
        }

        public void Process(Var Variable, Func<Node, int, number> PocketMod)
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

        public void RecursiveBest(Player Opponent)
        {
            // Decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);
        }

        public void RecursiveBestPlusUpdate(Player Opponent)
        {
            // Decide strategy for children nodes.
            foreach (Node node in Branches)
            {
                node.CalculateBestAgainst(Opponent);
            }
        }

        protected virtual void UpdateChildrensPDFs(Player Opponent)
        {
        }

        public void Update(PocketData PreviousPDF, PocketData Strategy, PocketData Destination)
        {
            number ChanceToProceed = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (MyCommunity.AvailablePocket[p])
                    ChanceToProceed += PreviousPDF[p] * Strategy[p];
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
        public number TotalChance(PocketData pdf, PocketData chance)
        {
            number RaiseChance = 0;
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
        public number TotalChance(PocketData pdf, PocketData chance, int ExcludePocketIndex)
        {
            number RaiseChance = 0;
            number weight = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
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
        /// Takes in a pocket PDF and updates it based on new information,
        /// the new information being that a particular pocket should be excluded.
        /// </summary>
        /// <param name="P">The prior PDF</param>
        /// <param name="UpdatedP">The posterior PDF</param>
        /// <param name="ExcludePocketIndex">The index of the excluded pocket.</param>
        public void UpdateOnExclusion(PocketData P, PocketData UpdatedP, int ExcludePocketIndex)
        {
            number TotalWeight = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
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

        public virtual number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            return 0;
        }

		public static number __t = 0;

		/// <summary>
		/// Combines strategy S and B into a new strategy S' equivalent to randomly choosing between
		/// S and B for each hand with probabilities t1 and t2 respectively.
		/// </summary>
		/// <param name="t1">The probability that S' chooses to behave like S.</param>
		/// <param name="t2">The probability that S' chooses to behave like B.</param>
        public void CombineStrats(number t1, number t2)
        {
			if (Setup.SimultaneousBetting)
			{
				for (int p = 0; p < Pocket.N; p++)
					CombineStrats_Simultaneous(p, t1, t2);
			}
			else
			{
				for (int p = 0; p < Pocket.N; p++)
				{
					_CombineStrats(p, t1, t2, Player.Button);
					_CombineStrats(p, t1, t2, Player.Dealer);
				}
			}
        }
        void CombineStrats_Simultaneous(int p, number t1, number t2)
        {
            number _t1, _t2;

            if (S != null && B != null && MyCommunity.AvailablePocket[p])
            {
                number normalize = (t1 * S[p] + t2 * B[p]);
				
				if (normalize == 0)
				{
					_t1 = _t2 = 0;
				}
				else
				{
					_t1 = t1 * S[p] / normalize;
					_t2 = t2 * B[p] / normalize;
				}

                S.Linear(p, t1, S, t2, B);
            }
            else
            {
                _t1 = t1; _t2 = t2;
            }

            if (Branches != null) foreach (Node node in Branches)
                    node.CombineStrats_Simultaneous(p, _t1, _t2);
        }
		public virtual void _CombineStrats(int p, number t1, number t2, Player player)
		{
			if (Branches != null) foreach (Node node in Branches)
					node._CombineStrats(p, t1, t2, player);
		}

		/// <summary>
		/// Combines strategy S and B into a new strategy S' equivalent to randomly choosing between
		/// what S and B would do at each point with probabilities t1 and t2 respectively.
		/// Note that this is NOT the same as the "correct" CombineStrats above.
		/// CombineStrats and NaiveCombine are linear convex combinations in a particular space.
		/// </summary>
		/// <param name="t1">The probability that S' chooses to behave like S at a given point.</param>
		/// <param name="t2">The probability that S' chooses to behave like B at a given point.</param>
        public void NaiveCombine(Var S1, number t1, Var S2, number t2, Var Destination)
        {
            PocketData s1 = S1(this), s2 = S2(this), destination = Destination(this);

            if (s1 != null && s2 != null && destination != null)
            {
                for (int p = 0; p < Pocket.N; p++)
                    destination.Linear(p, t1, s1, t2, s2);
            }

            if (Branches != null) foreach (Node node in Branches)
                    node.NaiveCombine(S1, t1, S2, t2, Destination);
        }

        public number Hash(Var v)
        {
            PocketData data = v(this);
            number hash;

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
