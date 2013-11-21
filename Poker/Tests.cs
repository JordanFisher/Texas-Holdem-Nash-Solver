using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poker
{
#if SINGLE
	using number = Single;
#elif DOUBLE
	using number = Double;
#elif DECIMAL
	using number = Decimal;
#endif

	class Tests
	{
		public static number Simulation(Var S1, Var S2, PocketRoot root)
		{
			if (BetNode.SimultaneousBetting)
			{
				return _Simulation(S1, S2, root);
			}
			else
			{
				number EV1 = _Simulation(S1, S2, root);
				number EV2 = -_Simulation(S2, S1, root);
				number TotalEV = ((number).5) * (EV1 + EV2);
				Console.WriteLine("EV = {0} : {1} -> {2}", EV1, EV2, TotalEV);
				return TotalEV;
			}
		}

		public static number _Simulation(Var S1, Var S2, PocketRoot root)
		{
			number EV = 0, TotalMass = 0;

			for (int p1 = 0; p1 < Pocket.N; p1++)
			{
				number PocketEV = 0, PocketTotalMass = 0;
				for (int p2 = 0; p2 < Pocket.N; p2++)
				{
					if (Pocket.Pockets[p1].Overlaps(Pocket.Pockets[p2])) continue;

					for (int flop = 0; flop < Flop.N; flop++)
					{
						if (Pocket.Pockets[p1].Overlaps(Flop.Flops[flop])) continue;
						if (Pocket.Pockets[p2].Overlaps(Flop.Flops[flop])) continue;

						for (int turn = 0; turn < Card.N; turn++)
						{
							if (Flop.Flops[flop].Contains(turn)) continue;
							if (Pocket.Pockets[p1].Contains(turn)) continue;
							if (Pocket.Pockets[p2].Contains(turn)) continue;

							for (int river = 0; river < Card.N; river++)
							{
								if (turn == river) continue;
								if (Flop.Flops[flop].Contains(river)) continue;
								if (Pocket.Pockets[p1].Contains(river)) continue;
								if (Pocket.Pockets[p2].Contains(river)) continue;

#if SUIT_REDUCE
								// Map all of our cards (community and pockets) using the flop map
								// (because we are using a suit reduced data structure).
								var f = Flop.Flops[flop];
								int _flop = Flop.IndexOf(f.Representative);
								int _turn = f.CardMap[turn];
								int _river = f.CardMap[river];

								int _p1 = f.PocketMap[p1];
								int _p2 = f.PocketMap[p2];

								number ev = root.Simulate(S1, S2, _p1, _p2, _flop, _turn, _river);
#else
								number ev = root.Simulate(S1, S2, p1, p2, flop, turn, river);
#endif

								TotalMass += 1;
								EV += ev;

								PocketTotalMass += 1;
								PocketEV += ev;
							}
						}
					}
				}

				//Console.WriteLine("  EV(p1 = {0}) = {1}", p1, PocketEV / PocketTotalMass);
				//Console.WriteLine("  EV(p1 = {0}) = {1}", Pocket.Pockets[p1], PocketEV / PocketTotalMass);
			}
			Assert.IsNum(EV / TotalMass);

			return EV / TotalMass;
		}

		/// <summary>
		/// Given S and B = B(S), verify that B is the best strategy against S.
		/// Procedure: Verify EV(~B, S) < EV(B, S) for all other strategies ~B.
		/// </summary>
		private static void B_Test(PocketRoot root)
		{
			number EV;
			Assert.That(Node.MakeHold);

			// Strategy S
			root.Process(i => (number)Math.Cos(i));
			//root.Process(i => .5);
			//root.Process(Node.VarB, (n, i) => Math.Sin(i));

			// Strategy B = Hold = B(S). Must have Hold data initialized.
			EV = root.BestAgainstS();
			root.CopyTo(Node.VarB, Node.VarHold);

			// Calculate EV(~B, S) for many ~B
			number Min = number.MaxValue;
			for (int k = 0; k < 10000; k++)
			{
				root.CopyTo(Node.VarHold, Node.VarB);
				root.Process(Node.VarB, (n, i) => n.B[i] + (number)((Tools.Rnd.NextDouble() - .5f) * .01f));
				number ModdedEV = Simulation(Node.VarB, Node.VarS, root);
				Min = Math.Min(Min, EV - ModdedEV);
				Console.WriteLine("Difference = {0}, min = {1}", EV - ModdedEV, Min);
			}
		}

		public static void TestBestAgainstOtherStrategies(PocketRoot root)
		{
			number EV;
			Assert.That(Node.MakeHold);

			root.Process(Node.VarHold, (n, j) => number.IsNaN(n.B[j]) ? 0 : n.B[j] + .01f);
			EV = Simulation(Node.VarHold, Node.VarS, root);
			Console.WriteLine("Simulated EV = {0}  (perturbed)", EV);

			root.Process(Node.VarHold, (n, j) => .5f);
			EV = Simulation(Node.VarS, Node.VarHold, root);
			Console.WriteLine("Simulated EV = {0}  (idiot)", EV);

			root.Process(Node.VarHold, (n, j) => 1);
			EV = Simulation(Node.VarS, Node.VarHold, root);
			Console.WriteLine("Simulated EV = {0}  (aggressive)", EV);

			root.Process(Node.VarHold, (n, j) => 0);
			EV = Simulation(Node.VarS, Node.VarHold, root);
			Console.WriteLine("Simulated EV = {0}  (passive)", EV);
		}
	}
}
