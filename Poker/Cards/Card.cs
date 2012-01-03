using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    struct Card
    {
        public enum OutputStyle { Text, Pretty, Number };
        public static OutputStyle DefaultStyle = OutputStyle.Number;

        //public const int Vals = 13;
        //public const int Suits = 4;

        //public const int Vals = 5;
        //public const int Suits = 4;

        public const int Vals = 13;
        public const int Suits = 1;

        public const int N = Vals * Suits;

        //static char[] SuitChar = { '\u2660', '\u2661', '\u2662', '\u2663' };
        static char[] SuitChar = { '\u2660', '\u2665', '\u2666', '\u2663' };
        const ConsoleColor Black = ConsoleColor.Gray, Red = ConsoleColor.Red;
        static ConsoleColor[] Color = { Black, Red, Red, Black };
        static string[] ValChar = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        static char[] _SuitChar = { 's', 'h', 'd', 'c' };
        static string[] _ValChar = { "a", "2", "3", "4", "5", "6", "7", "8", "9", "10", "j", "q", "k" };

        public int Value, Suit, Number;

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

        public static string CommunityToString(params int[] Indices)
        {
            string str = "";
            if (Indices.Length > 0) str += Flop.Flops[Indices[0]];
            if (Indices.Length > 1) str += " " + ToString(OutputStyle.Number, Indices[1]);
            if (Indices.Length > 2) str += " " + ToString(OutputStyle.Number, Indices[2]);

            return str;
        }

        public static void Print(int[] Cards)
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
        public static string ToString(int[] Cards, OutputStyle Style)
        {
            string s = "";
            for (int i = 0; i < Cards.Length; i++)
            {
                if (i > 0) s += " ";
                s += new Card(Cards[i]).ToString(Style);
            }

            return s;
        }

        public void Print()
        {
            Console.Write(ValChar[Value]);
            Console.ForegroundColor = Color[Suit];
            Console.Write(SuitChar[Suit]);
            Console.ForegroundColor = ConsoleColor.Gray;
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
                    return ValChar[Value] + SuitChar[Suit];
                case OutputStyle.Text:
                    return _ValChar[Value] + _SuitChar[Suit];
                case OutputStyle.Number:
                    return Number.ToString();
                default:
                    return null;
            }
        }
    }
}