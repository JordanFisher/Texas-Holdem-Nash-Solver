using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class HumanPlayer : PlayerImplementation
    {
        public static bool EnterToConfirm = true;
        public override PlayerAction GetAction()
        {
            if (BetNode.SimultaneousBetting)
                return GetAction_Simultaneous();
            else
                return GetAction_Complex();
        }

        PlayerAction GetAction_Simultaneous()
        {
            while (true)
            {
                Console.Write("Your pocket: ");
                Card.PrintCards(c1, c2);
                Console.WriteLine(". Pot is {0}. Next raise is {1}.", MyGame.Pot, MyGame.NextRaiseAmount);

                Console.WriteLine("(R)aise or (F)old?");

                var action = GetInputAction();
                if (action == PlayerAction.Raise || action == PlayerAction.Fold) return action;

                Console.WriteLine("\nInvalid input.");
            }
        }

        PlayerAction GetAction_Complex()
        {
            while (true)
            {
                Console.Write("Your pocket: ");
                Card.PrintCards(c1, c2);
                Console.WriteLine(". Current bet is {0}. You're in for {1}.", MyGame.Pot, MyGame.ActiveSpent);

                if (MyGame.SpentA != MyGame.SpentB)
                {
                    if (MyGame.NumRaises < BetNode.AllowedRaises)
                    {
                        Console.WriteLine("(R)aise for {0}, (C)all, (F)old?", MyGame.NextRaiseAmount);

                        var action = GetInputAction();
                        if (action == PlayerAction.Raise || action == PlayerAction.Call || action == PlayerAction.Fold) return action;
                    }
                    else
                    {
                        Console.WriteLine("(C)all or (F)old?");

                        var action = GetInputAction();
                        if (action == PlayerAction.Call || action == PlayerAction.Fold) return action;
                    }
                }
                else
                {
                    if (MyGame.NumRaises < BetNode.AllowedRaises)
                    {
                        Console.WriteLine("(R)aise for {0} or (C)all?", MyGame.NextRaiseAmount);

                        var action = GetInputAction();
                        if (action == PlayerAction.Raise || action == PlayerAction.Call) return action;
                    }
                    else
                    {
                        Assert.NotReached();
                    }
                }

                Console.WriteLine("\nInvalid input.");
            }
        }

        PlayerAction GetInputAction()
        {
            char Input;
            if (EnterToConfirm)
            {
                var line = Console.ReadLine();
                if (line.Length == 0) Input = ' ';
                else Input = line[0];
            }
            else
                Input = (char)Console.ReadKey().KeyChar;

            if (Input == 'R' || Input == 'r') return PlayerAction.Raise;
            if (Input == 'F' || Input == 'f') return PlayerAction.Fold;
            if (Input == 'C' || Input == 'c') return PlayerAction.Call;
            return PlayerAction.Nothing;
        }

        int c1, c2;
        public override void SetPocket(int c1, int c2)
        {
            this.c1 = c1;
            this.c2 = c2;

            if (MyGame.Output)
                if (!BetNode.SimultaneousBetting)
                {
                    if (MyGame.PlayerA == this)
                        Console.WriteLine("You are the button!");
                    else if (MyGame.PlayerB == this)
                        Console.WriteLine("You are the dealer!");
                    else
                        Assert.NotReached();
                }
        }
    }
}
