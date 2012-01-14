using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    public enum Player { Button, Dealer, Undefined };

    class BetNode : Node
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public const bool SimultaneousBetting = true;
        public const int AllowedRaises = 1;
        protected int NumRaises;

        protected Player ActivePlayer;

        protected int RaiseVal = Ante.RaiseAmount;

        public BetNode(Node parent, Player ActivePlayer, int Spent, int Pot, int NumRaises)
            : base(parent, Spent, Pot)
        {
            Phase = MyPhaseRoot.Phase;
            Weight = double.NaN;

            this.ActivePlayer = ActivePlayer;
            this.NumRaises = NumRaises;

#if DEBUG
            InstanceCount++;
            //Console.WriteLine(this);
#endif
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            Assert.That(ActivePlayer != Player.Undefined);

            //if (ActivePlayer != Player.Button)
            if (ActivePlayer == Opponent)
                CalculateBest_Inactive(Opponent);
            else
                CalculateBest_Active(Opponent);
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            Assert.That(ActivePlayer != Player.Undefined);

            //if (ActivePlayer != Player.Button)
            if (ActivePlayer == Opponent)
                UpdateChildrensPDFs_Inactive();
            else
                UpdateChildrensPDFs_Active();

            base.UpdateChildrensPDFs(Opponent);
        }

        protected virtual void UpdateChildrensPDFs_Active()
        {
            if (Branches != null) foreach (Node Branch in Branches)
                    Branch.PocketP.CopyFrom(PocketP);
        }
        protected virtual void UpdateChildrensPDFs_Inactive() { }

        protected virtual void CalculateBest_Active(Player Opponent) { }
        protected virtual void CalculateBest_Inactive(Player Opponent) { }

        public override string ToString()
        {
            if (ActivePlayer == Player.Button)
                return string.Format("{0} bet, button in for {1}", Pot, Spent);
            else
                return string.Format("{0} bet, dealer in for {1}", Pot, Spent);
        }
    }
}
