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

    class RiverCommunity : CommunityNode
    {
#if DEBUG
		new public static int InstanceCount = 0;
#endif

        public Flop MyFlop;
        public int MyTurn, MyRiver;

        public uint[] PocketValue;
        public int[] SortedPockets;
        public uint[] SortedPocketValue;

        public RiverCommunity(TurnCommunity Parent, Flop flop, int turn, int river)
            : base(Parent)
        {
#if DEBUG
			InstanceCount++;
#endif

            MyFlop = flop;
            MyTurn = turn;
            MyRiver = river;
            ClassifyAvailability();

            Weight = ((number)1) / (Card.N - 4 - 3 - 1);
            Phase = BettingPhase.River;
            InitiallyActivePlayer = Player.Dealer;

            // Get all pocket values
            PocketValue = new uint[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (Collision(p))
                //    PocketValue[p] = 0;//uint.MaxValue;
                //else
                //    PocketValue[p] = Value.Eval(MyFlop, MyTurn, MyRiver, Pocket.Pockets[p]);
                if (AvailablePocket[p])
                    PocketValue[p] = Value.Eval(MyFlop, MyTurn, MyRiver, Pocket.Pockets[p]);
                else
                    PocketValue[p] = 0;//uint.MaxValue;
            }

            // Sort pockets
            SortedPockets = new int[Pocket.N];
            for (int i = 0; i < Pocket.N; i++) SortedPockets[i] = i;

            SortedPocketValue = new uint[Pocket.N];
            PocketValue.CopyTo(SortedPocketValue, 0);

            Array.Sort(SortedPocketValue, SortedPockets);

			MakeScratchSpace();
        }

		public override string FileString()
		{
			return FileString_Community(MyFlop, MyTurn, MyRiver);
		}

        public override bool NewCollision(Pocket p)
        {
            return p.Contains(MyRiver);
        }

        public override bool Collision(Pocket p)
        {
            return p.Contains(MyRiver) || p.Contains(MyTurn) || p.Overlaps(MyFlop);
        }

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card) || card == MyTurn || card == MyRiver;
        }

        public override string ToString()
        {
            return Card.CommunityToString(MyFlop, MyTurn, MyRiver);
        }
    }
}
