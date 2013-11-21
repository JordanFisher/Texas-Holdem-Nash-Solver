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

    public enum PlayerAction { Nothing, Raise, Call, Fold };
    class PlayerImplementation
    {
        public Game MyGame;
		public string Name;

        public PlayerImplementation()
        {
        }

		public virtual void SetName(int PlayerNumber, PlayerImplementation Opponent)
		{
			Name = string.Format("Player {0}", PlayerNumber);
		}

        public virtual PlayerAction GetAction() { return PlayerAction.Nothing; }
        public virtual void OpponentDoes(PlayerAction action) { }

        public virtual void Reset() { }
        
        public virtual void SetPocket(int c1, int c2) { }
        public virtual void SetFlop(int c1, int c2, int c3) { }
        public virtual void SetTurn(int c1) { }
        public virtual void SetRiver(int c1) { }
    }
}
