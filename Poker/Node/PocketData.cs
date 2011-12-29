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
        public double[] data;
        public PocketData()
        {
            data = new double[Pocket.N];
        }

        public double this[int n]
        {
            get { return data[n]; }
            set { data[n] = value; }
        }

        public void CopyFrom(PocketData Source)
        {
            for (int i = 0; i < Pocket.N; i++)
                this[i] = Source[i];
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