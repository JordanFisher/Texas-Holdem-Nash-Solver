using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class PocketNode : Node
    {
        public PocketNode()
            : base(null)
        {
            Initialize();
            Spent = Pot = Ante.PreDeal;
        }

        public float BestAgainstS()
        {
            ClearWorkVariables();
            CalculatePostRaisePDF();
            return CalculateBest();
        }

        public override void CalculatePostRaisePDF()
        {
            // First assume a uniform prior for which pockets opponent has.
            float UniformP = 1f / Pocket.N;
            for (int i = 0; i < Pocket.N; i++)
                PostRaiseP[i] = UniformP;

            // Then update the Pocket PDF assuming the opponent raises.
            base.CalculatePostRaisePDF();
        }

        public override float CalculateBest()
        {
            float SingleFlopWeight = 1f / Counting.Choose(Card.N - 4, 3);
            CalculateBest_AccountForOverlaps(SingleFlopWeight);

            // Calculate final EV.
            float FinalEV = 0;
            for (int p = 0; p < Pocket.N; p++)
            {
                FinalEV += EV[p];
                //Console.WriteLine("EV(p1 = {0}) = {1}", p, EV[p]);
            }
            FinalEV /= Pocket.N;
            Console.WriteLine("EV = {0}", FinalEV);
            
            return FinalEV;
        }

        public override void CreateBranches()
        {
            Branches = new List<Node>(Flop.N);

            foreach (Flop flop in Flop.Flops)
                Branches.Add(new FlopNode(this, flop));
            BranchesByIndex = Branches;
        }
    }
}