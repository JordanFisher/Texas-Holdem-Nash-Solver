using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class PocketData
    {
        public float[] data;
        public PocketData()
        {
            data = new float[Pocket.N];
        }

        public float this[int n]
        {
            get { return data[n]; }
            set { data[n] = value; }
        }

        public string[] Formated
        {
            get
            {
                var d = new string[Pocket.N];
                for (int i = 0; i < Pocket.N; i++)
                    d[i] = string.Format("{0} -> {1}", Pocket.Pockets[i], data[i]);
                return d;
            }
        }
    }
}