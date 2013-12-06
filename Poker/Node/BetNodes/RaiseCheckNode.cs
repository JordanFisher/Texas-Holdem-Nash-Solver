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

    class FirstActionNode_PostFlop : RaiseCheckNode
    {
        public FirstActionNode_PostFlop(Node parent, Player ActivePlayer, int Spent, int Pot)
            : base(parent, PlayerAction.Nothing, ActivePlayer, Spent, Pot, 0)
        {
        }

        protected override void CreateBranches()
        {
            if (NumRaises + 1 == AllowedRaises)
                RaiseBranch = new CallFoldNode	   (this, PlayerAction.Raise, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1, Node.MaxDepth / 2);
            else
                RaiseBranch = new RaiseCallFoldNode(this, PlayerAction.Raise,  Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1, Node.MaxDepth / 2);
            
			CheckBranch =     new RaiseCheckNode   (this, PlayerAction.Call,  Tools.NextPlayer(ActivePlayer), Pot, Pot, NumRaises);

            Branches = new List<Node>(2);
            Branches.Add(RaiseBranch);
            Branches.Add(CheckBranch);
        }
    }

    class RaiseCheckNode : BetNode
    {
        protected Node RaiseBranch, CheckBranch;

        public RaiseCheckNode(Node parent, PlayerAction ActionTaken, Player ActivePlayer, int Spent, int Pot, int NumRaises)
            : base(parent, ActionTaken, ActivePlayer, Spent, Pot, NumRaises)
        {
            Assert.That(Spent == Pot);
            Initialize();
        }

        protected override void Initialize()
        {
            S = new PocketData();
            B = new PocketData();
            if (MakeHold) Hold = new PocketData();

            CreateBranches();
        }


		PocketData NotS { get { return MyCommunity.NotS; } }
		//public static PocketData NotS = new PocketData();

        protected override void UpdateChildrensPDFs_Inactive()
        {
            NotS.InverseOf(S);

            Update(PocketP, S, RaiseBranch.PocketP);
            Update(PocketP, NotS, CheckBranch.PocketP);
        }

        protected override void CreateBranches()
        {
            if (NumRaises + 1 == AllowedRaises)
                RaiseBranch =      new CallFoldNode(this, PlayerAction.Raise, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1, Node.MaxDepth / 2);
            else
                RaiseBranch = new RaiseCallFoldNode(this, PlayerAction.Raise, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1, Node.MaxDepth / 2);

            if (Phase == BettingPhase.River)
                CheckBranch = new ShowdownNode(this, Pot);
            else
                CheckBranch = Junction.GetJunction(Phase, this, Pot, Pot);

            Branches = new List<Node>(2);
            Branches.Add(RaiseBranch);
            Branches.Add(CheckBranch);
        }

        protected override void CalculateBest_Active(Player Opponent)
        {
            // First decide strategy for children nodes.
            RaiseBranch.PocketP.CopyFrom(PocketP);
            RaiseBranch.CalculateBestAgainst(Opponent);
            CheckBranch.PocketP.CopyFrom(PocketP);
            CheckBranch.CalculateBestAgainst(Opponent);

            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (number.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get EV for raising/Checking/folding.
                number RaiseEV = RaiseBranch.EV[p1];
                number CheckEV = CheckBranch.EV[p1];

                // Decide strategy based on which action is better.
                if (RaiseEV >= CheckEV)
                {
                    B[p1] = 1;
                    EV[p1] = RaiseEV;
                }
                else
                {
                    B[p1] = 0;
                    EV[p1] = CheckEV;
                }
                Assert.IsNum(EV[p1]);
            }
        }

        protected override void CalculateBest_Inactive(Player Opponent)
        {
            // First decide strategy for children nodes.
            NotS.InverseOf(S);
            Update(PocketP, S, RaiseBranch.PocketP);
            Update(PocketP, NotS, CheckBranch.PocketP);

            RaiseBranch.CalculateBestAgainst(Opponent);
            CheckBranch.CalculateBestAgainst(Opponent);
            

            // For each pocket we might have, calculate what we expect to happen.
#if NAIVE
#else
            Data.ChanceToActPrecomputation(PocketP, S, MyCommunity);
#endif
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get likelihoods for opponent raising/Checking/folding.
#if NAIVE
                number RaiseChance = TotalChance(PocketP, S, p1);
#else
                number RaiseChance = Data.ChanceToActWithExclusion(PocketP, S, p1);
#endif
                number CheckChance = 1 - RaiseChance;

                // Get EV assuming opponent raising/Checking/folding.
                number RaiseEV = RaiseBranch.EV[p1];
                number CheckEV = CheckBranch.EV[p1];

                // Calculate total EV
                EV[p1] = RaiseChance * RaiseEV +
                         CheckChance * CheckEV;
                Assert.IsNum(EV[p1]);
            }
        }

		public override void _CombineStrats(int p, number t1, number t2, Player player)
		{
			t1 = Tools.Restrict(t1); t2 = 1 - t1;

			number _t1, _t2;

			if (S != null && B != null && ActivePlayer == player && MyCommunity.AvailablePocket[p])
			{
				// Update Raise branch
				number normalize_r = (t1 * S[p] + t2 * B[p]);

				if (normalize_r == 0)
				{
					_t1 = _t2 = 0;
				}
				else
				{
					_t1 = t1 * S[p] / normalize_r;
					_t2 = t2 * B[p] / normalize_r;
				}

				RaiseBranch._CombineStrats(p, _t1, _t2, player);

				// Update Check branch
				number normalize_c = (t1 * (1 - S[p]) + t2 * (1 - B[p]));

				if (normalize_c == 0)
				{
					_t1 = _t2 = 0;
				}
				else
				{
					_t1 = t1 * (1 - S[p]) / normalize_c;
					_t2 = t2 * (1 - B[p]) / normalize_c;
				}

				CheckBranch._CombineStrats(p, _t1, _t2, player);

				// Update this node's strategy
				S.Linear(p, t1, S, t2, B);

				Assert.That(t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1 && (Tools.Equals(t1 + t2, 1) || t1 == 0 && t2 == 0));
				Assert.That(S.IsValid());
			}
			else
			{
				_t1 = t1; _t2 = t2;

				RaiseBranch._CombineStrats(p, _t1, _t2, player);
				CheckBranch._CombineStrats(p, _t1, _t2, player);
			}
		}

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data = (ActivePlayer == Player.Button ? S1 : S2)(this);
            int pocket = ActivePlayer == Player.Button ? p1 : p2;

            number RaiseEV = RaiseBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            number CheckEV = CheckBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);

            number EV = Data[pocket] * RaiseEV +
                        (1 - Data[pocket]) * CheckEV;
            Assert.IsNum(EV);

            return EV;
        }

        public override Node AdvanceHead(PlayerAction action)
        {
            Assert.That(action == PlayerAction.Raise || action == PlayerAction.Call);

            if (action == PlayerAction.Raise) return RaiseBranch;
            else return CheckBranch;
        }
    }
}
