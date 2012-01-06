using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class TurnRoot : PhaseRoot
    {
        public Flop MyFlop;
        public int MyTurn;

        public TurnRoot(Node parent, int turn, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            FlopRoot PreviousPhase = Parent.MyPhaseRoot as FlopRoot;
            MyFlop = PreviousPhase.MyFlop;
            MyTurn = turn;

            Weight = 1f / (Card.N - 4 - 3);
            Phase = BettingPhase.Turn;
            InitiallyActivePlayer = Player.Dealer;

            Initialize();
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
            return string.Format("({0}) {1}", MyFlop.ToString(), Card.ToString(Card.DefaultStyle, MyTurn));
        }
    }
}
