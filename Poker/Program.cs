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

                                double ev = node.Simulate(S1, S2, p1, p2, flop, turn, river);

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
        static void Main(string[] args)
        {
            double EV;

            Counting.Test();

            Pocket.InitPockets();
            Flop.InitFlops();

            Console.WriteLine("Init done.");

            //var FistNode = new FirstActionNode(null, true, 1, 2);


            node = new PocketNode();
            //node.Process(Node.VarS, (n, i) =>
            //{
            //    if (n is PocketNode)// || n is FlopNode)// || n is TurnNode)
            //        return i < 1 ? 1 : 0;
            //    else
            //        return 0;
            //});
            node.Process(i => 1f);
            //node.Process(i => .35f);
            //node.Process(i => 0);
            //node.Process(i => i == 0 ? 1 : 0);
            //node.Process(i => i == 0 ? 1 : 0.1f);



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
            }

            // BiHarmonic
            double ev1, ev2;
            for (int i = 0; i < 1000; i++)
            {
                ev1 = node.BestAgainstS();
                node.SToHold();
                node.BToS();
                ev2 = node.BestAgainstS();

                node.BiHarmonicAlg(i + 2, ev1, ev2);
                Console.WriteLine("----------------");
            }

            
            node.CalculatePostRaisePDF();
            Console.WriteLine("Post raise done.");

            node.CalculateBest();
            //double t = Tools.Benchmark(node.CalculateBest, 10);
            //Console.WriteLine("Average time: {0}", t);
            
            //node.CalculateBest();
            Console.WriteLine("Best done! {0} ops.", RiverNode.OpCount);
            EV = Simulation(Node.VarB, Node.VarS);
            Console.WriteLine("Simulated EV = {0}", EV);
            



            //node.Process(i => 1f);
            //node.Switch();
            //node.Process((n, i) =>
            //{
            //    if (n is PocketNode || n is FlopNode)// || n is TurnNode)
            //        return i < Pocket.N ? 1 : 0;
            //    else
            //        return 0;
            //});
            //node.Switch();

            //EV = Simulation();
            //Console.WriteLine("Simulated EV = {0}", EV);
            //node.Switch();
            //EV = Simulation();
            //Console.WriteLine("Simulated EV = {0}", EV);
            //node.Switch();
            //EV = Simulation();
            //Console.WriteLine("Simulated EV = {0}", EV);

            Console.Read();
        }
    }
}
