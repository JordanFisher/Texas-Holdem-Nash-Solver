using System;


namespace Poker
{
    class Ante
    {
        public const int LittleBlind = 1, BigBlind = 2;

        //public const int PreDeal = 1, PreFlop = 1, Flop = 1, Turn = 1, River = 250;
        public const int PreDeal = 1, PreFlop = 1, Flop = 2, Turn = 2, River = 4;
        //public const int PreDeal = 1, PreFlop = 2, Flop = 4, Turn = 8, River = 16;
        public const int MaxPot = PreDeal + PreFlop + Flop + Turn + River;
    }

    class Program
    {
        static double Simulation(Var S1, Var S2)
        {
            double EV = 0, TotalMass = 0;

            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                double PocketEV = 0, PocketTotalMass = 0;
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

                                //double ev = node.Simulate(S1, S2, p1, p2, flop, turn, river);
                                double ev = root.Simulate(S1, S2, p1, p2, flop, turn, river);

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

        static PocketNode node;
        static PocketRoot root;
        static void Main(string[] args)
        {
            double EV; double t;
            double ev1, ev2;

            Counting.Test();

            Pocket.InitPockets();
            Flop.InitFlops();

            Console.WriteLine("Init done.");



            root = new PocketRoot();
            root.Process(i => 1f);
            //root.Process(i => Math.Abs(Math.Cos(i)));
            //root.Process(i => .5f);
            //Console.WriteLine("Hash = {0}.", root.Hash(Node.VarS));
            
            //t = Tools.Benchmark(() => root.BestAgainstS(), 5);
            //Console.WriteLine("Time = {0}.", t);
            //root.BestAgainstS();
            //Console.WriteLine("Best done! {0} ops.", ShowdownNode.OpCount);
            
            
            // Harmonic
            for (int i = 0; i < 1000; i++)
            {
                root.BestAgainstS();

                EV = Simulation(Node.VarB, Node.VarS);
                Console.WriteLine("Simulated EV = {0}", EV);

                root.Process(Node.VarHold, (n, j) => double.IsNaN(n.B[j]) ? 0 : n.B[j] + .01f);
                EV = Simulation(Node.VarHold, Node.VarS);
                Console.WriteLine("Simulated EV = {0}  (perturbed)", EV);

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
            // BiHarmonic
            for (int i = 0; i < 1000; i++)
            {
                ev1 = root.BestAgainstS();
                Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));
                root.CopyTo(Node.VarS, Node.VarHold);
                root.CopyTo(Node.VarB, Node.VarS);
                //root.SToHold();
                //root.BToS();
                ev2 = root.BestAgainstS();
                Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

                root.BiHarmonicAlg(i + 2, ev1, ev2);
                Console.WriteLine("Hash = {0}.", root.Hash(Node.VarS));
                Console.WriteLine("----------------");
            }*/
            


            node = new PocketNode();
            node.Process(i => 1f);
            //node.Process(i => Math.Abs(Math.Cos(i)));
            //node.Process(i => .5f);
            //node.Process(i => 0);
            //Console.WriteLine("Hash = {0}.", root.Hash(Node.VarS));

            //t = Tools.Benchmark(() => node.BestAgainstS(), 5);
            //Console.WriteLine("Time = {0}.", t);
            //node.BestAgainstS();
            //Console.WriteLine("Best done! {0} ops.", RiverNode.OpCount);







            // Simple loop
            //for (int i = 0; i < 1000; i++)
            //{
            //    double ev = node.BestAgainstS();
            //    node.BToS();

            //    //node.Process(Node.VarHold, (n, j) => .5f);
            //    //EV = Simulation(Node.VarS, Node.VarHold);
            //    //Console.WriteLine("Simulated EV = {0}  (idiot)", EV);

            //    //node.Process(Node.VarHold, (n, j) => 1);
            //    //EV = Simulation(Node.VarS, Node.VarHold);
            //    //Console.WriteLine("Simulated EV = {0}  (aggressive)", EV);

            //    //node.Process(Node.VarHold, (n, j) => 0);
            //    //EV = Simulation(Node.VarS, Node.VarHold);
            //    //Console.WriteLine("Simulated EV = {0}  (passive)", EV);

            //    if (ev < .1f) break;
            //}

            /*
            // Harmonic
            for (int i = 0; i < 1000; i++)
            {
                node.BestAgainstS();

                //Console.WriteLine("hash(B) = {0}", node.Hash(Node.VarB));

                EV = Simulation(Node.VarB, Node.VarS);
                Console.WriteLine("Simulated EV = {0}", EV);

                node.Process(Node.VarHold, (n, j) => double.IsNaN(n.B[j]) ? 0 : n.B[j] + .01f);
                EV = Simulation(Node.VarHold, Node.VarS);
                Console.WriteLine("Simulated EV = {0}  (perturbed)", EV);

                //node.Process(Node.VarHold, (n, j) => .5f);
                //EV = Simulation(Node.VarS, Node.VarHold);
                //Console.WriteLine("Simulated EV = {0}  (idiot)", EV);

                //node.Process(Node.VarHold, (n, j) => 1);
                //EV = Simulation(Node.VarS, Node.VarHold);
                //Console.WriteLine("Simulated EV = {0}  (aggressive)", EV);

                //node.Process(Node.VarHold, (n, j) => 0);
                //EV = Simulation(Node.VarS, Node.VarHold);
                //Console.WriteLine("Simulated EV = {0}  (passive)", EV);

                node.HarmonicAlg(i + 2);
            }*/

            // BiHarmonic
            for (int i = 0; i < 1000; i++)
            {
                ev1 = node.BestAgainstS();
                Console.WriteLine("Hash = {0}.", node.Hash(Node.VarB));

                node.CopyTo(Node.VarS, Node.VarHold);
                node.CopyTo(Node.VarB, Node.VarS);
                //node.SToHold();
                //node.BToS();
                ev2 = node.BestAgainstS();
                Console.WriteLine("Hash = {0}.", node.Hash(Node.VarB));

                node.BiHarmonicAlg(i + 2, ev1, ev2);
                Console.WriteLine("Hash = {0}.", node.Hash(Node.VarS));
                Console.WriteLine("----------------");
            }

            Console.Read();
        }
    }
}
