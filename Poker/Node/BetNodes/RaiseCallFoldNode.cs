using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
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
                //if (decimal.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get EV for raising/calling/folding.
                decimal RaiseEV = RaiseBranch.EV[p1];
                decimal CallEV = CallBranch.EV[p1];
                decimal FoldEV = -Spent;

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
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                //if (decimal.IsNaN(PocketP[p1])) continue;
                if (!MyCommunity.AvailablePocket[p1]) continue;

                // Get likelihoods for opponent raising/calling/folding.
                decimal RaiseChance = TotalChance(PocketP, _S.Raise, p1);
                decimal CallChance  = TotalChance(PocketP, _S.Call, p1);
                decimal FoldChance = ((decimal)1) - RaiseChance - CallChance;

                // Get EV assuming opponent raising/calling/folding.
                decimal RaiseEV = RaiseBranch.EV[p1];
                decimal CallEV = CallBranch.EV[p1];
                decimal FoldEV = Spent;

                // Calculate total EV
                EV[p1] = RaiseChance * RaiseEV +
                         CallChance * CallEV +
                         FoldChance * FoldEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override decimal _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            RaiseCallFoldData Data = (ActivePlayer == Player.Button ? S1 : S2)(this) as RaiseCallFoldData;
            int pocket = ActivePlayer == Player.Button ? p1 : p2;

            decimal RaiseEV = RaiseBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            decimal CallEV = CallBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            decimal FoldEV = ActivePlayer == Player.Button ? -Spent : Spent;

            decimal EV = Data.Raise[pocket] * RaiseEV +
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
