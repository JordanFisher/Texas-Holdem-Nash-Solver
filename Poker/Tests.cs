using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

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
				Tools.LogPrint("EV = {0} : {1} -> {2}", EV1, EV2, TotalEV);
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

								number ev;
								if (DerivedSetup.SuitReduce)
								{
									// Map all of our cards (community and pockets) using the flop map
									// (because we are using a suit reduced data structure).
									var f = Flop.Flops[flop];
									int _flop = Flop.IndexOf(f.Representative);
									int _turn = f.CardMap[turn];
									int _river = f.CardMap[river];

									int _p1 = f.PocketMap[p1];
									int _p2 = f.PocketMap[p2];

									ev = root.Simulate(S1, S2, _p1, _p2, _flop, _turn, _river);
								}
								else
								{
									ev = root.Simulate(S1, S2, p1, p2, flop, turn, river);
								}

								TotalMass += 1;
								EV += ev;

								PocketTotalMass += 1;
								PocketEV += ev;
							}
						}
					}
				}

				//Tools.LogPrint("  EV(p1 = {0}) = {1}", p1, PocketEV / PocketTotalMass);
				//Tools.LogPrint("  EV(p1 = {0}) = {1}", Pocket.Pockets[p1], PocketEV / PocketTotalMass);
			}
			Assert.IsNum(EV / TotalMass);

			return EV / TotalMass;
		}

		/// <summary>
		/// Given S and B = B(S), verify that B is the best strategy against S.
		/// Procedure: Verify EV(B', S) is less than EV(B, S) for all other strategies B'.
		/// </summary>
		public static void B_Test(PocketRoot root)
		{
			number EV;

			// Calculate Strategy B = B(S)
			EV = root.BestAgainstS();

			// Calculate EV(B', S) for many B'
			double Min = double.MaxValue;
			for (int k = 0; k < 10000; k++)
			{
				// Pure passive
				//root.Process(Node.VarB, (n, i) => (number)0);

				// Pure aggressive
				//root.Process(Node.VarB, (n, i) => (number)1);

				// Totally randomize
				//root.Process(Node.VarB, (n, i) => (number)(Tools.Rnd.NextDouble()));

				// Random perturbation
				//root.Process(Node.VarB, (n, i) => n.B[i] + (number)((Tools.Rnd.NextDouble() - .5f) * .1f));

				// Random raise
				root.Process(Node.VarB, (n, i) => n.B[i] + (number)(Tools.Rnd.NextDouble() > .85 ? 1 : 0));

				// Random fold
				//root.Process(Node.VarB, (n, i) => n.B[i] + (number)(Tools.Rnd.NextDouble() > .99 ? -1 : 0));

				// Full simulation
				//number ModdedEV = Simulation(Node.VarB, Node.VarS, root);

				// Monte Carlo simulation
				var game = new Game(new StrategyPlayer(Node.VarB), new StrategyPlayer(Node.VarS), Seed: 0);
				float ModdedEV = (float)game.Round(4999999);
				Tools.LogPrint("Monte Carlo Simulation EV = {0}", ModdedEV);

				Min = Math.Min((double)Min, (double)EV - (double)ModdedEV);
				Tools.LogPrint("Difference = {0}, min = {1}", (double)EV - (double)ModdedEV, Min);
			}
		}

		public static void TestBestAgainstOtherStrategies(PocketRoot root)
		{
			number EV;
			Assert.That(Node.MakeHold);

			root.Process(Node.VarHold, (n, j) => n.B[j] + (number).01);
			EV = Simulation(Node.VarHold, Node.VarS, root);
			Tools.LogPrint("Simulated EV = {0}  (perturbed)", EV);

			root.Process(Node.VarHold, (n, j) => (number).5);
			EV = Simulation(Node.VarS, Node.VarHold, root);
			Tools.LogPrint("Simulated EV = {0}  (idiot)", EV);

			root.Process(Node.VarHold, (n, j) => 1);
			EV = Simulation(Node.VarS, Node.VarHold, root);
			Tools.LogPrint("Simulated EV = {0}  (aggressive)", EV);

			root.Process(Node.VarHold, (n, j) => 0);
			EV = Simulation(Node.VarS, Node.VarHold, root);
			Tools.LogPrint("Simulated EV = {0}  (passive)", EV);
		}

		public static void Combine_Test(PocketRoot root)
		{
			root.Process(Node.VarS,		(node, i) => (number)Math.Cos(i));
			root.Process(Node.VarB,		(node, i) => (number)Math.Sin(i));
			root.Process(Node.VarHold,	(node, i) => (number)Math.Tan(i));

			number h_vs_s = Tests.Simulation(Node.VarHold, Node.VarS, root);
			number h_vs_b = Tests.Simulation(Node.VarHold, Node.VarB, root);

			root.CombineStrats((number).5, (number).5);
			//root.NaiveCombine(Node.VarS, .5, Node.VarB, .5, Node.VarS);

			number h_vs_mix = Tests.Simulation(Node.VarHold, Node.VarS, root);

			Tools.LogPrint("vs s   = {0}", h_vs_s);
			Tools.LogPrint("vs b   = {0}", h_vs_b);
			Tools.LogPrint("vs mix = {0}", h_vs_mix);
			Tools.LogPrint("should = {0}", .5 * (h_vs_s + h_vs_b));
		}

		public static void SaveLoad_Test(PocketRoot root)
		{
			Stopwatch stopwatch;

			root.Process(i => (number)Math.Cos(i));
			var hash_saved = root.Hash(Node.VarS);

			stopwatch = Stopwatch.StartNew();
			var file = root.FullSave();
			stopwatch.Stop();
			Tools.LogPrint("Save done! Time = {0}", stopwatch.Elapsed.TotalSeconds);

			root.Process(i => 0);
			var hash_cleared = root.Hash(Node.VarS);

			stopwatch = Stopwatch.StartNew();
			root.FullLoad(file);
			var hash_loaded = root.Hash(Node.VarS);
			stopwatch.Stop();

			Tools.LogPrint("Load done! Time = {0}", stopwatch.Elapsed.TotalSeconds);

			Tools.LogPrint("Hash comparison {0} vs {1}.  (Sanity check : {2})", hash_saved, hash_loaded, hash_cleared);
		}
	}
}
