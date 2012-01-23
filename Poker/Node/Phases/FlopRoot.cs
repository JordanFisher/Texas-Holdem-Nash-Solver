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

    class FlopRoot : PhaseRoot
    {
        public Flop MyFlop;

        public FlopRoot(Node Parent, CommunityNode Community, int Spent, int Pot, int RootCount)
            : base(Parent, Community, Spent, Pot, RootCount)
        {
        }

        protected override void Initialize()
        {
            MyFlop = ((FlopCommunity)MyCommunity).MyFlop;

#if SUIT_REDUCE
            FindRepresentative();

            // Only initialize if this is a representative branch
            if (IsRepresentative())
                base.Initialize();
#else
            base.Initialize();
#endif
        }

#if SUIT_REDUCE
        public FlopRoot Representative;
        public bool IsRepresentative() { return MyFlop.IsRepresentative(); }

        public void FindRepresentative()
        {
            // Find representative flop root
            Flop rep = MyFlop.Representative;

            if (IsRepresentative())
                Representative = this;
            else
                Representative = (FlopRoot)Parent.BranchesByIndex[Game.FlopLookup[rep.c1, rep.c2, rep.c3]];
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            // Only calculate if this is a representative branch
            if (IsRepresentative())
                base.CalculateBestAgainst(Opponent);
        }
#endif
    }
}