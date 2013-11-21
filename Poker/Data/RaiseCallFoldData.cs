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

    class RaiseCallFoldData : PocketData
    {
		public override void Serialize(System.IO.BinaryWriter writer)
		{
			Raise.Serialize(writer);
			Call.Serialize(writer);
		}

		public override void Deserialize(System.IO.BinaryReader reader)
		{
			Raise.Deserialize(reader);
			Call.Deserialize(reader);
		}

        public PocketData Raise, Call;
        public RaiseCallFoldData()
        {
            this.Raise = this;
            this.Call = new PocketData();
        }

        public number Fold(int n) { return ((number)1) - Raise[n] - Call[n]; }

        public void Set(int pocket, number r, number c)
        {
            Assert.IsProbability(r);
            Assert.IsProbability(c);
            Assert.IsProbability(r + c);

            Raise[pocket] = r;
            Call[pocket] = c;
        }

        public bool IsValid()
        {
            for (int i = 0; i < Pocket.N; i++)
            {
                if (Raise[i] < -Tools.eps || Raise[i] > 1 + Tools.eps) return false;
                if (Call[i] < -Tools.eps || Call[i] > 1 + Tools.eps) return false;
                if (Raise[i] + Call[i] > 1 + Tools.eps) return false;
            }

            return true;
        }

        public override void Reset()
        {
            for (int i = 0; i < Pocket.N; i++)
            {
                Raise[i] = 0;
                Call[i] = 0;
            }
        }

        public override void CopyFrom(PocketData Source)
        {
            RaiseCallFoldData _Source = Source as RaiseCallFoldData;

            for (int i = 0; i < Pocket.N; i++)
            {
                Raise[i] = _Source.Raise[i];
                Call[i]  = _Source.Call[i];
            }
        }

        public override void SwitchWith(PocketData other)
        {
            RaiseCallFoldData _other = other as RaiseCallFoldData;
            Assert.That(_other != null);

            base.SwitchWith(other);
            Call.SwitchWith(_other.Call);
        }

        public override void Linear(int pocket, number t1, PocketData data1, number t2, PocketData data2)
        {
            base.Linear(pocket, t1, data1, t2, data2);
            Call.Linear(pocket, t1, ((RaiseCallFoldData)data1).Call, t2, ((RaiseCallFoldData)data2).Call);
        }

        public override void Linear(int pocket, number t1, PocketData data1, number t2, PocketData data2, number t3, PocketData data3)
        {
            base.Linear(pocket, t1, data1, t2, data2, t3, data3);
            Call.Linear(pocket, t1, ((RaiseCallFoldData)data1).Call, t2, ((RaiseCallFoldData)data2).Call, t3, ((RaiseCallFoldData)data3).Call);
        }

        public override string[] Formated
        {
            get
            {
                var d = new string[Pocket.N];
                for (int p = 0; p < Pocket.N; p++)
                    d[p] = string.Format("{0} -> {1} / {2} / {3}", Pocket.Pockets[p], Raise[p], Call[p], Fold(p));
                return d;
            }
        }

        public override string ShortFormat(int p)
        {
            return base.ShortFormat(p) + "/" + Call.ShortFormat(p);
        }

        public override number Hash()
        {
            number hash = 0;
            for (int i = 0; i < Pocket.N; i++)
            {
                //if (number.IsNaN(Raise[i])) hash += -1f * (i + 1);
                //else hash += Raise[i] * (i + 1);
                //if (number.IsNaN(Call[i])) hash += -1f * (i + 1);
                //else hash += Call[i] * (i + 1);
                hash += Raise[i] * (i + 1);
                hash += Call[i] * (i + 1);
            }

            return hash;
        }
    }
}