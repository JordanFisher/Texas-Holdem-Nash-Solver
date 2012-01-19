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
#if NAIVE
            /* Naive implementation. O(N^4) */
            // For each pocket we might have, calculate EV.
            PocketData UpdatedP = new PocketData();
            uint PocketValue1, PocketValue2;
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (decimal.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;
                PocketValue1 = PocketValue[p1];
                Assert.That(PocketValue1 < uint.MaxValue);

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PocketP, UpdatedP, p1);
                OpCount++;

                decimal ShowdownEV = 0;
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    //if (decimal.IsNaN(UpdatedP[p2])) continue;
                    if (!MyCommunity.AvailablePocket[p2]) continue;
                    PocketValue2 = PocketValue[p2];
                    Assert.That(PocketValue2 < uint.MaxValue);

                    if (PocketValue1 == PocketValue2) continue;
                    else if (PocketValue1 > PocketValue2)
                        ShowdownEV += UpdatedP[p2] * Pot;
                    else
                        ShowdownEV -= UpdatedP[p2] * Pot;
                }

                EV[p1] = ShowdownEV;
                Assert.IsNum(EV[p1]);
            }
#else
            /* Assymptotically optimal implementation. O(N^2) */
            RiverCommunity River = (RiverCommunity)MyCommunity;
            Optimize.ProbabilityPrecomputation(PocketP, MyCommunity);
            decimal Correction;
            int _p1;

            // For each pocket we might have, calculate the chance to win.
            Optimize.ResetSummed();

            _p1 = 0;
            while (_p1 < Pocket.N)
            {
                int p1 = River.SortedPockets[_p1];
                //if (decimal.IsNaN(PocketP[p1])) { _p1++; continue; }
                if (!MyCommunity.AvailablePocket[p1]) { _p1++; continue; }

                // Find next highest pocket
                int NextHighest = _p1 + 1;
                uint CurrentPocketValue = River.PocketValue[p1];
                while (NextHighest < Pocket.N && River.SortedPocketValue[NextHighest] == CurrentPocketValue)
                    NextHighest++;

                // For all pockets of equal value, calculate the chance to win
                for (int EqualValuedPocket = _p1; EqualValuedPocket < NextHighest; EqualValuedPocket++)
                {
                    int p = River.SortedPockets[EqualValuedPocket];
                    //if (decimal.IsNaN(PocketP[p])) continue;
                    if (!MyCommunity.AvailablePocket[p]) continue;

                    var pocket1 = Pocket.Pockets[p];
                    int c1 = pocket1.Cards[0], c2 = pocket1.Cards[1];

                    decimal ChanceToWin =
                    Optimize.SummedChance -
                        Optimize.SummedChance_OneCardFixed[c1] -
                        Optimize.SummedChance_OneCardFixed[c2];

                    Correction = Optimize.MassAfterExclusion(PocketP, p);
                    if (Correction == 0)
                        ChanceToWin = 0;
                    else
                        ChanceToWin /= Correction;

                    EV[p] = ChanceToWin * Pot;
                    Assert.IsNum(EV[p]);
                }

                // Total the probability mass of our opponent having a hand with equal value
                for (int EqualValuedPocket = _p1; EqualValuedPocket < NextHighest; EqualValuedPocket++)
                {
                    int p = River.SortedPockets[EqualValuedPocket];
                    //if (decimal.IsNaN(PocketP[p])) continue;
                    if (!MyCommunity.AvailablePocket[p]) continue;

                    var pocket1 = Pocket.Pockets[p];
                    int c1 = pocket1.Cards[0], c2 = pocket1.Cards[1]; 
                    
                    decimal P = PocketP[p];

                    Optimize.SummedChance += P;
                    Optimize.SummedChance_OneCardFixed[c1] += P;
                    Optimize.SummedChance_OneCardFixed[c2] += P;
                }

                _p1 = NextHighest;
            }

            // For each pocket we might have, calculate the chance to lose.
            Optimize.ResetSummed();

            _p1 = Pocket.N - 1;
            while (_p1 > 0)
            {
                int p1 = River.SortedPockets[_p1];
                //if (decimal.IsNaN(PocketP[p1])) { _p1--; continue; }
                if (!MyCommunity.AvailablePocket[p1]) { _p1--; continue; }

                // Find next highest pocket
                int NextLowest = _p1 - 1;
                uint CurrentPocketValue = River.PocketValue[p1];
                while (NextLowest >= 0 && River.SortedPocketValue[NextLowest] == CurrentPocketValue)
                    NextLowest--;

                // For all pockets of equal value, calculate the chance to win
                for (int EqualValuedPocket = _p1; EqualValuedPocket > NextLowest; EqualValuedPocket--)
                {
                    int p = River.SortedPockets[EqualValuedPocket];
                    //if (decimal.IsNaN(PocketP[p])) continue;
                    if (!MyCommunity.AvailablePocket[p]) continue;

                    var pocket1 = Pocket.Pockets[p];
                    int c1 = pocket1.Cards[0], c2 = pocket1.Cards[1];

                    decimal ChanceToLose =
                    Optimize.SummedChance -
                        Optimize.SummedChance_OneCardFixed[c1] -
                        Optimize.SummedChance_OneCardFixed[c2];

                    Correction = Optimize.MassAfterExclusion(PocketP, p);
                    if (Correction < Tools.eps)
                        ChanceToLose = 0;
                    else
                        ChanceToLose /= Correction;

                    EV[p] -= ChanceToLose * Pot;
                    Assert.IsNum(EV[p]);
                }

                // Total the probability mass of our opponent having a hand with equal value
                for (int EqualValuedPocket = _p1; EqualValuedPocket > NextLowest; EqualValuedPocket--)
                {
                    int p = River.SortedPockets[EqualValuedPocket];
                    //if (decimal.IsNaN(PocketP[p])) continue;
                    if (!MyCommunity.AvailablePocket[p]) continue;

                    var pocket1 = Pocket.Pockets[p];
                    int c1 = pocket1.Cards[0], c2 = pocket1.Cards[1];

                    decimal P = PocketP[p];

                    Optimize.SummedChance += P;
                    Optimize.SummedChance_OneCardFixed[c1] += P;
                    Optimize.SummedChance_OneCardFixed[c2] += P;
                }

                _p1 = NextLowest;
            }
#endif
        }

        public override decimal _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
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