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

    class Optimize
    {
        public static number SummedChance;
        public static number[] SummedChance_OneCardFixed = new number[Card.N];

        public static number TotalChance;
        public static number[] TotalChance_OneCardFixed = new number[Card.N];
        public static number TotalMass;
        public static number[] TotalMass_OneCardFixed = new number[Card.N];

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

            number mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (number.IsNaN(pdf[p])) continue;
                if (!community.AvailablePocket[p]) continue;

                mass = pdf[p];
                TotalMass += mass;

                pocket = Pocket.Pockets[p];
                int c1 = pocket.Cards[0], c2 = pocket.Cards[1];
                TotalMass_OneCardFixed[c1] += mass;
                TotalMass_OneCardFixed[c2] += mass;
            }
        }

        public static number MassAfterExclusion(PocketData pdf, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            number Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            return Mass;
        }

        public static void ChanceToActPrecomputation(PocketData pdf, PocketData chance, CommunityNode community)
        {
            ResetTotal();

            number val, mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                //if (number.IsNaN(pdf[p])) continue;
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

        public static number ChanceToActWithExclusion(PocketData pdf, PocketData chance, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            number Chance =
            TotalChance -
                TotalChance_OneCardFixed[c1] -
                TotalChance_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex] * chance[ExcludedPocketIndex];

            number Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            if (Mass < Tools.eps) return 0;
            else return Chance / Mass;
        }
    }
}
