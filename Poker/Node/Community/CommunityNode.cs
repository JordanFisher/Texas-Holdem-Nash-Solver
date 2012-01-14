using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class CommunityNode
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public static CommunityRoot Root;

        public List<CommunityNode> Branches;
        public List<CommunityNode> BranchesByIndex;

        public double Weight;
        public BettingPhase Phase = BettingPhase.NotSet;
        public Player InitiallyActivePlayer = Player.Undefined;

        public CommunityNode()
        {
#if DEBUG
            InstanceCount++;
            //Console.WriteLine(this);
#endif
        }

        protected virtual void CreateBranches()
        {
        }

        public virtual bool NewCollision(Pocket p)
        {
            return false;
        }
        public bool NewCollision(int p) { return NewCollision(Pocket.Pockets[p]); }

        public virtual bool Collision(Pocket p)
        {
            return false;
        }
        public bool Collision(int p) { return Collision(Pocket.Pockets[p]); }

        public virtual bool Contains(int card)
        {
            return false;
        }
    }
}
