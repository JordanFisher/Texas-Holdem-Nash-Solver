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

    class CommunityRoot : CommunityNode
    {
        public CommunityRoot()
            : base()
        {
            Phase = BettingPhase.PreFlop;
            InitiallyActivePlayer = Player.Button;

            CreateBranches();
        }

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Flop.N);

            foreach (Flop flop in Flop.Flops)
                Branches.Add(new FlopCommunity(flop));
            BranchesByIndex = Branches;
        }
    }

    class FlopCommunity : CommunityNode
    {
        public Flop MyFlop;

        public FlopCommunity(Flop flop)
            : base()
        {
            MyFlop = flop;

            Weight = 1f / Counting.Choose(Card.N - 4, 3);
            Phase = BettingPhase.Flop;
            InitiallyActivePlayer = Player.Dealer;

            CreateBranches();
        }

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Card.N - 3);
            BranchesByIndex = new List<CommunityNode>(Card.N);

            for (int turn = 0; turn < Card.N; turn++)
            {
                CommunityNode NewBranch;
                if (!Contains(turn))
                {
                    NewBranch = new TurnCommunity(MyFlop, turn);
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

        public override string ToString()
        {
            return Card.CommunityToString(MyFlop);
        }
    }

    class TurnCommunity : CommunityNode
    {
        public Flop MyFlop;
        public int MyTurn;

        public TurnCommunity(Flop flop, int turn)
            : base()
        {
            MyFlop = flop;
            MyTurn = turn;

            Weight = 1f / (Card.N - 4 - 3);
            Phase = BettingPhase.Turn;
            InitiallyActivePlayer = Player.Dealer;

            CreateBranches();
        }

        protected override void CreateBranches()
        {
            Branches = new List<CommunityNode>(Card.N - 4);
            BranchesByIndex = new List<CommunityNode>(Card.N);

            for (int river = 0; river < Card.N; river++)
            {
                CommunityNode NewBranch;
                if (!Contains(river))
                {
                    NewBranch = new RiverCommunity(MyFlop, MyTurn, river);
                    Branches.Add(NewBranch);
                }
                else
                    NewBranch = null;
                BranchesByIndex.Add(NewBranch);
            }
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Contains(MyTurn);
        }

        public override bool Collision(Pocket p)
        {
            return p.Contains(MyTurn) || p.Overlaps(MyFlop);
        }

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card) || card == MyTurn;
        }

        public override string ToString()
        {
            return Card.CommunityToString(MyFlop, MyTurn);
        }
    }

    class RiverCommunity : CommunityNode
    {
        public Flop MyFlop;
        public int MyTurn, MyRiver;

        public uint[] PocketValue;
        public int[] SortedPockets;
        public uint[] SortedPocketValue;

        public static double SummedChance;
        public static double[] SummedChance_OneCardFixed = new double[Card.N];

        public static double TotalChance;
        public static double[] TotalChance_OneCardFixed = new double[Card.N];
        public static double TotalMass;
        public static double[] TotalMass_OneCardFixed = new double[Card.N];

        public static void ResetSummed()
        {
            SummedChance = 0;
            for (int i = 0; i < Card.N; i++)
                SummedChance_OneCardFixed[i] = 0;
        }
        public static void ResetTotal()
        {
            TotalChance = TotalMass = 0;
            for (int i = 0; i < Card.N; i++)
                TotalMass_OneCardFixed[i] =
                TotalChance_OneCardFixed[i] = 0;
        }
        public static void ResetMass()
        {
            TotalMass = 0;
            for (int i = 0; i < Card.N; i++)
                TotalMass_OneCardFixed[i] = 0;
        }

        public static void ProbabilityPrecomputation(PocketData pdf)
        {
            ResetMass();

            double mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(pdf[p])) continue;

                mass = pdf[p];
                TotalMass += mass;

                pocket = Pocket.Pockets[p];
                int c1 = pocket.Cards[0], c2 = pocket.Cards[1];
                TotalMass_OneCardFixed[c1] += mass;
                TotalMass_OneCardFixed[c2] += mass;
            }
        }

        public static double MassAfterExclusion(PocketData pdf, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            double Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            return Mass;
        }

        public static void ChanceToActPrecomputation(PocketData pdf, PocketData chance)
        {
            ResetTotal();

            double val, mass;
            Pocket pocket;
            for (int p = 0; p < Pocket.N; p++)
            {
                if (double.IsNaN(pdf[p])) continue;

                mass = pdf[p];
                val = mass * chance[p];
                TotalChance += val;
                TotalMass += mass;

                pocket = Pocket.Pockets[p];
                int c1 = pocket.Cards[0], c2 = pocket.Cards[1];
                TotalChance_OneCardFixed[c1] += val;
                TotalChance_OneCardFixed[c2] += val;
                TotalMass_OneCardFixed[c1] += mass;
                TotalMass_OneCardFixed[c2] += mass;
            }
        }

        public static double ChanceToActWithExclusion(PocketData pdf, PocketData chance, int ExcludedPocketIndex)
        {
            var pocket = Pocket.Pockets[ExcludedPocketIndex];
            int c1 = pocket.Cards[0], c2 = pocket.Cards[1];

            double Chance = 
            TotalChance -
                TotalChance_OneCardFixed[c1] -
                TotalChance_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex] * chance[ExcludedPocketIndex];

            double Mass =
            TotalMass -
                TotalMass_OneCardFixed[c1] -
                TotalMass_OneCardFixed[c2] +
                    pdf[ExcludedPocketIndex];

            return Chance / Mass;
        }

        public RiverCommunity(Flop flop, int turn, int river)
            : base()
        {
            MyFlop = flop;
            MyTurn = turn;
            MyRiver = river;

            Weight = 1f / (Card.N - 4 - 3 - 1);
            Phase = BettingPhase.River;
            InitiallyActivePlayer = Player.Dealer;

            // Get all pocket values
            PocketValue = new uint[Pocket.N];
            for (int p = 0; p < Pocket.N; p++)
            {
                if (Collision(p))
                    PocketValue[p] = 0;//uint.MaxValue;
                else
                    PocketValue[p] = Value.Eval(MyFlop, MyTurn, MyRiver, Pocket.Pockets[p]);
            }

            // Sort pockets
            SortedPockets = new int[Pocket.N];
            for (int i = 0; i < Pocket.N; i++) SortedPockets[i] = i;

            SortedPocketValue = new uint[Pocket.N];
            PocketValue.CopyTo(SortedPocketValue, 0);

            Array.Sort(SortedPocketValue, SortedPockets);
            Tools.Nothing();
        }

        public override bool NewCollision(Pocket p)
        {
            return p.Contains(MyRiver);
        }

        public override bool Collision(Pocket p)
        {
            return p.Contains(MyRiver) || p.Contains(MyTurn) || p.Overlaps(MyFlop);
        }

        public override bool Contains(int card)
        {
            return MyFlop.Contains(card) || card == MyTurn || card == MyRiver;
        }

        public override string ToString()
        {
            return Card.CommunityToString(MyFlop, MyTurn, MyRiver);
        }
    }
}