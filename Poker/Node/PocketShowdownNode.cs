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

    class PocketShowdownNode : Node
    {
        public PocketShowdownNode(Node parent, int Pot)
            : base(parent, Pot, Pot)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            PocketP = new PocketData();
            EV = new PocketData();

            CreateBranches();
        }

        public static int OpCount = 0;
        public override void CalculateBestAgainst(Player Opponent)
        {
            // For each pocket we might have, calculate EV.
            PocketData UpdatedP = new PocketData();
            int PocketValue1, PocketValue2;
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (number.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;
                PocketValue1 = p1;

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PocketP, UpdatedP, p1);
                OpCount++;

                number PocketShowdownEV = 0;
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    //if (number.IsNaN(UpdatedP[p2])) continue;
                    if (!MyCommunity.AvailablePocket[p2]) continue;
                    PocketValue2 = p2;

                    if (PocketValue1 == PocketValue2) continue;
                    else if (PocketValue1 > PocketValue2)
                        PocketShowdownEV += UpdatedP[p2] * Pot;
                    else
                        PocketShowdownEV -= UpdatedP[p2] * Spent;
                }

                EV[p1] = PocketShowdownEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            int PocketValue1 = p1;
            int PocketValue2 = p2;

            if (PocketValue1 == PocketValue2)
                return 0;
            else if (PocketValue1 > PocketValue2)
                return Pot;
            else
                return -Pot;
        }
    }
}