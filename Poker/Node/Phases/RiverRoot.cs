using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class RiverRoot : PhaseRoot
    {
        public Flop MyFlop;
        public int MyTurn, MyRiver;

        public uint[] PocketValue;

        public RiverRoot(Node parent, int river, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            TurnRoot PreviousPhase = Parent.MyPhaseRoot as TurnRoot;
            MyFlop = PreviousPhase.MyFlop;
            MyTurn = PreviousPhase.MyTurn;
            MyRiver = river;

            Weight = 1f / (Card.N - 4 - 3 - 1);
            Phase = BettingPhase.River;
            InitiallyActivePlayer = Player.Dealer;

            PocketValue = new uint[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p))
                    PocketValue[p] = uint.MaxValue;
                else
                    PocketValue[p] = Value.Eval(MyFlop, MyTurn, MyRiver, Pocket.Pockets[p]);
            }

            Initialize();
        }

        public RiverRoot(Node parent, CommunityNode Community, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            MyCommunity = Community;
            MyFlop = ((RiverCommunity)MyCommunity).MyFlop;
            MyTurn = ((RiverCommunity)MyCommunity).MyTurn;
            MyRiver = ((RiverCommunity)MyCommunity).MyRiver;

            Weight = 1f / (Card.N - 4 - 3 - 1);
            Phase = BettingPhase.River;
            InitiallyActivePlayer = Player.Dealer;

            PocketValue = new uint[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p))
                    PocketValue[p] = uint.MaxValue;
                else
                    PocketValue[p] = Value.Eval(MyFlop, MyTurn, MyRiver, Pocket.Pockets[p]);
            }

            Initialize();
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
    }
}