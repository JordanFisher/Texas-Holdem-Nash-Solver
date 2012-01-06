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
        public const int InitialSpent = BetNode.SimultaneousBetting ? Ante.PreDeal : Ante.LittleBlind;
        public const int InitialBet = BetNode.SimultaneousBetting ? Ante.PreDeal : Ante.BigBlind;
        public PocketRoot()
            : base(null, InitialSpent, InitialBet)
        {
            Phase = BettingPhase.PreFlop;
            InitiallyActivePlayer = Player.Button;

            Initialize();
        }

        public double BestAgainstS()
        {
            ClearWorkVariables();
            UpdateChildrensPDFs();
            CalculateBest();

            double FinalEV = EV.Average();
            Console.WriteLine("EV = {0}", FinalEV);
            return FinalEV;
        }

        protected override void UpdateChildrensPDFs()
        {
            // Initially assume a uniform prior for which pockets opponent has.
            double UniformP = 1f / Pocket.N;
            for (int i = 0; i < Pocket.N; i++)
                PocketP[i] = UniformP;

            base.UpdateChildrensPDFs();
        }

        public double Simulate(Var S1, Var S2, int p1, int p2, params int[] BranchIndex)
        {
            return _Simulate(S1, S2, p1, p2, ref BranchIndex, 0);
        }
    }
}