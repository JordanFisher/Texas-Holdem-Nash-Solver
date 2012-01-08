using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    /*
    class CommunityNode
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        protected List<CommunityNode> Branches;

        public CommunityNode()
            : base()
        {
#if DEBUG
            InstanceCount++;
            //Console.WriteLine(this);
#endif
        }

        protected virtual void CreateBranches()
        {
            Branches = new List<CommunityNode>(Flop.N);

            foreach (Flop flop in Flop.Flops)
                Branches.Add(new FlopCommunity());
        }

        public virtual bool NewCollision(Pocket p)
        {
            return false;
        }

        public virtual bool Collision(Pocket p)
        {
            return false;
        }

        public virtual bool Contains(int card)
        {
            return false;
        }
    }

    class FlopCommunity : CommunityNode
    {
        public Flop MyFlop;

        public FlopCommunity()
            : base()
        {
        }

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Card.N - 3);

            for (int turn = 0; turn < Card.N; turn++)
            {
                if (!Contains(turn))
                    Branches.Add(new );
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

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card);
        }
    }

    class TurnCommunity : CommunityNode
    {
        public Flop MyFlop;
        public int MyTurn;

        public TurnCommunity()
            : base()
        {
        }

        protected override void CreateBranches()
        {
            Branches = new List<Node>(Card.N - 4);

            for (int river = 0; river < Card.N; river++)
            {
                Node NewBranch;
                Assert.That(MyPhaseRoot is TurnRoot);
                if (!MyPhaseRoot.Contains(river))
                //if (!MyFlop.Contains(river) && river != MyTurn)
                {
                    NewBranch = new RiverRoot(this, river, Spent, Pot);
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

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card);
        }
    }
     * */
}
