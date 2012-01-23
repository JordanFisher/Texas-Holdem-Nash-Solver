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

    class PocketRoot : PhaseRoot
    {
        public static PocketRoot Root;

        public const int InitialSpent = BetNode.SimultaneousBetting ? Ante.PreDeal : Ante.LittleBlind;
        public const int InitialBet = BetNode.SimultaneousBetting ? Ante.PreDeal : Ante.BigBlind;
        public PocketRoot()
            : base(null, CommunityNode.Root, InitialSpent, InitialBet, 0)
        {
        }

        public number BestAgainstS(Player Opponent)
        {
            //UpdateChildrensPDFs(Opponent);
            CalculateBestAgainst(Opponent);

            number FinalEV = EV.Average();
            return FinalEV;
        }

        public static double Best_LastTime, Best_AverageTime = 0, Best_TotalTime = 0;
        public static int Best_NumCalls = 0;
        public static string Result;
        public number BestAgainstS()
        {
            // Start timer
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Do Best calculation
            number FinalEV;

            if (BetNode.SimultaneousBetting)
            {
                FinalEV = BestAgainstS(Player.Undefined);
                Result = string.Format("EV = {0}", FinalEV);
            }
            else
            {
                number EV_AgainstButton = BestAgainstS(Player.Button);
                number EV_AgainstDealer = BestAgainstS(Player.Dealer);
                FinalEV = ((number).5) * (EV_AgainstButton + EV_AgainstDealer);
                Result = string.Format("EV = {0} : {1} -> {2}", EV_AgainstButton, EV_AgainstDealer, FinalEV);
            }

            // End timer, update statistics
            stopwatch.Stop();
            double t = stopwatch.Elapsed.TotalSeconds;
            Best_LastTime = t;
            Best_NumCalls++;
            Best_TotalTime += t;
            Best_AverageTime = Best_TotalTime / Best_NumCalls;

            // Return the EV of B vs S
            return FinalEV;
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            // Initially assume a uniform prior for which pockets opponent has.
            number UniformP = ((number)1) / Pocket.N;
            for (int i = 0; i < Pocket.N; i++)
                PocketP[i] = UniformP;

            base.UpdateChildrensPDFs(Opponent);
        }

        public number Simulate(Var S1, Var S2, int p1, int p2, params int[] BranchIndex)
        {
            return _Simulate(S1, S2, p1, p2, ref BranchIndex, 0);
        }
    }
}