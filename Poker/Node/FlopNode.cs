using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class FlopNode : Node
    {
        public Flop MyFlop;

        public FlopNode(Node parent, Flop flop, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            MyFlop = flop;
            Weight = 1f / Flop.N;

            Initialize();
            //Spent = Pot = Ante.PreDeal + Ante.PreFlop;
        }

        public override void CalculatePostRaisePDF()
        {
            CalculatePostRaisePDF_AccountForOverlaps();

            base.CalculatePostRaisePDF();
        }

        public override float CalculateBest()
        {
            float SingleTurnWeight = 1f / (Card.N - 4 - 3);
            CalculateBest_AccountForOverlaps(SingleTurnWeight);

            return float.MinValue;
        }

        public override void CreateBranches()
        {
            Branches = new List<Node>(Card.N - 3);
            BranchesByIndex = new List<Node>(Card.N);
            //return;

            for (int turn = 0; turn < Card.N; turn++)
            {
                Node NewBranch;
                if (!MyFlop.Contains(turn))
                {
                    NewBranch = new TurnNode(this, MyFlop, turn);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Overlaps(MyFlop);
        }

        public override bool Collision(Pocket p)
        {
            return p.Overlaps(MyFlop);
        }

        public override string ToString()
        {
            return MyFlop.ToString();
        }
    }
}