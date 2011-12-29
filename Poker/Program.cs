using System;


namespace Poker
{
    class Ante
    {
        public const int PreDeal = 1, PreFlop = 1, Flop = 2, Turn = 2, River = 4;
        public const int MaxPot = PreDeal + PreFlop + Flop + Turn + River;
    }

    class Program
    {
        static double Simulation()
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

                                double ev = node.Simulate(p1, p2, flop, turn, river);
                                
                                //if (p1 == 0)
                                //{
                                //    Console.WriteLine("EV(p1={0} p2={1} with [{2}]) = {3}", Pocket.Pockets[p1], Pocket.Pockets[p2], Card.CommunityToString(flop, turn, river), ev);
                                //    Console.WriteLine("  {0}", Value.GetHand(flop, turn, river, p1));
                                //    Console.WriteLine("  {0} against\n  {1}", Value.GetHand(flop, turn, river, p1).Description, Value.GetHand(flop, turn, river, p2).Description);
                                //}

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

            node = new PocketNode();
            node.Process((n, i) =>
            {
                if (n is PocketNode)// || n is FlopNode)// || n is TurnNode)
                    return i < 1 ? 1 : 0;
                else
                    return 0;
            });
            //node.Process(i => 1f);
            //node.Process(i => .35f);
            //node.Process(i => 1f);
            //node.Process(i => i == 0 ? 1 : 0);
            //node.Process(i => i == 0 ? 1 : 0.1f);


            
            node.CalculatePostRaisePDF();
            for (int i = 0; i < 1000; i++)
            {
                node.CalculatePostRaisePDF();
                node.CalculateBest();
                Console.WriteLine("Simulated EV = {0}", Simulation());
                //node.BToS();
                node.HarmonicAlg(i+2);
            }
            

            
            node.CalculatePostRaisePDF();
            Console.WriteLine("Post raise done.");

            node.CalculateBest();
            //double t = Tools.Benchmark(node.CalculateBest, 10);
            //Console.WriteLine("Average time: {0}", t);
            
            //node.CalculateBest();
            Console.WriteLine("Best done! {0} ops.", RiverNode.OpCount);
            EV = Simulation();
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
