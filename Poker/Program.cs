using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace Poker
{
#if SINGLE
	using number = Single;
#elif DOUBLE
	using number = Double;
#elif DECIMAL
	using number = Decimal;
#endif

    class Setup
    {
		// Cards
		public const int Vals = 3;
		public const int Suits = 4;
		public const bool Flushes = true;
		public const bool SuitReduce = false;

		// Betting
		public const bool SimultaneousBetting = false;
		public const int AllowedRaises = 1;

		// Classic Antes
        public const int LittleBlind = 1, BigBlind = 2, RaiseAmount = 2;

		// Simultaneous Antes
		public const int PreDeal = 2, PreFlop = 6, Flop = 6, Turn = 16, River = 16;
		
		// Max pot
		public const int MaxPot = SimultaneousBetting ?
			PreDeal + PreFlop + Flop + Turn + River :	// Max pot for simultaneous betting
			BigBlind + 4 * AllowedRaises * RaiseAmount;	// Max pot for classic betting

		// Directory strategy is saved to/loaded from
		public const string SaveDir = "Strategy";

		// If true we will load a saved strategy and start from there.
		public const bool	LoadStrategy = false;
		public const string StrategyFile = "Strategy/cards=4x3_ev=0.03367609_its=900___13_05_26___17_05_58.strat";
		public const int	StartIteration = 900;

		// Sim auxillary parameters
		public const int Save_Period = 1000;  // How long to wait between saves
		public const int Test_Period = 1000;  // How long to wait between tests
    }

    class Program
    {
        static PocketRoot GameRoot;

		static int StartIteration = Setup.LoadStrategy ? Setup.StartIteration : 0;

		//enum Algorithm { Harmonic_Naive, Harmonic_Correct, B2Defense };
		enum Algorithm { Harmonic_Naive, Harmonic_Correct };

		static void Main(string[] args)
        {
			Initialize();

			//GameRoot.Process(i => 1);
			//GameRoot.Process(i => Math.Pow(Math.Sin(i),2));
			//GameRoot.Process(i => (number)Math.Pow(Math.Cos(i), 2));

			if (Setup.LoadStrategy)
			{
				GameRoot.FullLoad(Setup.StrategyFile);
			}

			// Test combining
			//Tests.Combine_Test(GameRoot);
			//System.Threading.Thread.Sleep(100000000);

			// Test saving/loading
			//Tests.SaveLoad_Test(GameRoot);

			// Test the Best ev
            //Tests.B_Test(GameRoot);

			// Run a game
			////var pgame = new Game(new HumanPlayer(), new HumanPlayer(), true);
			//GameRoot.Process(i => (number)1);
			//var pgame = new Game(new HumanPlayer(), new StrategyPlayer(Node.VarS), true);
			////var pgame = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed: 0);
			//number result = (number)pgame.Round(2000000000);
			////Tools.LogPrint("Result = {0}", result);

            // Iterate
			number PrevEV = 100; double RepCount = 0; const int MinRepCount = 2;
			Algorithm alg = Algorithm.Harmonic_Correct;
			int N = 2;
			for (int iter = StartIteration; iter < 1000000; iter++)
            {
				number EV = GameRoot.HarmonicAlg(iter, UseNaive: true);

				//number EV = GameRoot.MiniHarmonicAlg(iter, UseNaive: false);
				//number EV = GameRoot.HarmonicAlg(1, UseNaive: false);
				//number EV = GameRoot.PureIterate(iter);

				/* Adaptive
				//number EV = GameRoot.HarmonicAlg(N, UseNaive: false);
				number EV = GameRoot.MiniHarmonicAlg(N, UseNaive: false);
				if (EV > PrevEV)
				{
					RepCount++;
					if (RepCount >= MinRepCount)
					{
						RepCount = 0;
						N = (int)(N * 1.75) + 1;
					}
				}
				else
				{
					RepCount = Math.Max(RepCount - .5, 0);
				}
				PrevEV = EV;
				Tools.LogPrint("{0} : {1}\n\n\n", iter, N);
				*/

				/* Adaptive + Switch
				number EV;
				switch (alg)
				{
					case Algorithm.Harmonic_Naive: EV = GameRoot.HarmonicAlg(N, true); break;
					case Algorithm.Harmonic_Correct: EV = GameRoot.HarmonicAlg(N, false); break;
					//case Algorithm.B2Defense: EV = GameRoot.B2Defense(iter); break;
					default: continue;
				}
				//number EV = GameRoot.HarmonicAlg(N, false);
				if (EV > PrevEV)
				{
					RepCount++;
					if (RepCount >= MinRepCount)
					{
						RepCount = 0;
						Tools.Incr(ref alg);
						N = (int)(N * 1.5) + 1;
					}
				}
				else
				{
					RepCount = Math.Max(RepCount - .25, 0);
				}
				PrevEV = EV;
				Tools.LogPrint("{0} : {1}", iter, alg);
				 */
            }

			Console.Read();
        }

		public static void Iteration_Auxillary(int i, number EV_FromBest)
		{
			Iteration_Save(i, EV_FromBest);
			Iteration_Test(i, EV_FromBest);
		}

		private static void Iteration_Save(int i, number EV_FromBest)
		{
			if (i % Setup.Save_Period == 0 && i != StartIteration)
			{
				// Save
				GameRoot.FullSave(i, (float)EV_FromBest);
			}
		}

		private static void Iteration_Test(int i, number EV_FromBest)
		{
			if (i % Setup.Test_Period == 0 && i != StartIteration)
			{
				// Monte Carlo Simulation
				var game = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed: 0);
				float EV_FromMonteCarlo = (float)game.Round(4999999);
				Tools.LogPrint("Monte Carlo Simulation EV = {0}", EV_FromMonteCarlo);

				//// Full Simulation
				//number EV_FromSim = Tests.Simulation(Node.VarB, Node.VarS, GameRoot);
				//Tools.LogPrint("Simulated EV = {0}", EV_FromSim);
			}
		}


		private static void Initialize()
		{
			Tools.LogPrint("Suits x Vals : {0} x {1}   = {2} card deck", Card.Suits, Card.Vals, Card.Suits * Card.Vals);

			Tools.LogPrint("Flop reduced = {0}", Flop.SuitReduce);

			Tools.LogPrint("Precision = {0}",
				#if SINGLE
					"single");
				#elif DOUBLE
					"double");
				#else
					"decimal");
				#endif

			Tools.LogPrint();

			Counting.Test();

			Pocket.InitPockets();
			Game.InitPocketLookup();

			Flop.InitFlops();
			Game.InitFlopLookup();

			//Node.InitPocketData();

			CommunityRoot.Root = new CommunityRoot();
			Tools.LogPrint("Lookups initialized.");

#if DEBUG
			Tools.LogPrint("#(Flops) = {0}", Flop.Flops.Count);
			Tools.LogPrint("#(Unique Flops) = {0}", Flop.Flops.Count(fl => fl.IsRepresentative()));
			Tools.LogPrint("#(FlopCommunites) = {0}", FlopCommunity.InstanceCount);
			Tools.LogPrint("#(TurnCommunity) = {0}", TurnCommunity.InstanceCount);
			Tools.LogPrint("#(RiverCommunity) = {0}", RiverCommunity.InstanceCount);
			Tools.LogPrint();
#endif

			PocketRoot.Root = GameRoot = new PocketRoot();
			Tools.LogPrint("Data structure initialized.");

#if DEBUG
			Tools.LogPrint("#(FlopRoots) = {0}", FlopRoot.InstanceCount);
			Tools.LogPrint("#(PocketDatas) = {0}", PocketData.InstanceCount);
			Tools.LogPrint("#(BetNodes) = {0}", BetNode.InstanceCount);
			Tools.LogPrint("#(ShowdownNodes) = {0}", ShowdownNode.InstanceCount);
			Tools.LogPrint();
#endif
		}
    }
}
