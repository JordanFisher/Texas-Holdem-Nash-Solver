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

    class Assert
    {
        public static void AlmostPos(number x)
        {
#if DEBUG
            if (x < -Tools.eps || Tools.IsNaN(x))
                Tools.Raise("Negative number!");
#endif
        }
        public static void AlmostOne(number x)
        {
#if DEBUG
            if (Math.Abs(x - 1) > Tools.eps || Tools.IsNaN(x))
                Tools.Raise("Not equal to 1!");
#endif
        }
        public static void ZeroOrOne(number x)
        {
#if DEBUG
            if ((Math.Abs(x - 1) > Tools.eps && x > 0) || Tools.IsNaN(x))
                Tools.Raise("Not 0 or 1!");
#endif
        }
        public static void IsProbability(number x)
        {
#if DEBUG
            if (Tools.IsNaN(x) || x < 0 || x > 1)
                Tools.Raise("Not a probability!");
#endif
        }
        public static void AlmostProbability(number x)
        {
#if DEBUG
            if (Tools.IsNaN(x) || x < -Tools.eps || x > 1 + Tools.eps)
                Tools.Raise("Not a probability!");
#endif
        }
        public static void AlmostEqual(number x, number y)
        {
#if DEBUG
            if (!Tools.Equals(x, y))
                Tools.Raise("Not equal!");
#endif
        }
        public static void IsNum(number x)
        {
#if DEBUG
            if (Tools.IsNaN(x))
                Tools.Raise("Unreal number!");
#endif
        }
        public static void That(bool expression)
        {
#if DEBUG
            if (!expression)
                Tools.Raise("Assert failed!");
#endif
        }
        public static void NotReached()
        {
#if DEBUG
            Tools.Raise("Quarantined code reached!");
#endif
        }
    }

    class Tools
    {
        public static void Raise(string message)
        {
#if DEBUG
            Console.WriteLine(message);
#endif
        }

        public const number Big = 1000000;
        public const number NaN = -Big;

        public static bool IsNaN(number x)
        {
            return x <= -Big || x >= Big;
        }

        public const number eps = (number).00001;
        public static bool Equals(number x, number y, number tolerance = eps)
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

        /// <summary>
        /// Returns the restriction of a number between 0 and 1.
        /// </summary>
        public static number Restrict(number p)
        {
            if (p > 1) return 1;
            else if (p < 0) return 0;
            else return p;
        }

        public static Random Rnd = new Random();
        public static int RandomCard()
        {
            return Rnd.Next(Card.N);
        }

        public static int RandomCard(params int[] ExistingCards)
        {
            return RandomCard(ExistingCards, ExistingCards.Length);
        }
        public static int RandomCard(int[] ExistingCards, int Length)
        {
            int NewCard = Rnd.Next(Card.N);
            while (ExistingCards.Contains(NewCard))
                NewCard = Rnd.Next(Card.N);
            return NewCard;
        }

        public static void RandomCards(int[] cards)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                new ArraySegment<int>(cards, 0, i);
                cards[i] = RandomCard(cards, i);
            }
        }

        public static int RandomPocket()
        {
            return Rnd.Next(Pocket.N);
        }

        public static int RandomPocket(int ExistingPockets)
        {
            int NewPocket = Rnd.Next(Pocket.N);
            while (Pocket.Pockets[NewPocket].Overlaps(Pocket.Pockets[ExistingPockets]))
                NewPocket = Rnd.Next(Pocket.N);
            return NewPocket;
        }

        public static string PlayerName(Player player)
        {
            switch (player)
            {
                case Player.Button: return "Button";
                case Player.Dealer: return "Dealer";
                default:            return "------";
            }
        }
        public static Player NextPlayer(Player CurrentPlayer)
        {
            switch (CurrentPlayer)
            {
                case Player.Button: return Player.Dealer;
                case Player.Dealer: return Player.Button;
                default: return Player.Undefined;
            }
        }

        public static void Nothing() { }
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
        public static Hand GetHand(int c1, int c2, int c3, int c4, int c5, int c6, int c7)
        {
            string CommunityStr = Card.ToString(Card.OutputStyle.Text, c1, c2, c3, c4, c5);
            string PocketStr = Card.ToString(Card.OutputStyle.Text, c6, c7);

            return new Hand(PocketStr, CommunityStr);
        }

        public static uint Eval(Flop flop, int turn, int river, Pocket p)
        {
            Hand h = GetHand(flop, turn, river, p);
            return h.HandValue;
        }
        public static uint Eval(int c1, int c2, int c3, int c4, int c5, int c6, int c7)
        {
            Hand h = GetHand(c1, c2, c3, c4, c5, c6, c7);
            return h.HandValue;
        }
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

        public static int Permute(int n, int r)
        {
            int val = 1;
            for (int i = n - r + 1; i <= n; i++) val *= i;

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
}
