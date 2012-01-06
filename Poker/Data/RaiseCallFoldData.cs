using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class RaiseCallFoldData : PocketData
    {
        public PocketData Raise, Call;
        public RaiseCallFoldData()
        {
            this.Raise = this;
            this.Call = new PocketData();
        }

        public double Fold(int n) { return 1f - Raise[n] - Call[n]; }

        public void Set(int pocket, double r, double c)
        {
            Assert.IsProbability(r);
            Assert.IsProbability(c);
            Assert.IsProbability(r + c);

            Raise[pocket] = r;
            Call[pocket] = c;
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

        public virtual void SwitchWith(PocketData other)
        {
            RaiseCallFoldData _other = other as RaiseCallFoldData;
            Assert.That(_other != null);

            base.SwitchWith(other);
            Call.SwitchWith(_other.Call);
        }

        public override void Linear(int pocket, double t1, PocketData data1, double t2, PocketData data2)
        {
            Raise.Linear(pocket, t1, data1, t2, data2);
            Call.Linear(pocket, t1, data1, t2, data2);
        }

        public override void Linear(int pocket, double t1, PocketData data1, double t2, PocketData data2, double t3, PocketData data3)
        {
            Raise.Linear(pocket, t1, data1, t2, data2, t3, data3);
            Call.Linear(pocket, t1, data1, t2, data2, t3, data3);
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

        public override double Hash()
        {
            double hash = 0;
            for (int i = 0; i < Pocket.N; i++)
            {
                if (double.IsNaN(Raise[i])) hash += -1f * (i + 1);
                else hash += Raise[i] * (i + 1);
                if (double.IsNaN(Call[i])) hash += -1f * (i + 1);
                else hash += Call[i] * (i + 1);
            }

            return hash;
        }
    }
}