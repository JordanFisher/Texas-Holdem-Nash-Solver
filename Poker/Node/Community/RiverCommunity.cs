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

        public static double SummedChance;
        public static double[] SummedChance_OneCardFixed = new double[Card.N];

        public static double TotalChance;
        public static double[] TotalChance_OneCardFixed = new double[Card.N];
        public static double TotalMass;
        public static double[] TotalMass_OneCardFixed = new double[Card.N];

        public static void ResetSummed()
        {
            SummedChance = 0;
            for (int i = 0; i < Card.N; i++)
                SummedChance_OneCardFixed[i] = 0;
        }
        public static void ResetTotal()
        {
            TotalChance = TotalMass = 0;
            for (int i = 0; i < Card.N; i++)
                TotalMass_OneCardFixed[i] =
                TotalChance_OneCardFixed[i] = 0;
        }
        public static void ResetMass()
        {
            TotalMass = 0;
            for (int i = 0; i < Card.N; i++)
                TotalMass_OneCardFixed[i] = 0;
        }

        public static void ProbabilityPrecomputation(PocketData pdf)
        {
            ResetMass();

            double mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(pdf[p])) continue;
                //if (AvailablePocket[p]) continue;

                mass = pdf[p];
                TotalMass += mass;

                pocket = Pocket.Pockets[p];
                int c1 = pocket.Cards[0], c2 = pocket.Cards[1];
                TotalMass_OneCardFixed[c1] += mass;
                TotalMass_OneCardFixed[c2] += mass;
            }
        }

        public static double MassAfterExclusion(PocketData pdf, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            double Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            return Mass;
        }

        public static void ChanceToActPrecomputation(PocketData pdf, PocketData chance)
        {
            ResetTotal();

            double val, mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(pdf[p])) continue;

                mass = pdf[p];
                val = mass * chance[p];
                TotalChance += val;
                TotalMass += mass;

                pocket = Pocket.Pockets[p];
                int c1 = pocket.Cards[0], c2 = pocket.Cards[1];
                TotalChance_OneCardFixed[c1] += val;
                TotalChance_OneCardFixed[c2] += val;
                TotalMass_OneCardFixed[c1] += mass;
                TotalMass_OneCardFixed[c2] += mass;
            }
        }

        public static double ChanceToActWithExclusion(PocketData pdf, PocketData chance, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            double Chance =
            TotalChance -
                TotalChance_OneCardFixed[c1] -
                TotalChance_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex] * chance[ExcludedPocketIndex];

            double Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            if (Mass < Tools.eps) return 0;
            else return Chance / Mass;
        }

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
