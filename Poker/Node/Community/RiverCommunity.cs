using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class RiverCommunity : CommunityNode
    {
        public Flop MyFlop;
        public int MyTurn, MyRiver;

        public uint[] PocketValue;
        public int[] SortedPockets;
        public uint[] SortedPocketValue;

        public RiverCommunity(Flop flop, int turn, int river)
            : base()
        {
            MyFlop = flop;
            MyTurn = turn;
            MyRiver = river;
            ClassifyAvailability();

            Weight = 1f / (Card.N - 4 - 3 - 1);
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
            Tools.Nothing();
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
