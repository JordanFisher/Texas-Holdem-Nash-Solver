using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class Pocket
    {
        public static List<Pocket> Pockets = new List<Pocket>();
        public static int N = Card.N * (Card.N - 1) / 2;

        public static void InitPockets()
        {
            for (int c1 = 0; c1 < Card.N; c1++)
                for (int c2 = 0; c2 < c1; c2++)
                    Pockets.Add(new Pocket(c1, c2));
            //N = Pockets.Count;

            foreach (Pocket p in Pockets)
                p.FindOverlapIndices();
        }

        public Pocket(params int[] cards)
        {
            for (int i = 0; i < 2; i++)
                this.Cards[i] = cards[i];
        }

        void FindOverlapIndices()
        {
            OverlapingPocketIndices = new int[NumOverlaps];

            int i = 0;
            for (int p2 = 0; p2 < Pocket.N; p2++)
            {
                if (Pocket.Pockets[p2].Overlaps(this))
                    OverlapingPocketIndices[i++] = p2;
            }
        }

        public int[] Cards = new int[2];

        /// <summary>
        /// The number of pockets in the pocket list this pocket overlaps with.
        /// </summary>
        public static int NumOverlaps = 2 * Card.N - 3;

        /// <summary>
        /// Indices of pockets in the pocket list this pocket overlaps with.
        /// </summary>
        public int[] OverlapingPocketIndices;

        public bool Contains(int c)
        {
            return Cards[0] == c || Cards[1] == c;
        }

        public bool Overlaps(int p)
        {
            return Overlaps(Pocket.Pockets[p]);
        }

        public bool Overlaps(Pocket p)
        {
            return p.Contains(Cards[0]) || p.Contains(Cards[1]);
        }

        public bool Overlaps(Flop flop)
        {
            return flop.Contains(Cards[0]) || flop.Contains(Cards[1]);
        }

        public override string ToString()
        {
            return ToString(Card.DefaultStyle);
        }

        public string ToString(Card.OutputStyle Style)
        {
            return Card.ToString(Cards, Style);
        }
    }
}