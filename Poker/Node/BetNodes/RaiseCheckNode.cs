﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class RaiseCheckNode : BetNode
    {
        protected Node RaiseBranch, CheckBranch;

        public RaiseCheckNode(Node parent, Player ActivePlayer, int Spent, int Pot, int NumRaises)
            : base(parent, ActivePlayer, Spent, Pot, NumRaises)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            PocketP = new PocketData();
            EV = new PocketData();

            S = new PocketData();
            B = new PocketData();
            //Hold = new PocketData();

            CreateBranches();
        }

        public static PocketData NotS = new PocketData();
        protected override void UpdateChildrensPDFs_Inactive()
        {
            NotS.InverseOf(S);

            Update(PocketP, S, RaiseBranch.PocketP);
            Update(PocketP, NotS, CheckBranch.PocketP);
        }

        public override void CreateBranches()
        {
            if (NumRaises + 1 == AllowedRaises)
                RaiseBranch =      new CallFoldNode(this, NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1);
            else
                RaiseBranch = new RaiseCallFoldNode(this, NextPlayer(ActivePlayer), Pot, Pot + RaiseVal, NumRaises + 1);

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
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) continue;

                // Get EV for raising/Checking/folding.
                double RaiseEV = RaiseBranch.EV[p1];
                double CheckEV = CheckBranch.EV[p1];

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
            foreach (Node node in Branches)
                node.CalculateBestAgainst(Opponent);

            // For each pocket we might have, calculate what we expect to happen.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) continue;

                // Get likelihoods for opponent raising/Checking/folding.
                double RaiseChance = TotalChance(PocketP, S, p1);
                double CheckChance = 1 - RaiseChance;

                // Get EV assuming opponent raising/Checking/folding.
                double RaiseEV = RaiseBranch.EV[p1];
                double CheckEV = CheckBranch.EV[p1];

                // Calculate total EV
                EV[p1] = RaiseChance * RaiseEV +
                         CheckChance * CheckEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data = (ActivePlayer == Player.Button ? S1 : S2)(this);
            int pocket = ActivePlayer == Player.Button ? p1 : p2;

            double RaiseEV = RaiseBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            double CheckEV = CheckBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);

            double EV = Data[pocket] * RaiseEV +
                        (1 - Data[pocket]) * CheckEV;
            Assert.IsNum(EV);

            return EV;
        }
    }
}
