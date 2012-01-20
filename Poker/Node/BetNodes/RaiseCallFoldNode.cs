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

    class FirstActionNode_PreFlop : RaiseCallFoldNode
    {
        public FirstActionNode_PreFlop(Node parent, Player ActivePlayer, int Spent, int Pot)
            : base(parent, ActivePlayer, Spent, Pot, 0)
        {
        }

        protected override void CreateBranches()
        {
            if (NumRaises + 1 == AllowedRaises)
                RaiseBranch =      new CallFoldNode(this, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1);
            else
                RaiseBranch = new RaiseCallFoldNode(this, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1);
            CallBranch = new RaiseCheckNode(this, Tools.NextPlayer(ActivePlayer), Pot, Pot, NumRaises);
            
            Branches = new List<Node>(2);
            Branches.Add(RaiseBranch);
            Branches.Add(CallBranch);
        }
    }

    class RaiseCallFoldNode : BetNode
    {
        protected Node RaiseBranch, CallBranch;

        public RaiseCallFoldNode(Node parent, Player ActivePlayer, int Spent, int Pot, int NumRaises)
            : base(parent, ActivePlayer, Spent, Pot, NumRaises)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            PocketP = new PocketData();
            EV = new PocketData();

            S = new RaiseCallFoldData();
            B = new RaiseCallFoldData();
            if (MakeHold) Hold = new RaiseCallFoldData();

            CreateBranches();
        }


        protected override void CreateBranches()
        {
            if (NumRaises + 1 == AllowedRaises)
                RaiseBranch =      new CallFoldNode(this, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1);
            else
                RaiseBranch = new RaiseCallFoldNode(this, Tools.NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1);

            if (Phase == BettingPhase.River)
                CallBranch = new ShowdownNode(this, Pot);
            else
                CallBranch = Junction.GetJunction(Phase, this, Pot, Pot);
            
            Branches = new List<Node>(2);
            Branches.Add(RaiseBranch);
            Branches.Add(CallBranch);
        }

        protected override void UpdateChildrensPDFs_Inactive()
        {
            RaiseCallFoldData _S = S as RaiseCallFoldData;

            Update(PocketP, _S.Raise, RaiseBranch.PocketP);
            Update(PocketP, _S.Call, CallBranch.PocketP);
        }

        protected override void CalculateBest_Active(Player Opponent)
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

            RaiseCallFoldData _B = B as RaiseCallFoldData;

            Assert.That(_B.IsValid());

            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (number.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get EV for raising/calling/folding.
                number RaiseEV = RaiseBranch.EV[p1];
                number CallEV = CallBranch.EV[p1];
                number FoldEV = -Spent;

                // Decide strategy based on which action is better.
                if (RaiseEV >= CallEV && RaiseEV >= FoldEV)
                {
                    _B.Set(p1, 1, 0);
                    EV[p1] = RaiseEV;
                }
                else if (CallEV >= FoldEV)
                {
                    _B.Set(p1, 0, 1);
                    EV[p1] = CallEV;
                }
                else
                {
                    _B.Set(p1, 0, 0);
                    EV[p1] = FoldEV;
                }
                Assert.IsNum(EV[p1]);
            }
        }

        protected override void CalculateBest_Inactive(Player Opponent)
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

            RaiseCallFoldData _S = S as RaiseCallFoldData;

            Assert.That(_S.IsValid());

            // For each pocket we might have, calculate what we expect to happen.
#if NAIVE
#else
            Optimize.Data.ChanceToActPrecomputation(PocketP, _S.Raise, MyCommunity);
            Optimize.Data2.ChanceToActPrecomputation(PocketP, _S.Call, MyCommunity);
#endif
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (number.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get likelihoods for opponent raising/calling/folding.
#if NAIVE
                number RaiseChance = TotalChance(PocketP, _S.Raise, p1);
                number CallChance = TotalChance(PocketP, _S.Call, p1);
#else
                number RaiseChance = Optimize.Data.ChanceToActWithExclusion(PocketP, _S.Raise, p1);
                number CallChance = Optimize.Data2.ChanceToActWithExclusion(PocketP, _S.Call, p1);
#endif
                number FoldChance = ((number)1) - RaiseChance - CallChance;

                // Get EV assuming opponent raising/calling/folding.
                number RaiseEV = RaiseBranch.EV[p1];
                number CallEV = CallBranch.EV[p1];
                number FoldEV = Spent;

                // Calculate total EV
                EV[p1] = RaiseChance * RaiseEV +
                         CallChance * CallEV +
                         FoldChance * FoldEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            RaiseCallFoldData Data = (ActivePlayer == Player.Button ? S1 : S2)(this) as RaiseCallFoldData;
            int pocket = ActivePlayer == Player.Button ? p1 : p2;

            number RaiseEV = RaiseBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            number CallEV = CallBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            number FoldEV = ActivePlayer == Player.Button ? -Spent : Spent;

            number EV = Data.Raise[pocket] * RaiseEV +
                        Data.Call[pocket]  * CallEV  +
                        Data.Fold(pocket)  * FoldEV;
            Assert.IsNum(EV);

            return EV;
        }

        public override Node AdvanceHead(PlayerAction action)
        {
            Assert.That(action == PlayerAction.Raise || action == PlayerAction.Call || action == PlayerAction.Fold);

            if (action == PlayerAction.Raise) return RaiseBranch;
            else if (action == PlayerAction.Call) return CallBranch;
            else return null;
        }
    }
}
