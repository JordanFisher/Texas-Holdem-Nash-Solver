using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class Flop
    {
        public static List<Flop> Flops = new List<Flop>();
        public static int N;

        public static void InitFlops()
        {
            for (int c1 = 0; c1 < Card.N; c1++)
                for (int c2 = 0; c2 < c1; c2++)
                    for (int c3 = 0; c3 < c2; c3++)
                        Flops.Add(new Flop(c1, c2, c3));

            N = Flops.Count;
        }

        public int c1, c2, c3;

        public Flop(int c1, int c2, int c3)
        {
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
        }

        public bool Contains(int c)
        {
            return c == c1 || c == c2 || c == c3;
        }

        public override string ToString()
        {
            return Card.ToString(Card.DefaultStyle, c1, c2, c3);
        }
    }
}