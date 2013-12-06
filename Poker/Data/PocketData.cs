using System;
using System.Collections.Generic;
using System.IO;
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

		public virtual void Serialize(BinaryWriter writer)
		{
			for (int i = 0; i < Pocket.N; i++)
			{
				writer.Write((float)data[i]);
			}
		}

		public virtual void Deserialize(BinaryReader reader)
		{
			for (int i = 0; i < Pocket.N; i++)
			{
				data[i] = (number)reader.ReadSingle();
			}
		}

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

        public virtual void MultiLinear(int n, int pocket, double[] weight, PocketData[] data)
        {
            number result = 0;
            for (int i = 0; i < n; i++)
                result += (number)weight[i] * data[i][pocket];

            this[pocket] = result;
        }

        public virtual void Linear(int pocket, number t1, PocketData data1, number t2, PocketData data2)
        {
			//t1 = .5 * (t1 + Node.__t);
			//t2 = .5 * (t2 + (1 - Node.__t));
            this[pocket] = t1 * data1[pocket] + t2 * data2[pocket];
        }

        public virtual void Linear(int pocket, number t1, PocketData data1, number t2, PocketData data2, number t3, PocketData data3)
        {
            this[pocket] = t1 * data1[pocket] + t2 * data2[pocket] + t3 * data3[pocket];
        }

		public virtual bool IsValid()
		{
			for (int i = 0; i < Pocket.N; i++)
			{
				if (data[i] < -Tools.eps || data[i] > 1 + Tools.eps) return false;
			}

			return true;
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