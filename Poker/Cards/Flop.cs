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

    class Flop
    {
		public const bool SuitReduce = Setup.SuitReduce && Setup.Flushes;
        public static List<Flop> Flops;
        public static int N;

        public static void InitFlops()
        {
            int TotalFlops = Counting.Choose(Card.N, 3);
            Flops = new List<Flop>(TotalFlops);

			if (Flop.SuitReduce)
			{
				int S = Card.Vals;

				int Count = 0;

				for (int s = 0; s < Card.Suits; s++)
					SuitMapSum += 10 * s;

				// Same suit
				for (int v1 = 0; v1 < Card.Vals; v1++)
					for (int v2 = 0; v2 < v1; v2++)
						for (int v3 = 0; v3 < v2; v3++)
						{
							CurrentRepresentative = null;
							for (int Suit = 0; Suit < Card.Suits; Suit++)
								Flops.Add(new Flop(v1 + Suit * S, v2 + Suit * S, v3 + Suit * S));
						}
				Count += Card.Suits * Counting.Choose(Card.Vals, 3);
				Assert.That(Count == Flops.Count);

				// Two suit
				for (int v1 = 0; v1 < Card.Vals; v1++)
					for (int v2 = 0; v2 < Card.Vals; v2++)
						for (int v3 = 0; v3 < v2; v3++)
						{
							CurrentRepresentative = null;
							for (int Suit1 = 0; Suit1 < Card.Suits; Suit1++)
								for (int Suit2 = 0; Suit2 < Card.Suits; Suit2++)
									if (Suit1 != Suit2)
										Flops.Add(new Flop(v1 + Suit1 * S, v2 + Suit2 * S, v3 + Suit2 * S));
						}
				Count += Counting.Permute(Card.Suits, 2) * Card.Vals * Counting.Choose(Card.Vals, 2);
				Assert.That(Count == Flops.Count);

				// Three suit, all different values
				for (int v1 = 0; v1 < Card.Vals; v1++)
					for (int v2 = 0; v2 < v1; v2++)
						for (int v3 = 0; v3 < v2; v3++)
						{
							CurrentRepresentative = null;
							for (int Suit1 = 0; Suit1 < Card.Suits; Suit1++)
								for (int Suit2 = 0; Suit2 < Card.Suits; Suit2++)
									for (int Suit3 = 0; Suit3 < Card.Suits; Suit3++)
										if (Suit1 != Suit2 && Suit1 != Suit3 && Suit2 != Suit3)
											Flops.Add(new Flop(v1 + Suit1 * S, v2 + Suit2 * S, v3 + Suit3 * S));
						}
				Count += Counting.Permute(Card.Suits, 3) * Counting.Choose(Card.Vals, 3);
				Assert.That(Count == Flops.Count);

				// Three suits, one pair
				for (int v1 = 0; v1 < Card.Vals; v1++)
					for (int v2 = 0; v2 < Card.Vals; v2++)
					{
						if (v1 != v2)
						{
							CurrentRepresentative = null;
							for (int Suit1 = 0; Suit1 < Card.Suits; Suit1++)
								for (int Suit2 = 0; Suit2 < Card.Suits; Suit2++)
									for (int Suit3 = 0; Suit3 < Suit2; Suit3++)
										if (Suit2 != Suit1 && Suit3 != Suit1)
											Flops.Add(new Flop(v1 + Suit1 * S, v2 + Suit2 * S, v2 + Suit3 * S));
						}
					}
				Count += Card.Suits * Counting.Choose(Card.Suits - 1, 2) * Counting.Permute(Card.Vals, 2);
				Assert.That(Count == Flops.Count);

				// Three of a kind
				for (int v1 = 0; v1 < Card.Vals; v1++)
				{
					CurrentRepresentative = null;
					for (int Suit1 = 0; Suit1 < Card.Suits; Suit1++)
						for (int Suit2 = 0; Suit2 < Suit1; Suit2++)
							for (int Suit3 = 0; Suit3 < Suit2; Suit3++)
								Flops.Add(new Flop(v1 + Suit1 * S, v1 + Suit2 * S, v1 + Suit3 * S));
				}
				Count += Counting.Choose(Card.Suits, 3) * Card.Vals;
				Assert.That(Count == Flops.Count);

				Assert.That(Flops.Count == TotalFlops);
			}
			else
			{
				for (int c1 = 0; c1 < Card.N; c1++)
					for (int c2 = 0; c2 < c1; c2++)
						for (int c3 = 0; c3 < c2; c3++)
							Flops.Add(new Flop(c1, c2, c3));
			}

            N = Flops.Count;
        }

        public int c1, c2, c3;

        public Flop Representative;
        static Flop CurrentRepresentative = null;
        public bool IsRepresentative() { return this == Representative; }

        private static int[] SuitMap = new int[Card.Suits];
        private static int SuitMapSum = 0;
        
		public int[] PocketMap = new int[Pocket.N];
		public int[] CardMap = new int[Card.N];

        public int MapCard(int c, int[] map)
        {
            var card = new Card(c);
            card.Suit = map[card.Suit];

            return card.CalculateNumber();
        }

        public int MapPocket(int p, int[] map)
        {
            Pocket pocket = Pocket.Pockets[p];
            int c1 = MapCard(pocket.Cards[0], map);
            int c2 = MapCard(pocket.Cards[1], map);

            return Game.PocketLookup[c1, c2];
        }

		/// <summary>
		/// Get the index of a given flop in the Flop list.
		/// </summary>
		public static int IndexOf(Flop flop)
		{
			return Game.FlopLookup[flop.c1, flop.c2, flop.c3];
		}

		/// <summary>
		/// Given the index of a flop, get the index of that flop's representative.
		/// </summary>
		/// <param name="flop"></param>
		/// <returns></returns>
		public static int RepresentativeOf(int flop)
		{
			return IndexOf(Flops[flop].Representative);
		}

        public Flop(int c1, int c2, int c3)
        {
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;

			if (Flop.SuitReduce)
			{
				// Get the representative of this flop
				if (CurrentRepresentative == null)
					CurrentRepresentative = this;
				Representative = CurrentRepresentative;

				// Determine the mapping between this flop and its representative
				for (int s = 0; s < Card.Suits; s++)
					SuitMap[s] = -1;

				int s1 = Card.GetSuit(c1), s2 = Card.GetSuit(c2), s3 = Card.GetSuit(c3);
				int _s1 = Card.GetSuit(Representative.c1), _s2 = Card.GetSuit(Representative.c2), _s3 = Card.GetSuit(Representative.c3);
				Assert.That(SuitMap[s1] < 0 || SuitMap[s1] == _s1); SuitMap[s1] = _s1;
				Assert.That(SuitMap[s2] < 0 || SuitMap[s2] == _s2); SuitMap[s2] = _s2;
				Assert.That(SuitMap[s3] < 0 || SuitMap[s3] == _s3); SuitMap[s3] = _s3;

				int NextSuit = Math.Max(_s1, Math.Max(_s2, _s3)) + 1;
				for (int s = 0; s < Card.Suits; s++)
				{
					if (SuitMap[s] < 0)
					{
						SuitMap[s] = NextSuit++;
						Assert.That(SuitMap[s] < Card.Suits);
					}
				}
				Assert.That(SuitMap.Sum(s => 10 * s) == SuitMapSum);

				// Use the suit map to determine the card mappings and the pocket mappings
				for (int c = 0; c < Card.N; c++)
					CardMap[c] = MapCard(c, SuitMap);

				for (int p = 0; p < Pocket.N; p++)
					PocketMap[p] = MapPocket(p, SuitMap);
			}
        }

        public bool Contains(int c)
        {
            return c == c1 || c == c2 || c == c3;
        }

        public override string ToString()
        {
            return Card.ToString(Card.OutputStyle.Pretty, c1, c2, c3);
            //return Card.ToString(Card.DefaultStyle, c1, c2, c3);
        }
    }
}