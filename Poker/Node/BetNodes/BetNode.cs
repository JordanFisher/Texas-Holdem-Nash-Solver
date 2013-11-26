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

    public enum Player { Button, Dealer, Undefined };

    class BetNode : Node
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public const bool SimultaneousBetting = Setup.SimultaneousBetting;
        public const int AllowedRaises = Setup.AllowedRaises;

        protected int NumRaises;
        protected Player ActivePlayer;
        protected int RaiseVal = Setup.RaiseAmount;

		public BetNode(Node parent, PlayerAction ActionTaken, Player ActivePlayer, int Spent, int Pot, int NumRaises, int DataOffset = 0)
            : base(parent, Spent, Pot)
        {
			switch (ActionTaken)
			{
				case PlayerAction.Raise:	BetCode += 'r'; break;
				case PlayerAction.Call:		BetCode += 'c'; break;
				
				case PlayerAction.Fold:		Assert.NotReached(); break;
				default:					BetCode += '/'; break;
			}

            Phase = MyPhaseRoot.Phase;
            Weight = 0;

            this.ActivePlayer = ActivePlayer;
            this.NumRaises = NumRaises;

            this.DataOffset += DataOffset;

#if DEBUG
            InstanceCount++;
#endif
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            Assert.That(ActivePlayer != Player.Undefined);

            if (!(this is CallFoldNode))
                UpdateChildrensPDFs(Opponent);

            if (ActivePlayer == Opponent)
                CalculateBest_Inactive(Opponent);
            else
                CalculateBest_Active(Opponent);
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            Assert.That(ActivePlayer != Player.Undefined);

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
