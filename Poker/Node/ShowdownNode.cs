using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class ShowdownNode : Node
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif
        public uint[] PocketValue;

        public ShowdownNode(Node parent, int Pot)
            : base(parent, Pot, Pot)
        {
            //RiverRoot LastPhase = MyPhaseRoot as RiverRoot;
            //PocketValue = LastPhase.PocketValue;

            PocketValue = ((RiverCommunity)MyCommunity).PocketValue;

            Initialize();

#if DEBUG
            InstanceCount++;
            if (InstanceCount > 5040) Tools.Nothing();
            if (InstanceCount > 0) Tools.Nothing();
#endif
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
            // Ignore pockets that collide with community
            //for (int p = 0; p < Pocket.N; p++)
            //{
            //    if (MyPhaseRoot.Collision(p)) { PocketP[p] = EV[p] = double.NaN; continue; }
            //}

            // For each pocket we might have, calculate EV.
            PocketData UpdatedP = new PocketData();
            uint PocketValue1, PocketValue2;
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) continue;
                PocketValue1 = PocketValue[p1];
                Assert.That(PocketValue1 < uint.MaxValue);

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PocketP, UpdatedP, p1);
                OpCount++;

                double ShowdownEV = 0;
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (double.IsNaN(UpdatedP[p2])) continue;
                    PocketValue2 = PocketValue[p2];
                    Assert.That(PocketValue2 < uint.MaxValue);

                    if (PocketValue1 == PocketValue2) continue;
                    else if (PocketValue1 > PocketValue2)
                        ShowdownEV += UpdatedP[p2] * Pot;
                    else
                        ShowdownEV -= UpdatedP[p2] * Spent;
                }

                EV[p1] = ShowdownEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            uint PocketValue1 = PocketValue[p1];
            uint PocketValue2 = PocketValue[p2];

            if (PocketValue1 == PocketValue2)
                return 0;
            else if (PocketValue1 > PocketValue2)
                return Pot;
            else
                return -Pot;
        }
    }
}