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

    abstract class CommunityNode
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public static CommunityRoot Root;

		public CommunityNode Parent;

        public List<CommunityNode> Branches;
        public List<CommunityNode> BranchesByIndex;

        public number Weight;
        public BettingPhase Phase = BettingPhase.NotSet;
        public Player InitiallyActivePlayer = Player.Undefined;

        public bool[] AvailablePocket, AvailableCard;

		// Below is "scratch space", used by many nodes that reference this community node.
		// We may want to revisit this to see if we can shave away some more bytes.
		// Originally this was all in a single static spot, but that was breaking when we did thread-level parallelization.
		// If we end up doing only Flop parallelization, or only Turn parallelization, etc, we can put this in the relevant subclass.
		public PocketData[] HoldEV, HoldPocketP;
		public Optimize Data, Data2;
		public PocketData NotS;
		public number[] IntersectP;

		public const int MaxDepth = 100;
		public void MakeScratchSpace()
		{
			if (Phase == BettingPhase.PreFlop || Phase == BettingPhase.Flop)
			{
				// Initialize HoldEV and HoldPocketP, both arrays of PocketDatas
				int NumData = MaxDepth + Flop.N + Card.N + Card.N;
				HoldEV = new PocketData[NumData];
				HoldPocketP = new PocketData[NumData];

				for (int i = 0; i < NumData; i++)
				{
					HoldEV[i] = new PocketData();
					HoldPocketP[i] = new PocketData();
				}

				// Initialize rest of scratch space
				Data = new Optimize();
				Data2 = new Optimize();
				NotS = new PocketData();
				IntersectP = new number[Card.N];
			}
			else
			{
				// Copy scratch space from parent community node
				HoldEV = Parent.HoldEV;
				HoldPocketP = Parent.HoldPocketP;
				Data = Parent.Data;
				Data2 = Parent.Data2;
				NotS = Parent.NotS;
				IntersectP = Parent.IntersectP;
			}
		}

        public CommunityNode(CommunityNode Parent)
        {
			this.Parent = Parent;

#if DEBUG
            InstanceCount++;
#endif
        }

		public abstract string FileString();

        protected virtual void CreateBranches()
        {
        }

		public string FileString_Community(Flop flop)
		{
			return string.Format("{0}/", Flop.IndexOf(flop));
		}

		public string FileString_Community(Flop flop, int turn)
		{
			return string.Format("{0}/{1}/", Flop.IndexOf(flop), turn);
		}

		public string FileString_Community(Flop flop, int turn, int river)
		{
			return string.Format("{0}/{1}/{2}/", Flop.IndexOf(flop), turn, river);
		}

        protected void ClassifyAvailability()
        {
            // Classify pockets as colliding or not colliding with community cards
            AvailablePocket = new bool[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p))
                    AvailablePocket[p] = false;
                else
                    AvailablePocket[p] = true;
            }

            // Classify cards as colliding or not colliding with community cards
            AvailableCard = new bool[Card.N];
            for (int c = 0; c < Card.N; c++)
            {
                if (Contains(c))
                    AvailableCard[c] = false;
                else
                    AvailableCard[c] = true;
            }
        }

        public virtual bool NewCollision(Pocket p)
        {
            return false;
        }
        public bool NewCollision(int p) { return NewCollision(Pocket.Pockets[p]); }

        public virtual bool Collision(Pocket p)
        {
            return false;
        }
        public bool Collision(int p) { return Collision(Pocket.Pockets[p]); }

        public virtual bool Contains(int card)
        {
            return false;
        }
    }
}
