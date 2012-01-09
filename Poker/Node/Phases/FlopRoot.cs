using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class FlopRoot : PhaseRoot
    {
        public Flop MyFlop;

        public FlopRoot(Node parent, Flop flop, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            MyFlop = flop;

            Weight = 1f / Counting.Choose(Card.N - 4, 3);
            Phase = BettingPhase.Flop;
            InitiallyActivePlayer = Player.Dealer;

            Initialize();
        }

        public FlopRoot(Node parent, CommunityNode Community, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            MyCommunity = Community;
            MyFlop = ((FlopCommunity)MyCommunity).MyFlop;

            Weight = 1f / Counting.Choose(Card.N - 4, 3);
            Phase = BettingPhase.Flop;
            InitiallyActivePlayer = Player.Dealer;

            Initialize();
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Overlaps(MyFlop);
        }

        public override bool Collision(Pocket p)
        {
            return p.Overlaps(MyFlop);
        }

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card);
        }

        public override string ToString()
        {
            return MyFlop.ToString();
        }
    }
}