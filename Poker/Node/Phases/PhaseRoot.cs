using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    public enum BettingPhase { NotSet, PreFlop, Flop, Turn, River };

    class PhaseRoot : Node
    {
        protected Node BettingBranch;
        protected Player InitiallyActivePlayer = Player.Undefined;

        public PhaseRoot(Node Parent, CommunityNode Community, int Spent, int Pot)
            : base(Parent, Spent, Pot)
        {
            MyPhaseRoot = this;
            MyCommunity = Community;

            Weight = MyCommunity.Weight;
            Phase = MyCommunity.Phase;
            InitiallyActivePlayer = MyCommunity.InitiallyActivePlayer;

            Initialize();
        }

        protected override void Initialize()
        {
            PocketP = new PocketData();

            CreateBranches();
            EV = BettingBranch.EV;
        }

        public override void CalculateBestAgainst(Player Opponent)
        {
            BettingBranch.CalculateBestAgainst(Opponent);
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            if (Parent != null)
            {
                // Copy the parent node's post-raise pocket PDF
                PocketP.CopyFrom(Parent.PocketP);

                // Pocket can't have cards that are in this flop
                double NewTotalMass = 0f;
                for (int p = 0; p < Pocket.N; p++)
                {
                    if (double.IsNaN(PocketP[p])) continue;

                    if (MyCommunity.NewCollision(p))
                        PocketP[p] = double.NaN;
                    else
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
                BettingBranch = new FirstActionNode(this, InitiallyActivePlayer, Spent, Pot);
            
            Branches = new List<Node>(1);
            Branches.Add(BettingBranch);
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            return BettingBranch._Simulate(S1, S2, p1, p2, ref BranchIndex, IndexOffset);
        }

        /*
        public override bool NewCollision(Pocket p)
        {
            return MyCommunity.NewCollision(p);
        }

        public override bool Collision(Pocket p)
        {
            return MyCommunity.Collision(p);
        }

        public override bool Contains(int card)
        {
            return MyCommunity.Contains(card);
        }*/
    }
}