using OxyPlot;
using System;
using System.Linq;
using System.Threading;

namespace WpfTSP
{
    public class TSPSolver
    {
        public static event Action<double> BestDistanceUpdated;

        public static int[] SeedFunction(double[,] distanceMatrix)
        {
            var sequence = Enumerable.Range(1, distanceMatrix.GetLength(0)).OrderBy(x => Guid.NewGuid()).ToArray();
            return sequence.Concat(new[] { sequence[0] }).ToArray();
        }

        public static int[] Stochastic2Opt(double[,] distanceMatrix, int[] cityTour)
        {
            var random = new Random();
            int i = random.Next(0, cityTour.Length - 1);
            int j = random.Next(0, cityTour.Length - 1);

            if (i > j)
            {
                (i, j) = (j, i);
            }

            var newTour = cityTour.Take(i)
                .Concat(cityTour.Skip(i).Take(j - i + 1).Reverse())
                .Concat(cityTour.Skip(j + 1))
                .ToArray();

            return newTour;
        }

        public static int[] LocalSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 50, int neighbourhoodSize = 5, Action<double> distanceUpdateCallback = null)
        {
            var count = 0;
            var solution = cityTour.ToArray();
            double bestDistance = DistanceCalc(distanceMatrix, solution);

            while (count < maxAttempts)
            {
                int[] candidate = null;

                for (var i = 0; i < neighbourhoodSize; i++)
                {
                    candidate = Stochastic2Opt(distanceMatrix, solution);
                }

                if (DistanceCalc(distanceMatrix, candidate) < DistanceCalc(distanceMatrix, solution))
                {
                    solution = candidate.ToArray();

                    double currentDistance = DistanceCalc(distanceMatrix, solution);
                    if (currentDistance < bestDistance)
                    {
                        bestDistance = currentDistance;

                        // Raporteaza distanta la fiecare iteratie
                        distanceUpdateCallback?.Invoke(bestDistance);
                    }
                }

                count++;
            }

            return solution;
        }


        public static int[] VariableNeighborhoodSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 20, int neighbourhoodSize = 5, int iterations = 50, CancellationToken cancellationToken = default, PlotModel tspPlotModel = null)
        {
            var count = 0;
            var solution = cityTour.ToArray();
            var bestSolution = cityTour.ToArray();
            double bestDistance = double.MaxValue;

            while (count < iterations)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                solution = LocalSearch(distanceMatrix, solution, maxAttempts, neighbourhoodSize); // Use LocalSearch here

                double currentDistance = DistanceCalc(distanceMatrix, solution);

                if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestSolution = solution.ToArray();

                    BestDistanceUpdated?.Invoke(bestDistance);

                    GraphPlotter.PlotTourWithColor(distanceMatrix, bestSolution, OxyColor.FromRgb(173, 216, 230), tspPlotModel);
                }

                count++;
                Console.WriteLine($"Iteration = {count} -> Distance = {bestDistance}");
            }

            return bestSolution;
        }


        public static double DistanceCalc(double[,] distanceMatrix, int[] cityTour)
        {
            double distance = 0;

            for (int k = 0; k < cityTour.Length - 1; k++)
            {
                int m = k + 1;
                distance += distanceMatrix[cityTour[k] - 1, cityTour[m] - 1];
            }

            return distance;
        }
    }
}
