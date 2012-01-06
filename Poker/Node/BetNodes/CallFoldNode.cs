﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class CallFoldNode : BetNode
    {
        protected Node CallBranch;

        public CallFoldNode(Node parent, Player ActivePlayer, int Spent, int Pot, int NumRaises)
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

        protected override void UpdateChildrensPDFs_Inactive()
        {
            Update(PocketP, S, CallBranch.PocketP);
        }

        public override void CreateBranches()
        {
            if (Phase == BettingPhase.River)
                CallBranch = new ShowdownNode(this, Pot);
            else
                CallBranch = Junction.GetJunction(Phase, this, Pot, Pot);
            
            Branches = new List<Node>(1);
            Branches.Add(CallBranch);
        }

        protected override void CalculateBest_Active()
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBest();

            // For each pocket we might have, calculate what we should do.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) continue;

                // Get EV for raising/calling/folding.
                double CallEV = CallBranch.EV[p1];
                double FoldEV = -Spent;

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

        protected override void CalculateBest_Inactive()
        {
            // First decide strategy for children nodes.
            foreach (Node node in Branches)
                node.CalculateBest();

            // For each pocket we might have, calculate what we expect to happen.
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PocketP[p1])) continue;

                // Get likelihoods for opponent raising/calling/folding.
                double CallChance = TotalChance(PocketP, S, p1);
                double FoldChance = 1f - CallChance;

                // Get EV assuming opponent raising/calling/folding.
                double CallEV = CallBranch.EV[p1];
                double FoldEV = Spent;

                // Calculate total EV
                EV[p1] = CallChance * CallEV +
                         FoldChance * FoldEV;
                Assert.IsNum(EV[p1]);
            }
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data = (ActivePlayer == Player.Button ? S1 : S2)(this);
            int pocket = ActivePlayer == Player.Button ? p1 : p2;

            double CallEV = CallBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
            double FoldEV = ActivePlayer == Player.Button ? -Spent : Spent;

            double EV = Data[pocket] * CallEV +
                        (1 - Data[pocket]) * FoldEV;
            Assert.IsNum(EV);

            return EV;
        }
    }
}