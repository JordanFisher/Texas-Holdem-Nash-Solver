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

    class PocketData
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public number[] data;
        public PocketData()
        {
            data = new number[Pocket.N];

#if DEBUG
            InstanceCount++;
#endif
        }

        public number this[int n]
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
                number temp = other[i];
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
                data[i] = ((number)1) - Source[i];
        }

        public number Average()
        {
            return data.Sum() / Pocket.N;
        }

        public virtual void Linear(int pocket, number t1, PocketData data1, number t2, PocketData data2)
        {
            this[pocket] = t1 * data1[pocket] + t2 * data2[pocket];
        }

        public virtual void Linear(int pocket, number t1, PocketData data1, number t2, PocketData data2, number t3, PocketData data3)
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

        public virtual number Hash()
        {
            number hash = 0;
            for (int i = 0; i < Pocket.N; i++)
            {
                //hash++;
                //if (number.IsNaN(data[i])) hash++;
                //if (number.IsNaN(data[i])) hash += -1f * (i + 1);
                //else hash += data[i] * (i + 1);
                hash += data[i] * (i + 1);
            }

            return hash;
        }
    }
}