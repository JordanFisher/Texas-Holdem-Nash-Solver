﻿using System;
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

    class Game
    {
		static void GamePrint(string format = "", params object[] args)
		{
			Console.WriteLine(format, args);
			PrintDelay();
		}

		private static void PrintDelay()
		{
			System.Threading.Thread.Sleep(350);
		}

        public static int[,] PocketLookup;
        public static void InitPocketLookup()
        {
            PocketLookup = new int[Card.N, Card.N];
            
#if DEBUG
            for (int i = 0; i < Card.N; i++)
            for (int j = 0; j < Card.N; j++)
                PocketLookup[i, j] = -1;
#endif
            for (int p = 0; p < Pocket.N; p++)
            {
                Pocket pocket = Pocket.Pockets[p];
                PocketLookup[pocket.Cards[0], pocket.Cards[1]] = p;
                PocketLookup[pocket.Cards[1], pocket.Cards[0]] = p;
            }
#if DEBUG
            for (int i = 0; i < Card.N; i++)
            for (int j = 0; j < Card.N; j++)
                Assert.That(PocketLookup[i,j] >= 0 || i == j);
#endif
        }

        public static int[,,] FlopLookup;
        public static void InitFlopLookup()
        {
            FlopLookup = new int[Card.N, Card.N, Card.N];

#if DEBUG
            for (int i = 0; i < Card.N; i++)
            for (int j = 0; j < Card.N; j++)
            for (int k = 0; k < Card.N; k++)
                FlopLookup[i, j, k] = -1;
#endif
            for (int f = 0; f < Flop.N; f++)
            {
                Flop flop = Flop.Flops[f];
                FlopLookup[flop.c1, flop.c2, flop.c3] =
                FlopLookup[flop.c2, flop.c1, flop.c3] =
                FlopLookup[flop.c3, flop.c1, flop.c2] =
                FlopLookup[flop.c1, flop.c3, flop.c2] =
                FlopLookup[flop.c2, flop.c3, flop.c1] =
                FlopLookup[flop.c3, flop.c2, flop.c1] = f;
            }
#if DEBUG
            for (int i = 0; i < Card.N; i++)
            for (int j = 0; j < Card.N; j++)
            for (int k = 0; k < Card.N; k++)
                Assert.That(FlopLookup[i, j, k] >= 0 || i == j || j == k || i == k);
#endif
        }

        public bool Output = true;

        public PlayerImplementation PlayerA, PlayerB;

        public int SpentA, SpentB;

        int[] Cards;
        int CardsOut;

        bool InPlay;

        BettingPhase Phase;

        Random Rnd = new Random();
        public number Rand() { return (number)Rnd.NextDouble(); }

        public Game(PlayerImplementation PlayerA, PlayerImplementation PlayerB, bool Output = false, int Seed = -1)
        {
            this.Output = Output;

			// Store the players
            this.PlayerA = PlayerA;
            this.PlayerB = PlayerB;
			PlayerA.MyGame = PlayerB.MyGame = this;

			// Set the names of the players
			PlayerA.SetName(1, PlayerB);
			PlayerB.SetName(2, PlayerA);

            Cards = new int[9];

            if (Seed >= 0)
                Rnd = new Random(Seed);
            else
                Rnd = new Random();
        }

        void AddCards(int NumCards)
        {
            for (int i = 0; i < NumCards; i++)
                AddCard();
        }

        int AddCard()
        {
            int NewCard = -1;
            bool Contains = true;
            while (Contains)
            {
                NewCard = Rnd.Next(Card.N);

                Contains = false;
                for (int i = 0; i < CardsOut; i++)
                    if (Cards[i] == NewCard)
                    {
                        Contains = true;
                        break;
                    }
            }

            Cards[CardsOut] = NewCard;
            CardsOut++;

            Assert.That(NewCard >= 0 && NewCard < Card.N);
            return NewCard;
        }

        Player ActivePlayerPosition;
        PlayerImplementation ActivePlayer
        {
            get
            {
                if (ActivePlayerPosition == Player.Button)
                    return PlayerA;
                else if (ActivePlayerPosition == Player.Dealer)
                    return PlayerB;
                else
                {
                    Assert.NotReached();
                    return null;
                }
            }
        }

        public int ActiveSpent
        {
            get
            {
                if (ActivePlayerPosition == Player.Button)
                    return SpentA;
                else
                    return SpentB;
            }
            set
            {
                if (ActivePlayerPosition == Player.Button)
                    SpentA = value;
                else
                    SpentB = value;
            }
        }

        public int Pot
        {
            get { return Math.Max(SpentA, SpentB); }
        }

        public int NextRaiseAmount;
        void DoBetting_Simultaneous(int RaiseAmount)
        {
            NextRaiseAmount = RaiseAmount;
            PlayerAction A_Does = PlayerA.GetAction();
            PlayerAction B_Does = PlayerB.GetAction();

            Assert.That(A_Does == PlayerAction.Raise || A_Does == PlayerAction.Fold);
            Assert.That(B_Does == PlayerAction.Raise || B_Does == PlayerAction.Fold);

            if (A_Does == PlayerAction.Raise)
            {
                if (B_Does == PlayerAction.Raise)
                {
                    SpentA += RaiseAmount;
                    SpentB += RaiseAmount;

                    if (Output)
                        GamePrint("Both players raised!");
                }
                else
                {
                    Outcome = SpentB;
                    InPlay = false;

                    if (Output)
                        GamePrint("{0} folds. {1} wins the hand!", PlayerB.Name, PlayerA.Name);
                }
            }
            else
            {
                if (B_Does == PlayerAction.Raise)
                {
                    Outcome = -SpentA;
                    InPlay = false;

                    if (Output)
                        GamePrint("{0} folds. {1} wins the hand!", PlayerA.Name, PlayerB.Name);
                }
                else
                {
                    Outcome = 0;
                    InPlay = false;

                    if (Output)
                        GamePrint("Both players fold. Hand is a draw!");
                }
            }
        }

        public int NumRaises;
        void DoBetting_Complex(int RaiseAmount)
        {
            NextRaiseAmount = RaiseAmount;
            bool CallEndsRound = false;

            NumRaises = 0;
            bool MoreBetting = true;
            while (MoreBetting)
            {
                PlayerAction action = ActivePlayer.GetAction();

                if (SpentA != SpentB)
                {
                    if (NumRaises < BetNode.AllowedRaises)
                    {
                        Assert.That(action == PlayerAction.Raise ||
                                    action == PlayerAction.Call ||
                                    action == PlayerAction.Fold);

                        if (action == PlayerAction.Raise)
                        {
                            ActiveSpent = Pot + RaiseAmount;
                            NumRaises++;

							if (Output) GamePrint("The {0} raises by {1}.", ActivePlayersQualifiedName(), RaiseAmount);
                        }
                        else if (action == PlayerAction.Call)
                        {
                            ActiveSpent = Pot;
                            Assert.That(SpentA == SpentB);

							if (Output) GamePrint("The {0} calls.", ActivePlayersQualifiedName());
                            if (CallEndsRound) MoreBetting = false;
                        }
                        else
                        {
                            if (ActivePlayerPosition == Player.Button)
                                Outcome -= SpentA;
                            else
                                Outcome += SpentB;
                            InPlay = false;

							if (Output) GamePrint("The {0} folds.", ActivePlayersQualifiedName());
                            MoreBetting = false;
                        }
                    }
                    else
                    {
                        Assert.That(action == PlayerAction.Call ||
                                    action == PlayerAction.Fold);

                        if (action == PlayerAction.Call)
                        {
                            ActiveSpent = Pot;
                            Assert.That(SpentA == SpentB);

							if (Output) GamePrint("The {0} calls.", ActivePlayersQualifiedName());
                            if (CallEndsRound) MoreBetting = false;
                        }
                        else
                        {
                            if (ActivePlayerPosition == Player.Button)
                                Outcome -= SpentA;
                            else
                                Outcome += SpentB;
                            InPlay = false;

							if (Output) GamePrint("The {0} folds.", ActivePlayersQualifiedName());
                            MoreBetting = false;
                        }
                    }
                }
                else
                {
                    if (NumRaises < BetNode.AllowedRaises)
                    {
                        Assert.That(action == PlayerAction.Raise ||
                                    action == PlayerAction.Call);

                        if (action == PlayerAction.Raise)
                        {
                            ActiveSpent = Pot + RaiseAmount;
                            NumRaises++;

							if (Output) GamePrint("The {0} raises by {1}.", ActivePlayersQualifiedName(), RaiseAmount);
                        }
                        else if (action == PlayerAction.Call)
                        {
                            ActiveSpent = Pot;
                            Assert.That(SpentA == SpentB);

							if (Output) GamePrint("The {0} calls.", ActivePlayersQualifiedName());
                            if (CallEndsRound) MoreBetting = false;
                        }
                    }
                    else
                    {
                        Assert.NotReached();
                    }
                }

                ActivePlayerPosition = Tools.NextPlayer(ActivePlayerPosition);
                ActivePlayer.OpponentDoes(action);
                CallEndsRound = true;
            }              
        }

		private string ActivePlayersQualifiedName()
		{
			return Tools.QualifiedName(ActivePlayerPosition, ActivePlayer);
		}

        void DoPockets()
        {
            Phase = BettingPhase.PreFlop;
            AddCards(4);

            if (Output) GamePrint("\nBeginning of round.\n");

            PlayerA.SetPocket(Cards[0], Cards[1]);
            PlayerB.SetPocket(Cards[2], Cards[3]);

            if (BetNode.SimultaneousBetting)
                DoBetting_Simultaneous(Setup.PreFlop);
            else
            {
                ActivePlayerPosition = Player.Button;
                SpentA = Setup.LittleBlind;
                SpentB = Setup.BigBlind;
                DoBetting_Complex(Setup.RaiseAmount);
            }
        }

        void DoFlop()
        {
            Phase = BettingPhase.Flop;
            AddCards(3);

            if (Output)
            {
                Console.Write("\nFlop:  ");
                Card.PrintCards(Cards[4], Cards[5], Cards[6]);
                GamePrint();
            }

            PlayerA.SetFlop(Cards[4], Cards[5], Cards[6]);
            PlayerB.SetFlop(Cards[4], Cards[5], Cards[6]);

            if (BetNode.SimultaneousBetting)
                DoBetting_Simultaneous(Setup.Flop);
            else
            {
                ActivePlayerPosition = Player.Dealer;
                DoBetting_Complex(Setup.RaiseAmount);
            }
        }

        void DoTurn()
        {
            Phase = BettingPhase.Turn;
            AddCards(1);

            if (Output)
            {
                Console.Write("\nTurn:  ");
                Card.PrintCards(Cards[4], Cards[5], Cards[6], Cards[7]);
                GamePrint();
            }

            PlayerA.SetTurn(Cards[7]);
            PlayerB.SetTurn(Cards[7]);

            if (BetNode.SimultaneousBetting)
                DoBetting_Simultaneous(Setup.Turn);
            else
            {
                ActivePlayerPosition = Player.Dealer;
                DoBetting_Complex(Setup.RaiseAmount);
            }
        }

        void DoRiver()
        {
            Phase = BettingPhase.River;
            AddCards(1);

            if (Output)
            {
                Console.Write("\nRiver: ");
                Card.PrintCards(Cards[4], Cards[5], Cards[6], Cards[7], Cards[8]);
                GamePrint();
            }

            PlayerA.SetRiver(Cards[8]);
            PlayerB.SetRiver(Cards[8]);

            if (BetNode.SimultaneousBetting)
                DoBetting_Simultaneous(Setup.River);
            else
            {
                ActivePlayerPosition = Player.Dealer;
                DoBetting_Complex(Setup.RaiseAmount);
            }
        }

        void DoShowdown()
        {
            Hand BestHand1 = Value.GetHand(Cards[4], Cards[5], Cards[6], Cards[7], Cards[8], Cards[0], Cards[1]);
            Hand BestHand2 = Value.GetHand(Cards[4], Cards[5], Cards[6], Cards[7], Cards[8], Cards[2], Cards[3]);

            if (Output)
            {
                GamePrint("\nShowdown:");

                Console.Write("{0}: ", PlayerA.Name);
                Card.PrintCards(Cards[0], Cards[1]);
                Console.Write(" -> {0}\n", BestHand1.Description);
				PrintDelay();

                Console.Write("{0}: ", PlayerB.Name);
                Card.PrintCards(Cards[2], Cards[3]);
                Console.Write(" -> {0}\n", BestHand2.Description);
				PrintDelay();
            }

            uint Value1 = BestHand1.HandValue;
            uint Value2 = BestHand2.HandValue;

            if (Value1 > Value2)
            {
                if (Output) GamePrint("{0} wins!\n", PlayerA.Name);
                Outcome = SpentB;
            }
            else if (Value2 > Value1)
            {
                if (Output) GamePrint("{0} wins!\n", PlayerB.Name);
                Outcome = -SpentA;
            }
            else
            {
                if (Output) GamePrint("Draw!\n");
                Outcome = 0;
            }
        }

        public string Community()
        {
            if (CardsOut < 5)
                return "";
            else
                return Card.ToString(Cards, Card.OutputStyle.Pretty, 4, CardsOut);
        }

        public int RoundNumber;
		public double Round(int NumberOfRounds)
        {
            for (int i = 0; i < NumberOfRounds; i++)
            {
                // Do one round, swapping player roles if necessary
                if (!BetNode.SimultaneousBetting && RoundNumber % 2 == 1) SwapPlayers();
                double outcome = (double)Round();
                if (!BetNode.SimultaneousBetting && RoundNumber % 2 == 1) SwapPlayers();

                // Update totals and averages
                if (BetNode.SimultaneousBetting)
                    Total += outcome;
                else
                {
                    if (RoundNumber % 2 == 0)
                        Total_1vs2 += outcome;
                    else
                        Total_2vs1 -= outcome;
                    Total = Total_1vs2 + Total_2vs1;
                }

                RoundNumber++;
                Average = Total / RoundNumber;
                Average_1vs2 = Total_1vs2 / (RoundNumber / 2.0);
				Average_2vs1 = Total_2vs1 / (RoundNumber / 2.0);

                // Display averages
                if (i % 1000000 == 0 && i > 0)
                {
                    if (BetNode.SimultaneousBetting)
                        Console.WriteLine("{0} -> {1}", i, Average);
                    else
						Console.WriteLine("{0} -> {1}, {2} -> {3}", i, Average_1vs2, Average_2vs1, Average);
                }

				// Display chips
				if (Output)
				{
					PrintDelay();
					GamePrint("");
					GamePrint("{0} has {1} chips", PlayerA.Name, Total);
					GamePrint("{0} has {1} chips", PlayerB.Name, -Total);
					
					PrintDelay();
					PrintDelay();
					PrintDelay();
					PrintDelay();
					PrintDelay();
					PrintDelay();
					GamePrint("\n\n");
					PrintDelay();
				}
            }

            return Average;
        }

        public double Total_1vs2 = 0, Total_2vs1 = 0, Total = 0;
		public double Average_1vs2 = 0, Average_2vs1 = 0, Average = 0;
        void SwapPlayers()
        {
            PlayerImplementation Hold = PlayerA;
            PlayerA = PlayerB;
            PlayerB = Hold;
        }

        number Outcome;
        number Round()
        {
            PlayerA.Reset();
            PlayerB.Reset();
            
            SpentA = SpentB = Setup.PreDeal;

            CardsOut = 0;
            Outcome = 0;
            InPlay = true;

            DoPockets(); if (!InPlay) return Outcome;
            DoFlop();    if (!InPlay) return Outcome;
            DoTurn();    if (!InPlay) return Outcome;
            DoRiver();   if (!InPlay) return Outcome;
            DoShowdown();

            return Outcome;
        }
    }
}
