using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class Optimize
    {
        public static decimal SummedChance;
        public static decimal[] SummedChance_OneCardFixed = new decimal[Card.N];

        public static decimal TotalChance;
        public static decimal[] TotalChance_OneCardFixed = new decimal[Card.N];
        public static decimal TotalMass;
        public static decimal[] TotalMass_OneCardFixed = new decimal[Card.N];

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

        public static void ProbabilityPrecomputation(PocketData pdf, CommunityNode community)
        {
            ResetMass();

            decimal mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (decimal.IsNaN(pdf[p])) continue;
                if (!community.AvailablePocket[p]) continue;

                mass = pdf[p];
                TotalMass += mass;

                pocket = Pocket.Pockets[p];
                int c1 = pocket.Cards[0], c2 = pocket.Cards[1];
                TotalMass_OneCardFixed[c1] += mass;
                TotalMass_OneCardFixed[c2] += mass;
            }
        }

        public static decimal MassAfterExclusion(PocketData pdf, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            decimal Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            return Mass;
        }

        public static void ChanceToActPrecomputation(PocketData pdf, PocketData chance, CommunityNode community)
        {
            ResetTotal();

            decimal val, mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (decimal.IsNaN(pdf[p])) continue;
                if (!community.AvailablePocket[p]) continue;

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

        public static decimal ChanceToActWithExclusion(PocketData pdf, PocketData chance, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            decimal Chance =
            TotalChance -
                TotalChance_OneCardFixed[c1] -
                TotalChance_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex] * chance[ExcludedPocketIndex];

            decimal Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            if (Mass < Tools.eps) return 0;
            else return Chance / Mass;
        }
    }
}
