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

    class StrategyPlayer : PlayerImplementation
    {
		public override void SetName(int PlayerNumber, PlayerImplementation Opponent)
		{
			if (Opponent is HumanPlayer)
				Name = string.Format("Nash", PlayerNumber);
			else
				Name = string.Format("Computer {0}", PlayerNumber);
		}

        Var Strategy;
        Node Head;

        public StrategyPlayer(Var Strategy)
        {
            this.Strategy = Strategy;
        }

        public override void Reset()
        {
            Head = PocketRoot.Root;
        }

        public override PlayerAction GetAction()
        {
            if (BetNode.SimultaneousBetting)
                return GetAction_Simultaneous();
            else
                return GetAction_Complex();
        }

        public PlayerAction GetAction_Simultaneous()
        {
            PocketData Data = Strategy(Head);

            number d = MyGame.Rand();
            PlayerAction action = PlayerAction.Nothing;
            if (d < Data[Pocket])
                action = PlayerAction.Raise;
            else
                action = PlayerAction.Fold;

            Head = Head.AdvanceHead(action);
            return action;
        }

        public PlayerAction GetAction_Complex()
        {
            PocketData Data = Strategy(Head);

            number d = MyGame.Rand();

            PlayerAction action = PlayerAction.Nothing;

            RaiseCallFoldData RCF = Data as RaiseCallFoldData;
            if (null != RCF)
            {
                if (d < RCF.Raise[Pocket])
                    action = PlayerAction.Raise;
                else if (d < RCF.Raise[Pocket] + RCF.Call[Pocket])
                    action = PlayerAction.Call;
                else
                    action = PlayerAction.Fold;
            }
            else if (Head is RaiseCheckNode)
            {
                if (d < Data[Pocket])
                    action = PlayerAction.Raise;
                else
                    action = PlayerAction.Call;
            }
            else
            {
                if (d < Data[Pocket])
                    action = PlayerAction.Call;
                else
                    action = PlayerAction.Fold;
            }

            Head = Head.AdvanceHead(action);
            return action;
        }

        public override void OpponentDoes(PlayerAction action)
        {
            if (BetNode.SimultaneousBetting)
                return;
            else
                Head = Head.AdvanceHead(action);
        }

        int Pocket;
        public override void SetPocket(int c1, int c2)
        {
            Pocket = Game.PocketLookup[c1, c2];
            Head = Head.AdvanceHead(PlayerAction.Nothing);
        }

		Flop TrueFlop;
		int TruePocket, TrueTurn, TrueRiver;

        public override void SetFlop(int c1, int c2, int c3)
        {
            int flop = Game.FlopLookup[c1, c2, c3];

			if (Flop.SuitReduce)
			{
				TrueFlop = Flop.Flops[flop];
				flop = Flop.RepresentativeOf(flop);

				TruePocket = Pocket;
				Pocket = TrueFlop.PocketMap[Pocket];
			}

			Head = Head.AdvanceHead(flop);
		}

        public override void SetTurn(int c1)
        {
			if (Flop.SuitReduce)
			{
				TrueTurn = c1;
				c1 = TrueFlop.CardMap[c1];
			}

			Head = Head.AdvanceHead(c1);
        }

        public override void SetRiver(int c1)
        {
			if (Flop.SuitReduce)
			{
				TrueRiver = c1;
				c1 = TrueFlop.CardMap[c1];
			}

            Head = Head.AdvanceHead(c1);
        }
    }
}