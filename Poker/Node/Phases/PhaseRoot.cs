﻿using System;
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

    public enum BettingPhase { NotSet, PreFlop, Flop, Turn, River };

    class PhaseRoot : Node
    {
        protected Node BettingBranch;
        protected Player InitiallyActivePlayer = Player.Undefined;

        public PhaseRoot(Node Parent, CommunityNode Community, int Spent, int Pot, int RootCount)
            : base(Parent, Spent, Pot)
        {
            MyPhaseRoot = this;
            MyCommunity = Community;

            Weight = MyCommunity.Weight;
            Phase = MyCommunity.Phase;
            InitiallyActivePlayer = MyCommunity.InitiallyActivePlayer;

            DataOffset = MaxDepth + RootCount;
            if (Phase == BettingPhase.Turn) DataOffset += Flop.N;
            if (Phase == BettingPhase.River) DataOffset += Flop.N + Card.N;

            Initialize();
        }

        protected override void Initialize()
        {
            //PocketP = new PocketData();

            CreateBranches();
            //EV = BettingBranch.EV;
            //EV.CopyFrom(BettingBranch.EV);
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            UpdateChildrensPDFs(Opponent);
            BettingBranch.CalculateBestAgainst(Opponent);
            EV.CopyFrom(BettingBranch.EV);
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            if (Branches == null) return;

            if (Parent != null)
            {
                // Copy the parent node's pocket PDF
                PocketP.CopyFrom(Parent.PocketP);

                // Pocket can't have cards that are in the community
                number NewTotalMass = 0;
                for (int p = 0; p < Pocket.N; p++)
                {
                    if (MyCommunity.AvailablePocket[p])
                        NewTotalMass += PocketP[p];
                }
                Assert.AlmostPos(NewTotalMass);

                if (NewTotalMass <= 0) NewTotalMass = 1;

                // Normalize Pocket PDF 
                for (int i = 0; i < Pocket.N; i++)
                    PocketP[i] = PocketP[i] / NewTotalMass;
            }

            BettingBranch.PocketP.CopyFrom(PocketP);
            base.UpdateChildrensPDFs(Opponent);
        }

        protected override void CreateBranches()
        {
            if (BetNode.SimultaneousBetting)
                BettingBranch = new SimultaneousNode(this, Spent, Pot, Phase);
            else
            {
                if (Phase == BettingPhase.PreFlop)
                    BettingBranch = new FirstActionNode_PreFlop(this, InitiallyActivePlayer, Spent, Pot);
                else
                    BettingBranch = new FirstActionNode_PostFlop(this, InitiallyActivePlayer, Spent, Pot);
            }

            Branches = new List<Node>(1);
            Branches.Add(BettingBranch);
        }

        public override number _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            return BettingBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
        }

        public override Node AdvanceHead(PlayerAction action)
        {
            Assert.That(action == PlayerAction.Nothing);

            return BettingBranch;
        }
    }
}