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
		public const int Vals = 5;
		public const int Suits = 4;

		// Betting
		public const bool SimultaneousBetting = false;
		public const int AllowedRaises = 1;

		// Classic Antes
        public const int LittleBlind = 1, BigBlind = 2, RaiseAmount = 2;

		// Simultaneous Antes
		public const int PreDeal = 1, PreFlop = 1, Flop = 2, Turn = 2, River = 4;
		
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
    }

    class Program
    {
        static PocketRoot GameRoot;

		static void Main(string[] args)
        {
			Initialize();

			if (Setup.LoadStrategy)
			{
				GameRoot.FullLoad(Setup.StrategyFile);
			}

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

            // Harmonic
			int StartIteration = Setup.LoadStrategy ? Setup.StartIteration : 0;
			for (int i = StartIteration; i < 1000000; i++)
            {
				Tools.LogPrint();

                number EV_FromBest = GameRoot.BestAgainstS();
				Tools.LogPrint("({2})          EV = {0}          ({1})", EV_FromBest, PocketRoot.Best_AverageTime, i);

				if (i % 20 == 0 && i != StartIteration)
				{
					// Save
					GameRoot.FullSave(i, EV_FromBest);

					//// Monte Carlo Simulation
					var game = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed: 0);
					float EV_FromMonteCarlo = (float)game.Round(4999999);
					Tools.LogPrint("Monte Carlo Simulation EV = {0}", EV_FromMonteCarlo);

					//// Full Simulation
					//number EV_FromSim = Tests.Simulation(Node.VarB, Node.VarS, GameRoot);
					//Tools.LogPrint("Simulated EV = {0}", EV_FromSim);
				}

				GameRoot.HarmonicAlg(i + 2);
            }

			Console.Read();
        }

		private static void Initialize()
		{
			Tools.LogPrint("Suits x Vals : {0} x {1}   = {2} card deck", Card.Suits, Card.Vals, Card.Suits * Card.Vals);

			Tools.LogPrint("Flop reduced = {0}",
				#if SUIT_REDUCE
					true);
				#else
					false);
				#endif

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
