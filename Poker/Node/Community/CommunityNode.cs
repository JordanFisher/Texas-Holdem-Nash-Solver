using System;
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

    class CommunityNode
    {
#if DEBUG
        public static int InstanceCount = 0;
#endif

        public static CommunityRoot Root;

        public List<CommunityNode> Branches;
        public List<CommunityNode> BranchesByIndex;

        public number Weight;
        public BettingPhase Phase = BettingPhase.NotSet;
        public Player InitiallyActivePlayer = Player.Undefined;

        public bool[] AvailablePocket, AvailableCard;

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

        protected void ClassifyAvailability()
        {
            // Classify pockets as colliding or not colliding with community cards
            AvailablePocket = new bool[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p))
                    AvailablePocket[p] = false;
                else
                    AvailablePocket[p] = true;
            }

            // Classify cards as colliding or not colliding with community cards
            AvailableCard = new bool[Card.N];
            for (int c = 0; c < Card.N; c++)
            {
                if (Contains(c))
                    AvailableCard[c] = false;
                else
                    AvailableCard[c] = true;
            }
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
