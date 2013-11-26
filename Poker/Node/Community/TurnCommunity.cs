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

    class TurnCommunity : CommunityNode
    {
#if DEBUG
		new public static int InstanceCount = 0;
#endif

        public Flop MyFlop;
        public int MyTurn;

        public TurnCommunity(FlopCommunity Parent, Flop flop, int turn)
			: base(Parent)
        {
#if DEBUG
			InstanceCount++;
#endif

            MyFlop = flop;
            MyTurn = turn;
            ClassifyAvailability();

            Weight = ((number)1) / (Card.N - 4 - 3);
            Phase = BettingPhase.Turn;
            InitiallyActivePlayer = Player.Dealer;

			MakeScratchSpace();
            CreateBranches();
        }

		public override string FileString()
		{
			return FileString_Community(MyFlop, MyTurn);
		}

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Card.N - 4);
            BranchesByIndex = new List<CommunityNode>(Card.N);

            for (int river = 0; river < Card.N; river++)
            {
                CommunityNode NewBranch;
                //if (!Contains(river))
                if (AvailableCard[river])
                {
                    NewBranch = new RiverCommunity(this, MyFlop, MyTurn, river);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Contains(MyTurn);
        }

        public override bool Collision(Pocket p)
        {
            return p.Contains(MyTurn) || p.Overlaps(MyFlop);
        }

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card) || card == MyTurn;
        }

        public override string ToString()
        {
            return Card.CommunityToString(MyFlop, MyTurn);
        }
    }
}
