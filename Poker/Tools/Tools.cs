using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class Assert
    {
        public static void AlmostPos(float x)
        {
#if DEBUG
            if (x < -0.0001f || float.IsNaN(x) || float.IsInfinity(x))
                Console.WriteLine("Negative number!");
#endif
        }
        public static void AlmostOne(float x)
        {
#if DEBUG
            if (Math.Abs(x - 1) > 0.0001f || float.IsNaN(x) || float.IsInfinity(x))
                Console.WriteLine("Not equal to 1!");
#endif
        }
        public static void ZeroOrOne(float x)
        {
#if DEBUG
            if ((Math.Abs(x - 1) > 0.0001f && x > 0) || float.IsNaN(x) || float.IsInfinity(x))
                Console.WriteLine("Not 0 or 1!");
#endif
        }
        public static void IsNum(float x)
        {
#if DEBUG
            if (float.IsNaN(x) || float.IsInfinity(x))
                Console.WriteLine("Unreal number!");
#endif
        }
        public static void That(bool expression)
        {
#if DEBUG
            if (!expression)
                Console.WriteLine("Assert failed!");
#endif
        }
        public static void NotReached()
        {
#if DEBUG
            Console.WriteLine("Quarantined code reached!");
#endif
        }
    }

    class Tools
    {
        public const float eps = .0001f;
        public static bool Equals(float x, float y, float tolerance = eps)
        {
            return Math.Abs(x - y) < tolerance;
        }

        public static double Benchmark(Action action, int repititions=1)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            {
                for (int i = 0; i < repititions; i++)
                    action();
            }
            stopwatch.Stop();

            return stopwatch.Elapsed.TotalSeconds / repititions;
        }

        public static void Swap<T>(ref T t1, ref T t2)
        {
            T temp = t2;
            t2 = t1;
            t1 = temp;
        }
    }

    class Value
    {
        public static Hand GetHand(int flop, int turn, int river, int p)
        {
            return GetHand(Flop.Flops[flop], turn, river, Pocket.Pockets[p]);
        }
        public static Hand GetHand(Flop flop, int turn, int river, Pocket p)
        {
            string CommunityStr = Card.ToString(Card.OutputStyle.Text, flop.c1, flop.c2, flop.c3, turn, river);
            string PocketStr = p.ToString(Card.OutputStyle.Text);

            return new Hand(PocketStr, CommunityStr);
        }

        public static uint Eval(Flop flop, int turn, int river, Pocket p)
        {
            Hand h = GetHand(flop, turn, river, p);
            return h.HandValue;
        }

        /* Testing
        var c = new Hand5(6, 24, 36, 1, 5);
        var p1 = new Pocket(2, 20);
        var p2 = new Pocket(18, 48);
        var h1 = GetHand(c, p1);
        var h2 = GetHand(c, p2);

        Card.Print(c.Cards);
        Console.WriteLine("{0} vs {1}, {2} wins", h1, h2, h1 > h2 ? 'A' : 'B');
        Console.WriteLine("{2}\n{0}\n{1}", p1, p2, c);
        Console.WriteLine(GetHand(c, p1).Description);
        Console.WriteLine(GetHand(c, p2).Description);
        */
    }

    class Counting
    {
        public static int Choose(int n, int r)
        {
            int val = 1;
            for (int i = n - r + 1; i <= n; i++) val *= i;
            for (int i = 1; i <= r; i++) val /= i;

            return val;
        }

        public static void Test()
        {
            // Choose
            Assert.That(Choose(5,5) == 1);
            Assert.That(Choose(3,1) == 3);
            Assert.That(Choose(4,2) == 6);
        }
    }

    /* Junk
        static Hand GetHand(Hand5 Community, Pocket P)
        {
            return new Hand(P.ToString(Card.OutputStyle.Text), Community.ToString(Card.OutputStyle.Text));
        }
        static uint Eval(Hand5 Community, Pocket P)
        {
            Hand h = new Hand(P.ToString(Card.OutputStyle.Text), Community.ToString(Card.OutputStyle.Text));
            return h.HandValue;
        }

    class Hand5
    {
        public static List<Hand5> Hands = new List<Hand5>();

        public static void InitHands()
        {
            for (int c1 = 0; c1 < Card.N; c1++)
                for (int c2 = 0; c2 < c1; c2++)
                    for (int c3 = 0; c3 < c2; c3++)
                        for (int c4 = 0; c4 < c3; c4++)
                            for (int c5 = 0; c5 < c4; c5++)
                                Hand5.Hands.Add(new Hand5(c1, c2, c3, c4, c5));
        }

        public Hand5(params int[] cards)
        {
            for (int i = 0; i < 5; i++)
                this.Cards[i] = cards[i];
        }

        public int[] Cards = new int[5];

        public override string ToString()
        {
            return ToString(Card.DefaultStyle);
        }

        public string ToString(Card.OutputStyle Style)
        {
            return Card.ToString(Cards, Style);
        }
    }
     */
}
