using System;
using System.Linq;
//using Microsoft.SolverFoundation;
//using Microsoft.SolverFoundation.Common;
//using Microsoft.SolverFoundation.Services;

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

    class Versus
    {
        public int S1, S2;
        public double Payoff;
    }

    class LpSolver
    {
		/*
        public static double[] FindNash(double[,] PayoffMatrix, int n, bool ShowReport = false)
        {
            SolverContext context = SolverContext.GetContext();
            context.ClearModel();

            Model model = context.CreateModel();
            

            Set StratNum = new Set(Domain.IntegerNonnegative, "StratNum");
            Parameter PayOff = new Parameter(Domain.Real, "PayOff", StratNum, StratNum);

            // List the indices of the pure strates: 1, 2, 3, ..., n
            var StratIndices = new int[n]; for (int i = 0; i < n; i++) StratIndices[i] = i;

            // Convert payoff matrix into a collection of Versus variables.
            // This is the required format for binding the data to our PayOff Parameter.
            var VersusMatrix = from p1 in StratIndices
                               from p2 in StratIndices
                               select new Versus { S1 = p1, S2= p2, Payoff = PayoffMatrix[p1,p2] };
            PayOff.SetBinding(VersusMatrix, "Payoff", "S1", "S2");
            model.AddParameters(PayOff);


            // Declare variable to hold the Nash mixed strategy weights.
            Decision Blend = new Decision(Domain.Probability, "Blend", StratNum);
            model.AddDecisions(Blend);
                        //Decision Worst = new Decision(Domain.Real, "Worst");
                        //model.AddDecisions(Worst);

            // Mixed strategy ratios must add to 1.
            model.AddConstraint("Sum", Model.Sum(Model.ForEach(StratNum, i => Blend[i])) == 1);

            // Nash must perform well against all pure strategies.
            for (int i = 0; i < n; i++)
                model.AddConstraint("bigger" + i, Model.Sum(Model.ForEach(StratNum, j => PayOff[i, j] * Blend[j])) >= 0);
                        //model.AddConstraint("bigger" + i, Model.Sum(Model.ForEach(StratNum, j => PayOff[i, j] * Blend[j])) >= Worst);


            // Get the solution
            Solution solution = context.Solve(new SimplexDirective());
            //context.PropagateDecisions();

            var BlendDecision = solution.Decisions.ToArray()[0].GetValues().ToArray();

            // Report
            if (ShowReport)
            {
                Report report = solution.GetReport();
                Console.Write("{0}", report);
                Console.WriteLine();
                Console.WriteLine();
            }

            // Extract the Nash weights
            double[] Nash = new double[n];
            for (int i = 0; i < n; i++)
                Nash[i] = (double)BlendDecision[i][0];

            return Nash;
        }
		*/
    }
}