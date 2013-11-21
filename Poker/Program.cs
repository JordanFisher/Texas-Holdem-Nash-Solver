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

    class Ante
    {
        public const int LittleBlind = 1, BigBlind = 2, RaiseAmount = 2;

        //public const int PreDeal = 1, PreFlop = 1, Flop = 1, Turn = 1, River = 250;
        public const int PreDeal = 1, PreFlop = 1, Flop = 2, Turn = 2, River = 4;
        //public const int PreDeal = 1, PreFlop = 2, Flop = 4, Turn = 8, River = 16;
        public const int MaxPot = PreDeal + PreFlop + Flop + Turn + River;
    }

    class Program
    {
        static number Simulation(Var S1, Var S2)
        {
            if (BetNode.SimultaneousBetting)
                return _Simulation(S1, S2);
            else
            {
                number EV1 = _Simulation(S1, S2);
                number EV2 = -_Simulation(S2, S1);
                number TotalEV = ((number).5) * (EV1 + EV2);
                Console.WriteLine("EV = {0} : {1} -> {2}", EV1, EV2, TotalEV);
                return TotalEV;
            }
        }

        static number _Simulation(Var S1, Var S2)
        {
            number EV = 0, TotalMass = 0;

            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                number PocketEV = 0, PocketTotalMass = 0;
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (Pocket.Pockets[p1].Overlaps(Pocket.Pockets[p2])) continue;

                    for (int flop = 0; flop < Flop.N; flop++)
                    {
                        if (Pocket.Pockets[p1].Overlaps(Flop.Flops[flop])) continue;
                        if (Pocket.Pockets[p2].Overlaps(Flop.Flops[flop])) continue;

                        for (int turn = 0; turn < Card.N; turn++)
                        {
                            if (Flop.Flops[flop].Contains(turn)) continue;
                            if (Pocket.Pockets[p1].Contains(turn)) continue;
                            if (Pocket.Pockets[p2].Contains(turn)) continue;

                            for (int river = 0; river < Card.N; river++)
                            {
                                if (turn == river) continue;
                                if (Flop.Flops[flop].Contains(river)) continue;
                                if (Pocket.Pockets[p1].Contains(river)) continue;
                                if (Pocket.Pockets[p2].Contains(river)) continue;

#if SUIT_REDUCE
								// Map all of our cards (community and pockets) using the flop map
								// (because we are using a suit reduced data structure).
								var f = Flop.Flops[flop];
								int _flop = Flop.IndexOf(f.Representative);
								int _turn = f.CardMap[turn];
								int _river = f.CardMap[river];

								int _p1 = f.PocketMap[p1];
								int _p2 = f.PocketMap[p2];
                                
								number ev = root.Simulate(S1, S2, _p1, _p2, _flop, _turn, _river);
#else
								number ev = root.Simulate(S1, S2, p1, p2, flop, turn, river);
#endif

                                TotalMass += 1;
                                EV += ev;

                                PocketTotalMass += 1;
                                PocketEV += ev;
                            }
                        }
                    }
                }

                //Console.WriteLine("  EV(p1 = {0}) = {1}", p1, PocketEV / PocketTotalMass);
                //Console.WriteLine("  EV(p1 = {0}) = {1}", Pocket.Pockets[p1], PocketEV / PocketTotalMass);
            }
            Assert.IsNum(EV / TotalMass);

            return EV / TotalMass;
        }



        static double[,] Payoffs = new double[100, 100];
        static int Columns = 0;
        static void AddColumn()
        {
            root.CopyToList(Columns);

            Columns++;

            for (int i = 0; i < Columns; i++)
            {
                double payoff = Simulation(n => n.PureList[i], n => n.PureList[Columns - 1]);
                Payoffs[i, Columns - 1] = -payoff;
                Payoffs[Columns - 1, i] = payoff;
            }
        }

        static string MatrixString()
        {
            string output = "";

            for (int i = 0; i < Columns; i++)
            {
                for (int j = 0; j < Columns; j++)
                    output += string.Format("{0,8:F3}", Payoffs[i, j]);
                output += "\n";
            }

            return output;
        }


        static PocketRoot root;
        static Dictionary<int, bool> HashNote = new Dictionary<int, bool>(100000);
        static void Main(string[] args)
        {
            //var Payoffs = new int[][] {
            //                new int[] {  0,  1, -1 },
            //                new int[] { -1,  0,  1 },
            //                new int[] {  1, -1,  0 } };

            //double[] PartialNash = LpSolver.FindNash(Payoffs, 3);
            //for (int i = 0; i < 3; i++)
            //    Console.WriteLine("{0} -> {1}", i, PartialNash[i]);
            //Console.ReadLine();




            number EV; number t;
            number ev1, ev2;

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

			PocketRoot.Root = root = new PocketRoot();
			Console.WriteLine("Data structure initialized.");

#if DEBUG
			Console.WriteLine("#(FlopRoots) = {0}", FlopRoot.InstanceCount);
			Console.WriteLine("#(PocketDatas) = {0}", PocketData.InstanceCount);
			Console.WriteLine("#(BetNodes) = {0}", BetNode.InstanceCount);
			Console.WriteLine("#(ShowdownNodes) = {0}", ShowdownNode.InstanceCount);
			Console.WriteLine();
#endif


            //B_Test();

			////var pgame = new Game(new HumanPlayer(), new HumanPlayer(), true);
			//var pgame = new Game(new HumanPlayer(), new StrategyPlayer(Node.VarS), true);
			////var pgame = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed:0);
			//number result = (number)pgame.Round(2000000000);
			////Console.WriteLine("Result = {0}", result);


           
            //root.Process(i => 1);
            //root.CopyToList(0);
            //root.Process(i => .5f);
            //root.CopyToList(1);
            //root.MultiCombine(2, new double[] { .5f, .5f });
            
            //root.Process(Node.VarB, (n, i) => .25f);
            //Console.WriteLine(Simulation(Node.VarB, Node.Pure(0)));
            //Console.WriteLine(Simulation(Node.VarB, Node.Pure(1)));
            //Console.WriteLine(Simulation(Node.VarB, Node.VarS));
            //Console.Read();

            
            /* LP-Loop
            AddColumn();
            root.BestAgainstS(); root.Switch(Node.VarS, Node.VarB); AddColumn();

            for (int i = 0; i < 50; i++)
            {
                Console.WriteLine();
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine();

                root.BestAgainstS(); root.Switch(Node.VarS, Node.VarB); AddColumn();
                Console.WriteLine("New pure strat {1} -> {0,-36}", PocketRoot.Result, i);
                root.BestAgainstS();
                Console.WriteLine("   (Best EV against new pure strat is {0})", PocketRoot.Result);

                //Console.WriteLine();
                //Console.WriteLine(MatrixString());
                //Console.WriteLine();

                double[] PartialNash = LpSolver.FindNash(Payoffs, Columns);
                //for (int j = 0; j < Columns; j++)
                //    Console.WriteLine("{0} -> {1}", j, PartialNash[j]);

                //root.MultiCombine_Naive(Columns, PartialNash);
                root.MultiCombine(Columns, PartialNash);
            }
			*/


            
            /* Harmonic */
            for (int i = 0; i < 1000000; i++)
            {
				Console.WriteLine();

                number EV_FromBest = root.BestAgainstS();
				Console.WriteLine("          EV = {0}", EV_FromBest);

				//int hash = (int)root.Hash(Node.VarB) / 10;
				//if (!HashNote.ContainsKey((int)hash))
				//    HashNote.Add((int)hash, true);
				//else
				//    col++;
                //Console.WriteLine("{0,-36}, Hash = {1,-15}, S[~] = {2,-15}, #{3} {4}", PocketRoot.Result, hash, root.MyPhaseRoot.Branches[0].Branches[0].Branches[50].Branches[0].S[5], HashNote.Count, col);
				//Console.WriteLine("{0,-36}, Hash = {1,-15}", PocketRoot.Result, hash);
                //Console.WriteLine("Average time = {0}", PocketRoot.Best_AverageTime);

				// Monte Carlo Simulation
				var game = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed: 0);
				float EV_FromMonteCarlo = (float)game.Round(100000000);
				Console.WriteLine("Monte Carlo Simulation EV = {0}", EV_FromMonteCarlo);
				
				// Full Simulation
				number EV_FromSim = Simulation(Node.VarB, Node.VarS);
				Console.WriteLine("Simulated EV = {0}", EV_FromSim);

                //root.Process(Node.VarHold, (n, j) => number.IsNaN(n.B[j]) ? 0 : n.B[j] + .01f);
                //EV = Simulation(Node.VarHold, Node.VarS);
                //Console.WriteLine("Simulated EV = {0}  (perturbed)", EV);

                //root.Process(root.VarHold, (n, j) => .5f);
                //EV = Simulation(root.VarS, root.VarHold);
                //Console.WriteLine("Simulated EV = {0}  (idiot)", EV);

                //root.Process(root.VarHold, (n, j) => 1);
                //EV = Simulation(root.VarS, root.VarHold);
                //Console.WriteLine("Simulated EV = {0}  (aggressive)", EV);

                //root.Process(root.VarHold, (n, j) => 0);
                //EV = Simulation(root.VarS, root.VarHold);
                //Console.WriteLine("Simulated EV = {0}  (passive)", EV);

                root.HarmonicAlg(i + 2);
            }



			/* BiHarmonic
			for (int i = 0; i < 1000; i++)
			{
			    ev1 = root.BestAgainstS();
			    Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

			    root.CopyTo(Node.VarS, Node.VarHold);
			    root.CopyTo(Node.VarB, Node.VarS);
			    ev2 = root.BestAgainstS();
			    Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

			    root.BiHarmonicAlg(i + 2, ev1, ev2);
			    Console.WriteLine("Hash = {0}.", root.Hash(Node.VarS));
			    Console.WriteLine("----------------");
			}
			*/
			Console.Read();
        }

        /// <summary>
        /// Given S and B = B(S), verify that B is the best strategy against S.
        /// Procedure: Verify EV(~B, S) < EV(B, S) for all other strategies ~B.
        /// </summary>
        private static void B_Test()
        {
            number EV;
            Assert.That(Node.MakeHold);

            // Strategy S
            //root.Process(i => .5);
            root.Process(i => (number)Math.Cos(i));
            //root.Process(Node.VarB, (n, i) => Math.Sin(i));

            // Strategy B = Hold = B(S). Must have Hold data initialized.
            EV = root.BestAgainstS();
            root.CopyTo(Node.VarB, Node.VarHold);

            // Calculate EV(~B, S) for many ~B
            number Min = number.MaxValue;
            for (int k = 0; k < 10000; k++)
            {
                root.CopyTo(Node.VarHold, Node.VarB);
                root.Process(Node.VarB, (n, i) => n.B[i] + (number)((Tools.Rnd.NextDouble() - .5f) * .01f));
                number ModdedEV = Simulation(Node.VarB, Node.VarS);
                Min = Math.Min(Min, EV - ModdedEV);
                Console.WriteLine("Difference = {0}, min = {1}", EV - ModdedEV, Min);
            }
        }
    }
}
