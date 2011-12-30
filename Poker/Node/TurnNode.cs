using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class TurnNode : Node
    {
        public Flop MyFlop;
        public int MyTurn;

        public TurnNode(Node parent, Flop flop, int turn)
            : base(parent)
        {
            MyFlop = flop;
            MyTurn = turn;

            Initialize();
            Spent = Pot = Ante.PreDeal + Ante.PreFlop + Ante.Flop;
        }

        public override void CalculatePostRaisePDF()
        {
            CalculatePostRaisePDF_AccountForOverlaps();

            base.CalculatePostRaisePDF();
        }

        public override float CalculateBest()
        {
            float SingleRiverWeight = 1f / (Card.N - 4 - 3 - 1);
            CalculateBest_AccountForOverlaps(SingleRiverWeight);

            return float.MinValue;
        }

        public override void CreateBranches()
        {
            Branches = new List<Node>(Card.N - 4);
            BranchesByIndex = new List<Node>(Card.N);

            for (int river = 0; river < Card.N; river++)
            {
                Node NewBranch;
                if (!MyFlop.Contains(river) && river != MyTurn)
                {
                    NewBranch = new RiverNode(this, MyFlop, MyTurn, river);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Contains(MyTurn);
        }

        public override bool Collision(Pocket p)
        {
            return p.Contains(MyTurn) || p.Overlaps(MyFlop);
        }

        public override string ToString()
        {
            return string.Format("({0}) {1}", MyFlop.ToString(), Card.ToString(Card.DefaultStyle, MyTurn));
        }
    }
}
