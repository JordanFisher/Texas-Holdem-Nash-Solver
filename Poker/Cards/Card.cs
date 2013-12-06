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

    struct Card
    {
		// Print styles for cards
        public enum OutputStyle 
		{ 
			Text,		// Used to communicate with the Hand Evaluator
			Pretty,		// A nicely formatted style
			Number,		// Strictly numeric
		};

        public static OutputStyle DefaultStyle = OutputStyle.Number;

		public const int Vals = Setup.Vals;
		public const int Suits = Setup.Flushes ? Setup.Suits : 1;

		/// <summary>
		/// This shifts the values of all cards, so that the highest possible card is an Ace.
		/// This is useful for Royal Poker, turning a 6 into an Ace.
		/// </summary>
		const bool Royal = false;
		const int ValOffset = Royal ? 13 - Vals : 0;

        public const int N = Vals * Suits;

        static char[] SuitChar = { '\u2660', '\u2665', '\u2666', '\u2663' };
        const ConsoleColor Black = ConsoleColor.Gray, Red = ConsoleColor.Red;
        static ConsoleColor[] Color = { Black, Red, Red, Black };
		static string[] ValChar = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };

        static char[] _SuitChar = { 's', 'h', 'd', 'c' };
		static string[] _ValChar = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "j", "q", "k", "a" };

        public int Value, Suit, Number;

        public static int GetSuit(int c)
        {
            return c / Vals;
        }

        public Card(int x)
        {
            Number = x;
            Suit = x / Vals;
            Value = x - Suit * Vals;
        }

        public Card(int Value, int Suit)
        {
            this.Value = Value;
            this.Suit = Suit;

            Number = Value + Suit * Vals;
        }

        public int CalculateNumber()
        {
            Number = Value + Suit * Vals;
            return Number;
        }

        public static string CommunityToString(Flop flop, int turn = -1, int river = -1)
        {
            string str = "";
            
            str += string.Format("({0})", flop);

            if (turn >= 0) str += string.Format(" {0}", ToString(OutputStyle.Number, turn));
            else return str;

            if (river >= 0) str += string.Format(" {0}", ToString(OutputStyle.Number, river));
            else return str;

            return str;
        }

        public static string CommunityToString(params int[] Indices)
        {
            string str = "";
            if (Indices.Length > 0) str += Flop.Flops[Indices[0]];
            if (Indices.Length > 1) str += " " + ToString(OutputStyle.Number, Indices[1]);
            if (Indices.Length > 2) str += " " + ToString(OutputStyle.Number, Indices[2]);

            return str;
        }

        public static void PrintCards(params int[] Cards) { PrintArray(Cards); }
        public static void PrintArray(int[] Cards)
        {
            for (int i = 0; i < Cards.Length; i++)
            {
                if (i > 0) Console.Write(" ");
                new Card(Cards[i]).Print();
            }
        }

        public static string ToString(OutputStyle Style, params int[] Cards)
        {
            return ToString(Cards, Style);
        }
        public static string ToString(int[] Cards, OutputStyle Style, int StartIndex = 0, int EndIndex = -1)
        {
            if (EndIndex < 0) EndIndex = Cards.Length;

            string s = "";
            for (int i = StartIndex; i < EndIndex; i++)
            {
                if (i > 0) s += " ";
                s += new Card(Cards[i]).ToString(Style);
            }

            return s;
        }

		public static int SetSuit(int c, int suit)
		{
			var card = new Card(c);
			card.Suit = suit;
			return card.CalculateNumber();
		}


        public static bool ColorCardNum = true;
        public static bool ColorCards = true;
        public void Print()
        {
            if (ColorCards)
            {
                if (ColorCardNum)
                {
                    Console.ForegroundColor = Color[Suit];
					Console.Write(ValChar[Value + ValOffset]);
                    Console.Write(SuitChar[Suit]);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
					Console.Write(ValChar[Value + ValOffset]);
                    Console.ForegroundColor = Color[Suit];
                    Console.Write(SuitChar[Suit]);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
            }
            else
            {
				Console.Write(ValChar[Value + ValOffset]);
                Console.Write(SuitChar[Suit]);
            }
        }

        public override string ToString()
        {
            return ToString(OutputStyle.Pretty);
        }

        public string ToString(OutputStyle Style)
        {
            switch (Style)
            {
                case OutputStyle.Pretty:
                    return ValChar[Value + ValOffset] + SuitChar[Suit];
                case OutputStyle.Text:
					return _ValChar[Value + ValOffset] + _SuitChar[Suit];
                case OutputStyle.Number:
                    return Number.ToString();
                default:
                    return null;
            }
        }
    }
}