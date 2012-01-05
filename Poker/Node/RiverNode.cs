using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using HoldemHand;

namespace Poker
{
    class RiverNode : Node
    {
        public Flop MyFlop;
        public int MyTurn, MyRiver;

        public uint[] PocketValue;

        public RiverNode(Node parent, Flop flop, int turn, int river, int Spent, int Pot)
            : base(parent, Spent, Pot)
        {
            MyFlop = flop;
            MyTurn = turn;
            MyRiver = river;

            Initialize();
            //Spent = Pot = Ante.PreDeal + Ante.PreFlop + Ante.Flop + Ante.Turn;

            PocketValue = new uint[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p))
                    PocketValue[p] = uint.MaxValue;
                else
                    PocketValue[p] = Value.Eval(MyFlop, MyTurn, MyRiver, Pocket.Pockets[p]);
            }
        }

        public override void CalculatePostRaisePDF()
        {
            CalculatePostRaisePDF_AccountForOverlaps();

            base.CalculatePostRaisePDF();
        }

        public static int OpCount = 0;
        public override void CalculateBest()
        {
            // Ignore pockets that collide with community
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p)) { S[p] = PostRaiseP[p] = EV[p] = B[p] = double.NaN; continue; }
            }

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            int ShowdownPot = Pot + Ante.River;
            uint PocketValue1, PocketValue2;
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (double.IsNaN(PostRaiseP[p1])) continue;
                PocketValue1 = PocketValue[p1];
                Assert.That(PocketValue1 < uint.MaxValue);

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PostRaiseP, UpdatedP, p1);
                OpCount++;

                // Calculate the EV assuming we both raise.
                double ShowdownEV = 0;
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (double.IsNaN(UpdatedP[p2])) continue;
                    PocketValue2 = PocketValue[p2];
                    Assert.That(PocketValue2 < uint.MaxValue);

                    if (PocketValue1 == PocketValue2) continue;
                    else if (PocketValue1 > PocketValue2)
                        ShowdownEV += UpdatedP[p2] * ShowdownPot;
                    else
                        ShowdownEV -= UpdatedP[p2] * ShowdownPot;
                }

                // Calculate the chance the opponent will raise/fold
                double RaiseChance = TotalChance(PreRaiseP, S, p1);
                double FoldChance = 1 - RaiseChance;
                Assert.IsNum(RaiseChance);

                // Calculate EV for raising and folding.
                double RaiseEV = FoldChance * Pot + RaiseChance * ShowdownEV;
                double FoldEV = RaiseChance * (-Spent);

                // Decide strategy based on which action is better.
                if (RaiseEV >= FoldEV)
                //if (false)
                {
                    B[p1] = 1;
                    EV[p1] = RaiseEV;
                }
                else
                {
                    B[p1] = 0;
                    EV[p1] = FoldEV;
                }
                Assert.IsNum(EV[p1]);
            }
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Contains(MyRiver);
        }

        public override bool Collision(Pocket p)
        {
            return p.Contains(MyRiver) || p.Contains(MyTurn) || p.Overlaps(MyFlop);
        }

        public override string ToString()
        {
            return string.Format("({0}) {1}", MyFlop.ToString(), Card.ToString(Card.DefaultStyle, MyTurn, MyRiver));
        }

        public override double _Simulate(Var S1, Var S2, int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            PocketData Data1 = S1(this), Data2 = S2(this);

            int ShowdownPot = Pot + Ante.River;

            uint PocketValue1 = PocketValue[p1];
            uint PocketValue2 = PocketValue[p2];

            double ShowdownEV;
            if (PocketValue1 == PocketValue2) ShowdownEV = 0;
            else if (PocketValue1 > PocketValue2)
                ShowdownEV = ShowdownPot;
            else
                ShowdownEV = -ShowdownPot;

            double EV =
                Data1[p1]       * (Data2[p2] * ShowdownEV + (1 - Data2[p2]) * Pot) +
                (1 - Data1[p1]) * (Data2[p2] * (-Spent) + (1 - Data2[p2]) * 0);
            Assert.IsNum(EV);

            return EV;
        }
    }
}