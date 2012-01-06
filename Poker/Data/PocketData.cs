﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class PocketData
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public double[] data;
        public PocketData()
        {
            data = new double[Pocket.N];

#if DEBUG
            InstanceCount++;
#endif
        }

        public double this[int n]
        {
            get { return data[n]; }
            set { data[n] = value; }
        }

        public virtual void Reset()
        {
            for (int i = 0; i < Pocket.N; i++)
                data[i] = 0;
        }

        public virtual void SwitchWith(PocketData other)
        {
            for (int i = 0; i < Pocket.N; i++)
            {
                double temp = other[i];
                other[i] = data[i];
                data[i] = temp;
            }
        }

        public virtual void CopyFrom(PocketData Source)
        {
            for (int i = 0; i < Pocket.N; i++)
                data[i] = Source[i];
        }

        public virtual void InverseOf(PocketData Source)
        {
            for (int i = 0; i < Pocket.N; i++)
                data[i] = 1f - Source[i];
        }

        public double Average()
        {
            return data.Sum() / Pocket.N;
        }

        public virtual void Linear(int pocket, double t1, PocketData data1, double t2, PocketData data2)
        {
            this[pocket] = t1 * data1[pocket] + t2 * data2[pocket];
        }

        public virtual void Linear(int pocket, double t1, PocketData data1, double t2, PocketData data2, double t3, PocketData data3)
        {
            this[pocket] = t1 * data1[pocket] + t2 * data2[pocket] + t3 * data3[pocket];
        }

        public virtual string[] Formated
        {
            get
            {
                var d = new string[Pocket.N];
                for (int p = 0; p < Pocket.N; p++)
                    d[p] = string.Format("{0} -> {1}", Pocket.Pockets[p], data[p]);
                return d;
            }
        }

        public virtual string ShortFormat(int p)
        {
            return data[p].ToString("0.0");
        }

        public virtual double Hash()
        {
            double hash = 0;
            for (int i = 0; i < Pocket.N; i++)
            {
                //hash++;
                //if (double.IsNaN(data[i])) hash++;
                if (double.IsNaN(data[i])) hash += -1f * (i + 1);
                else hash += data[i] * (i + 1);
            }

            return hash;
        }
    }
}