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

        public void CopyFrom(PocketData Source)
        {
            for (int i = 0; i < Pocket.N; i++)
                data[i] = Source[i];
        }

        public string[] Formated
        {
            get
            {
                var d = new string[Pocket.N];
                for (int p = 0; p < Pocket.N; p++)
                    d[p] = string.Format("{0} -> {1}", Pocket.Pockets[p], data[p]);
                return d;
            }
        }

        public float FloatHash()
        {
            float hash = 0;
            for (int i = 0; i < Pocket.N; i++)
            {
                if (float.IsNaN(data[i])) hash += -1f * (i + 1);
                else hash += data[i] * (i + 1);
            }

            return hash;
        }
    }
}