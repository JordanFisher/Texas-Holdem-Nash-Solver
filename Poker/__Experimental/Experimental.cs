using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poker.__Experimental
{
	class Experimental
	{
		static PocketRoot root;

		static Dictionary<int, bool> HashNote = new Dictionary<int, bool>(100000);

		static double[,] Payoffs = new double[100, 100];
		static int Columns = 0;
		static void AddColumn()
		{
			root.CopyToList(Columns);

			Columns++;

			for (int i = 0; i < Columns; i++)
			{
				double payoff = Program.Simulation(n => n.PureList[i], n => n.PureList[Columns - 1]);
				Payoffs[i, Columns - 1] = -payoff;
				Payoffs[Columns - 1, i] = payoff;
			}
		}

		static string MatrixString()
		{
			string output = "";

			for (int i = 0; i < Columns; i++)
			{
				for (int j = 0; j < Columns; j++)
					output += string.Format("{0,8:F3}", Payoffs[i, j]);
				output += "\n";
			}

			return output;
		}

		public void BiHarmonicAlg(int n, number ev1, number ev2)
		{
			n++;
			number t = ((number)1) / n;
			number s = ((number)1) - t;

			number _ev1 = n - 1, _ev2 = ((number)1), _ev3 = ((number)1) * _ev2;
			//number _ev1 = 1f / ev1 + n, _ev2 = 1f / ev2, _ev3 = .5f * _ev2;

			double power = 1; //1.2f;
			_ev1 = (number)Math.Pow((double)_ev1, power);
			_ev2 = (number)Math.Pow((double)_ev2, power);
			_ev3 = (number)Math.Pow((double)_ev3, power);

			number total = _ev1 + _ev2 + _ev3;
			CombineStrats(_ev1 / total, _ev2 / total, _ev3 / total);
		}

		/* Combine 3 strategies at once
		public void CombineStrats(number t1, number t2, number t3)
		{
			for (int p = 0; p < Pocket.N; p++)
				_CombineStrats(p, t1, t2, t3);
		}
		void _CombineStrats(int p, number t1, number t2, number t3)
		{
			PocketData S1 = Hold, S2 = S, S3 = B;

			number _t1, _t2, _t3;

			if (S1 != null && S2 != null && S3 != null &&
				MyCommunity.AvailablePocket[p])
			{
				number normalize = (t1 * S1[p] + t2 * S2[p] + t3 * S3[p]);
				Assert.That(normalize != 0);
				_t1 = t1 * S1[p] / normalize;
				_t2 = t2 * S2[p] / normalize;
				_t3 = t3 * S3[p] / normalize;

				S.Linear(p, t1, S1, t2, S2, t3, S3);
			}
			else
			{
				_t1 = t1; _t2 = t2; _t3 = t3;
			}

			if (Branches != null) foreach (Node node in Branches)
					node._CombineStrats(p, _t1, _t2, _t3);
		}
		*/

		// Hash tracking. Put this immediately after calculating B.
		//int hash = (int)root.Hash(Node.VarB) / 10;
		//if (!HashNote.ContainsKey((int)hash))
		//    HashNote.Add((int)hash, true);
		//else
		//    col++;
		//Console.WriteLine("{0,-36}, Hash = {1,-15}, S[~] = {2,-15}, #{3} {4}", PocketRoot.Result, hash, root.MyPhaseRoot.Branches[0].Branches[0].Branches[50].Branches[0].S[5], HashNote.Count, col);
		//Console.WriteLine("{0,-36}, Hash = {1,-15}", PocketRoot.Result, hash);
		//Console.WriteLine("Average time = {0}", PocketRoot.Best_AverageTime);

		/* BiHarmonic
		for (int i = 0; i < 1000; i++)
		{
			ev1 = root.BestAgainstS();
			Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

			root.CopyTo(Node.VarS, Node.VarHold);
			root.CopyTo(Node.VarB, Node.VarS);
			ev2 = root.BestAgainstS();
			Console.WriteLine("Hash = {0}.", root.Hash(Node.VarB));

			root.BiHarmonicAlg(i + 2, ev1, ev2);
			Console.WriteLine("Hash = {0}.", root.Hash(Node.VarS));
			Console.WriteLine("----------------");
		}
		*/

		/* LP-Loop
		AddColumn();
		root.BestAgainstS(); root.Switch(Node.VarS, Node.VarB); AddColumn();

		for (int i = 0; i < 50; i++)
		{
			Console.WriteLine();
			Console.WriteLine("--------------------------------------------------------");
			Console.WriteLine();

			root.BestAgainstS(); root.Switch(Node.VarS, Node.VarB); AddColumn();
			Console.WriteLine("New pure strat {1} -> {0,-36}", PocketRoot.Result, i);
			root.BestAgainstS();
			Console.WriteLine("   (Best EV against new pure strat is {0})", PocketRoot.Result);

			//Console.WriteLine();
			//Console.WriteLine(MatrixString());
			//Console.WriteLine();

			double[] PartialNash = LpSolver.FindNash(Payoffs, Columns);
			//for (int j = 0; j < Columns; j++)
			//    Console.WriteLine("{0} -> {1}", j, PartialNash[j]);

			//root.MultiCombine_Naive(Columns, PartialNash);
			root.MultiCombine(Columns, PartialNash);
		}
		*/
	}
}
