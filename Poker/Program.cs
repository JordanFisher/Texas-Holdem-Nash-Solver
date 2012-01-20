using System;
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

                                //if (p1 > 40) Tools.Nothing();

                                number ev = root.Simulate(S1, S2, p1, p2, flop, turn, river);

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
            Assert.IsNum(EV);

            return EV / TotalMass;
        }


        static PocketRoot root;
        static Dictionary<int, bool> HashNote = new Dictionary<int, bool>(100000);
        static void Main(string[] args)
        {
            number EV; number t;
            number ev1, ev2;

            Counting.Test();

            Pocket.InitPockets();
            Game.InitPocketLookup();
            
            Flop.InitFlops();
            Game.InitFlopLookup();


            CommunityRoot.Root = new CommunityRoot();
            PocketRoot.Root = root = new PocketRoot();

            Console.WriteLine("Init done.");


            //B_Test();

            //var game = new Game(new HumanPlayer(), new HumanPlayer(), true);
            //var game = new Game(new HumanPlayer(), new StrategyPlayer(Node.VarS), true);
            //var game = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed:0);
            //number result = game.Round(2000000000);
            //Console.WriteLine("Result = {0}", result);

            //root.Process(i => 1);
            //root.BestAgainstS();



            
#if DEBUG
            Console.WriteLine("#(PocketDatas) = {0}", PocketData.InstanceCount);
            Console.WriteLine("#(BetNodes) = {0}", BetNode.InstanceCount);
            Console.WriteLine("#(ShowdownNodes) = {0}", ShowdownNode.InstanceCount);
#endif
            root.Process(i => 1);

            /*
            t = Tools.Benchmark(() => root.BestAgainstS(), 5);
            Console.WriteLine(t);
            */

            
            // Harmonic
            int col = 0;
            for (int i = 0; i < 1000000; i++)
            {
                root.BestAgainstS();
                int hash = (int)root.Hash(Node.VarB) / 10;
                if (!HashNote.ContainsKey((int)hash))
                    HashNote.Add((int)hash, true);
                else
                    col++;
                Console.WriteLine("{0,-36}, Hash = {1,-15}, S[~] = {2,-15}, #{3} {4}", PocketRoot.Result, hash, root.MyPhaseRoot.Branches[0].Branches[0].Branches[50].Branches[0].S[5], HashNote.Count, col);
                //Console.WriteLine("Average time = {0}", PocketRoot.Best_AverageTime);

                //EV = Simulation(Node.VarB, Node.VarS);
                //Console.WriteLine("Simulated EV = {0}", EV);

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
/*
            
            
            //// BiHarmonic
            //for (int i = 0; i < 1000; i++)
            //{
            //    ev1 = root.BestAgainstS();
            //    Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

            //    root.CopyTo(Node.VarS, Node.VarHold);
            //    root.CopyTo(Node.VarB, Node.VarS);
            //    ev2 = root.BestAgainstS();
            //    Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

            //    root.BiHarmonicAlg(i + 2, ev1, ev2);
            //    Console.WriteLine("Hash = {0}.", root.Hash(Node.VarS));
            //    Console.WriteLine("----------------");
            //}
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
