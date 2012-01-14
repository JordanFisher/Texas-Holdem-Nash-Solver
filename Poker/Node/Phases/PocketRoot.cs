using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class PocketRoot : PhaseRoot
    {
        public static PocketRoot Root;

        public const int InitialSpent = BetNode.SimultaneousBetting ? Ante.PreDeal : Ante.LittleBlind;
        public const int InitialBet = BetNode.SimultaneousBetting ? Ante.PreDeal : Ante.BigBlind;
        public PocketRoot()
            : base(null, CommunityNode.Root, InitialSpent, InitialBet)
        {
        }

        public double BestAgainstS(Player Opponent)
        {
            //ClearWorkVariables();
            UpdateChildrensPDFs(Opponent);
            CalculateBestAgainst(Opponent);

            double FinalEV = EV.Average();
            return FinalEV;
        }

        public double BestAgainstS()
        {
            double FinalEV;

            if (BetNode.SimultaneousBetting)
            {
                FinalEV = BestAgainstS(Player.Undefined);
                Console.WriteLine("EV = {0}", FinalEV);
            }
            else
            {
                double EV_AgainstButton = BestAgainstS(Player.Button);
                double EV_AgainstDealer = BestAgainstS(Player.Dealer);
                FinalEV = .5f * (EV_AgainstButton + EV_AgainstDealer);
                Console.WriteLine("EV = {0} : {1} -> {2}", EV_AgainstButton, EV_AgainstDealer, FinalEV);
            }

            return FinalEV;
        }

        protected override void UpdateChildrensPDFs(Player Opponent)
        {
            // Initially assume a uniform prior for which pockets opponent has.
            double UniformP = 1f / Pocket.N;
            for (int i = 0; i < Pocket.N; i++)
                PocketP[i] = UniformP;

            base.UpdateChildrensPDFs(Opponent);
        }

        public double Simulate(Var S1, Var S2, int p1, int p2, params int[] BranchIndex)
        {
            return _Simulate(S1, S2, p1, p2, ref BranchIndex, 0);
        }
    }
}