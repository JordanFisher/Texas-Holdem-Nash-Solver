using System;
using System.Linq;

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
    }

    class Program
    {
        static PocketRoot GameRoot;

		static void Main(string[] args)
        {
			Initialize();

			// Test the Best ev
            //B_Test();

			// Run a game
			//var pgame = new Game(new HumanPlayer(), new HumanPlayer(), true);
			//var pgame = new Game(new HumanPlayer(), new StrategyPlayer(Node.VarS), true);
			//var pgame = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed:0);
			//number result = (number)pgame.Round(2000000000);
			//Console.WriteLine("Result = {0}", result);

            // Harmonic
            for (int i = 0; i < 1000000; i++)
            {
				Console.WriteLine();

                number EV_FromBest = GameRoot.BestAgainstS();
				Console.WriteLine("          EV = {0}", EV_FromBest);

				// Monte Carlo Simulation
				//var game = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed: 0);
				//float EV_FromMonteCarlo = (float)game.Round(100000000);
				//Console.WriteLine("Monte Carlo Simulation EV = {0}", EV_FromMonteCarlo);
				
				// Full Simulation
				number EV_FromSim = Tests.Simulation(Node.VarB, Node.VarS, GameRoot);
				Console.WriteLine("Simulated EV = {0}", EV_FromSim);

                GameRoot.HarmonicAlg(i + 2);
            }

			Console.Read();
        }

		private static void Initialize()
		{
			Counting.Test();

			Pocket.InitPockets();
			Game.InitPocketLookup();

			Flop.InitFlops();
			Game.InitFlopLookup();

			Node.InitPocketData();

			CommunityRoot.Root = new CommunityRoot();
			Console.WriteLine("Lookups initialized.");

#if DEBUG
			Console.WriteLine("#(Flops) = {0}", Flop.Flops.Count);
			Console.WriteLine("#(Unique Flops) = {0}", Flop.Flops.Count(fl => fl.IsRepresentative()));
			Console.WriteLine("#(FlopCommunites) = {0}", FlopCommunity.InstanceCount);
			Console.WriteLine("#(TurnCommunity) = {0}", TurnCommunity.InstanceCount);
			Console.WriteLine("#(RiverCommunity) = {0}", RiverCommunity.InstanceCount);
			Console.WriteLine();
#endif

			PocketRoot.Root = GameRoot = new PocketRoot();
			Console.WriteLine("Data structure initialized.");

#if DEBUG
			Console.WriteLine("#(FlopRoots) = {0}", FlopRoot.InstanceCount);
			Console.WriteLine("#(PocketDatas) = {0}", PocketData.InstanceCount);
			Console.WriteLine("#(BetNodes) = {0}", BetNode.InstanceCount);
			Console.WriteLine("#(ShowdownNodes) = {0}", ShowdownNode.InstanceCount);
			Console.WriteLine();
#endif
		}
    }
}
