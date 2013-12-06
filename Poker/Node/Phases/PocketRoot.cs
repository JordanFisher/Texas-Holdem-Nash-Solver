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

        public const int InitialSpent = BetNode.SimultaneousBetting ? Setup.PreDeal : Setup.LittleBlind;
        public const int InitialBet = BetNode.SimultaneousBetting ? Setup.PreDeal : Setup.BigBlind;
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

		public number PureIterate(int n)
		{
			number EV_FromBest = BestAgainstS();

			Tools.LogPrint();
			Tools.LogPrint("{2,-14} EV = {0,-20} ({1})", EV_FromBest, PocketRoot.Best_AverageTime, string.Format("({0})", n));

			Program.Iteration_Auxillary(n, EV_FromBest);

			CopyTo(VarB, VarS);

			return EV_FromBest;
		}

		public number MiniHarmonicAlg(int n, bool UseNaive = true)
		{
			number EV_FromBest = BestAgainstS();

			Tools.LogPrint();
			Tools.LogPrint("{2,-14} EV = {0,-20} ({1})", EV_FromBest, PocketRoot.Best_AverageTime, string.Format("({0})", n));

			Program.Iteration_Auxillary(n, EV_FromBest);

			Switch(VarS, VarHold);
			Switch(VarB, VarS);

			for (int iter = 0; iter < 1; iter++)
			{
				number EV = HarmonicAlg(iter, UseNaive, Output : false);
			}
			Switch(VarS, VarB);
			Switch(VarHold, VarS);

			n += 3;

			number t = ((number)1) / n;
			number s = ((number)1) - t;

			if (UseNaive)
				NaiveCombine(Node.VarS, s, Node.VarB, t, Node.VarS);
			else
				CombineStrats(s, t);

			return EV_FromBest;
		}

		public number HarmonicAlg(int n, bool UseNaive = true, bool Output = true)
		{
			number EV_FromBest = BestAgainstS();

			if (Output)
			{
				Tools.LogPrint();
				Tools.LogPrint("{2,-14} EV = {0,-20} ({1})", EV_FromBest, PocketRoot.Best_AverageTime, string.Format("({0})", n));
				Program.Iteration_Auxillary(n, EV_FromBest);
			}

			n += 3;

			number t = ((number)1) / n;
			number s = ((number)1) - t;

			//Node.__t = s;
			if (UseNaive)
				NaiveCombine(Node.VarS, s, Node.VarB, t, Node.VarS);
			else
				CombineStrats(s, t);

			return EV_FromBest;
		}

		public number B2Defense(int n)
		{
			number EV_FromBest = BestAgainstS();

			Switch(VarS,	VarB);
			Switch(VarHold, VarB);

			number EV_FromBest_2 = BestAgainstS();

			Tools.LogPrint();
			Tools.LogPrint("({3})          EV = {0},  EV2 = {1}          ({2})", EV_FromBest, EV_FromBest_2, PocketRoot.Best_AverageTime, n);

			Switch(VarHold, VarS);

			Program.Iteration_Auxillary(n, EV_FromBest);

			number t = EV_FromBest / (EV_FromBest + EV_FromBest_2);
			//t /= 10;
			//t /= (n + 100) / 100 * 10;
			t /= (n + 1);

			CombineStrats(1 - t, t);
			//NaiveCombine(Node.VarS, 1 - t, Node.VarB, t, Node.VarS);

			// Test
			//number s_vs_b = Tests.Simulation(Node.VarS, Node.VarHold, this);
			//Tools.LogPrint("Test -> {0}", s_vs_b);
			//number b2_vs_b = Tests.Simulation(Node.VarB, Node.VarHold, this);
			//Tools.LogPrint("Test -> {0}", b2_vs_b);

			return EV_FromBest;
		}
    }
}