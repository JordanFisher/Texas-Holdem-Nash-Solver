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

        public RiverNode(Node parent, Flop flop, int turn, int river)
            : base(parent)
        {
            MyFlop = flop;
            MyTurn = turn;
            MyRiver = river;

            Initialize();
            Spent = Pot = Ante.PreDeal + Ante.PreFlop + Ante.Flop + Ante.Turn;

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
        public override float CalculateBest()
        {
            // Ignore pockets that collide with community
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p)) { S[p] = PostRaiseP[p] = EV[p] = B[p] = float.NaN; continue; }
            }

            // For each pocket we might have, calculate what we should do.
            PocketData UpdatedP = new PocketData();
            int ShowdownPot = Pot + Ante.River;
            uint PocketValue1, PocketValue2;
            for (int p1 = 0; p1 < Pocket.N; p1++)
            {
                if (float.IsNaN(PostRaiseP[p1])) continue;
                PocketValue1 = PocketValue[p1];
                Assert.That(PocketValue1 < uint.MaxValue);

                // Update the opponent's pocket PDF using the new information,
                // (which is that we now know which pocket we have).
                UpdateOnExclusion(PostRaiseP, UpdatedP, p1);

                // Calculate the EV assuming we both raise.
                float ShowdownEV = 0;
                for (int p2 = 0; p2 < Pocket.N; p2++)
                {
                    if (float.IsNaN(UpdatedP[p2])) continue;
                    PocketValue2 = PocketValue[p2];
                    Assert.That(PocketValue2 < uint.MaxValue);

                    if (PocketValue1 == PocketValue2) continue;
                    else if (PocketValue1 > PocketValue2)
                        ShowdownEV += UpdatedP[p2] * ShowdownPot;
                    else
                        ShowdownEV -= UpdatedP[p2] * ShowdownPot;
                }

                // Calculate the chance the opponent will raise/fold
                float RaiseChance = TotalChance(PreRaiseP, S, p1);
                float FoldChance = 1 - RaiseChance;
                Assert.IsNum(RaiseChance);

                // Calculate EV for raising and folding.
                float RaiseEV = FoldChance * Pot + RaiseChance * ShowdownEV;
                float FoldEV = RaiseChance * (-Spent);

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

            return float.MinValue;
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

        protected override float Simulate(int p1, int p2, ref int[] BranchIndex, int IndexOffset)
        {
            //return 0;

            int ShowdownPot = Pot + Ante.River;

            uint PocketValue1 = PocketValue[p1];
            uint PocketValue2 = PocketValue[p2];

            float ShowdownEV;
            if (PocketValue1 == PocketValue2) ShowdownEV = 0;
            else if (PocketValue1 > PocketValue2)
                ShowdownEV = ShowdownPot;
            else
                ShowdownEV = -ShowdownPot;

            float EV =
                B[p1]       * (S[p2] * ShowdownEV + (1 - S[p2]) * Pot) +
                (1 - B[p1]) * (S[p2] * (-Spent) + (1 - S[p2]) * 0);

            return EV;
        }
    }
}