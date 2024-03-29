﻿using System;
using System.Collections.Generic;


namespace Poker
{
#if SINGLE
	using number = Single;
#elif DOUBLE
	using number = Double;
#elif DECIMAL
	using number = Decimal;
#endif

    class CallFoldNode : BetNode
    {
        protected Node CallBranch;

        public CallFoldNode(Node parent, PlayerAction ActionTaken, Player ActivePlayer, int Spent, int Pot, int NumRaises, int DataOffset = 0)
            : base(parent, ActionTaken, ActivePlayer, Spent, Pot, NumRaises, DataOffset)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            S = new PocketData();
            B = new PocketData();
            if (MakeHold) Hold = new PocketData();

            CreateBranches();
        }

        protected override void UpdateChildrensPDFs_Inactive()
        {
            Update(PocketP, S, CallBranch.PocketP);
        }

        protected override void CreateBranches()
        {
            if (Phase == BettingPhase.River)
                CallBranch = new ShowdownNode(this, Pot);
            else
                CallBranch = Junction.GetJunction(Phase, this, Pot, Pot);
            
            Branches = new List<Node>(1);
            Branches.Add(CallBranch);
        }

        protected override void CalculateBest_Active(Player Opponent)
        {
            // First decide strategy for children nodes.
            CallBranch.PocketP.CopyFrom(PocketP);
            CallBranch.CalculateBestAgainst(Opponent);

            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get EV for raising/calling/folding.
                number CallEV = CallBranch.EV[p1];
                number FoldEV = -Spent;

                // Decide strategy based on which action is better.
                if (CallEV >= FoldEV)
                {
                    B[p1] = 1;
                    EV[p1] = CallEV;
                }
                else
                {
                    B[p1] = 0;
                    EV[p1] = FoldEV;
                }
                Assert.IsNum(EV[p1]);
            }
        }

        protected override void CalculateBest_Inactive(Player Opponent)
        {
            // First decide strategy for children nodes.
            Update(PocketP, S, CallBranch.PocketP);
            CallBranch.CalculateBestAgainst(Opponent);

            // For each pocket we might have, calculate what we expect to happen.
			if (!DerivedSetup.Naive)
	            Data.ChanceToActPrecomputation(PocketP, S, MyCommunity);

            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get likelihoods for opponent raising/calling/folding.
                number CallChance = TotalChance(PocketP, S, p1);
                number FoldChance = ((number)1) - CallChance;

                // Get EV assuming opponent raising/calling/folding.
                number CallEV = CallBranch.EV[p1];
                number FoldEV = Spent;

                // Calculate total EV
                EV[p1] = CallChance * CallEV +
                         FoldChance * FoldEV;
                Assert.IsNum(EV[p1]);
            }
        }


		public override void _CombineStrats(int p, number t1, number t2, Player player)
		{
			t1 = Tools.Restrict(t1); t2 = 1 - t1;

			number _t1, _t2;

			if (S != null && B != null && ActivePlayer == player && MyCommunity.AvailablePocket[p])
			{
				// Update Call branch
				number normalize = (t1 * S[p] + t2 * B[p]);

				if (normalize == 0)
				{
					_t1 = _t2 = 0;
				}
				else
				{
					_t1 = t1 * S[p] / normalize;
					_t2 = t2 * B[p] / normalize;
				}

				CallBranch._CombineStrats(p, _t1, _t2, player);

				// Update this node's strategy
				S.Linear(p, t1, S, t2, B);

				Assert.That(t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1 && (Tools.Equals(t1 + t2, 1) || t1 == 0 && t2 == 0));
				Assert.That(S.IsValid());
			}
			else
			{
				_t1 = t1; _t2 = t2;

				CallBranch._CombineStrats(p, _t1, _t2, player);
			}
		}

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data = (ActivePlayer == Player.Button ? S1 : S2)(this);
            int pocket = ActivePlayer == Player.Button ? p1 : p2;

            number CallEV = CallBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            number FoldEV = ActivePlayer == Player.Button ? -Spent : Spent;

            number EV = Data[pocket] * CallEV +
                        (1 - Data[pocket]) * FoldEV;
            Assert.IsNum(EV);

            return EV;
        }

        public override Node AdvanceHead(PlayerAction action)
        {
            Assert.That(action == PlayerAction.Call || action == PlayerAction.Fold);

            if (action == PlayerAction.Call) return CallBranch;
            else return null;
        }
    }
}
