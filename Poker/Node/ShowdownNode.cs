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

                    //if (PocketValue1 == PocketValue2) continue;
                    //else if (PocketValue1 > PocketValue2)
                    //    ShowdownEV += UpdatedP[p2] * Pot;
                    //else
                    //    ShowdownEV -= UpdatedP[p2] * Spent;

                    if (PocketValue1 == PocketValue2) continue;
                    else if (PocketValue1 > PocketValue2)
                        ShowdownEV += UpdatedP[p2] * 1;
                    //else
                    //    ShowdownEV -= UpdatedP[p2] * Spent;
                }

                EV[p1] = ShowdownEV;
                Assert.IsNum(EV[p1]);
            }


            // For each pocket we might have, calculate EV.
            //PocketData UpdatedP = new PocketData();
            //uint PocketValue1, PocketValue2;
            RiverCommunity River = (RiverCommunity)MyCommunity;
            RiverCommunity.Reset();
            int _p1 = 0, _p2;
            while (_p1 < Pocket.N)
            {
                int p1 = River.SortedPockets[_p1];
                if (double.IsNaN(PocketP[p1])) { _p1++; continue; }

                var pocket1 = Pocket.Pockets[p1];
                int c1 = pocket1.Cards[0], c2 = pocket1.Cards[1];
                double P = PocketP[p1];

                double ChanceToWin =
                RiverCommunity.SummedChance -
                    RiverCommunity.SummedChance_OneCardFixed[c1] -
                    RiverCommunity.SummedChance_OneCardFixed[c2];
                
                RiverCommunity.SummedChance += P;
                RiverCommunity.SummedChance_OneCardFixed[c1] += P;
                RiverCommunity.SummedChance_OneCardFixed[c2] += P;

                // Calculate chance to tie
                double ChanceToTie = 0;
                PocketValue1 = River.SortedPocketValue[_p1];
                for (_p2 = _p1 + 1;; _p2++)
                {
                    if (River.SortedPocketValue[_p2] != PocketValue1) break;

                    int p2 = River.SortedPockets[_p2];
                    var pocket2 = Pocket.Pockets[p2];
                    int c3 = pocket2.Cards[0], c4 = pocket2.Cards[1];
                    P = PocketP[p2];

                    if (c3 != c1 && c3 != c2 && c4 != c1 && c4 != c2)
                        ChanceToTie += P;

                    RiverCommunity.SummedChance += P;
                    RiverCommunity.SummedChance_OneCardFixed[c3] += P;
                    RiverCommunity.SummedChance_OneCardFixed[c4] += P;
                }

                //EV[p1] = ChanceToWin * Pot - (1 - ChanceToWin - ChanceToTie) * Pot;
                //double NewEV = ChanceToWin * Pot - (1 - ChanceToWin - ChanceToTie) * Pot;
                double NewEV = ChanceToWin * 1;
                Console.WriteLine("({2} -> {3}) {0} == {1}", EV[p1], NewEV, p1, PocketValue1);
                Assert.IsNum(EV[p1]);

                _p1 = _p2;
            }
            Console.WriteLine();
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