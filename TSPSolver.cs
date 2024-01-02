using OxyPlot;
using System;
using System.Linq;
using System.Threading;

namespace WpfTSP
{
    public class TSPSolver
    {
        // Eveniment pentru actualizarea celei mai bune distanțe
        public static event Action<double> BestDistanceUpdated;

        /// <summary>
        /// Generează o secvență aleatoare pentru inițializarea traseului.
        /// </summary>
        /// <param name="distanceMatrix">Matricea de distanțe între orașe.</param>
        /// <returns>Un traseu inițial aleator.</returns>
        public static int[] SeedFunction(double[,] distanceMatrix)
        {
            var sequence = Enumerable.Range(1, distanceMatrix.GetLength(0)).OrderBy(x => Guid.NewGuid()).ToArray();
            return sequence.Concat(new[] { sequence[0] }).ToArray();
        }

        /// <summary>
        /// Realizează o mutare 2-opt stochastic pe traseul dat.
        /// </summary>
        /// <param name="distanceMatrix">Matricea de distanțe între orașe.</param>
        /// <param name="cityTour">Traseul orașelor.</param>
        /// <returns>Un nou traseu obținut prin aplicarea mutării 2-opt stochastic.</returns>
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

        /// <summary>
        /// Realizează o căutare locală folosind mutarea 2-opt stochastic.
        /// </summary>
        /// <param name="distanceMatrix">Matricea de distanțe între orașe.</param>
        /// <param name="cityTour">Traseul orașelor inițial.</param>
        /// <param name="maxAttempts">Numărul maxim de încercări de îmbunătățire a soluției.</param>
        /// <param name="neighbourhoodSize">Dimensiunea vecinătății pentru mutarea 2-opt stochastic.</param>
        /// <param name="distanceUpdateCallback">Callback pentru actualizarea distanței la fiecare iteratie.</param>
        /// <returns>Traseul optimizat prin căutarea locală.</returns>
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

                        // Raportează distanța la fiecare iteratie
                        distanceUpdateCallback?.Invoke(bestDistance);
                    }
                }

                count++;
            }

            return solution;
        }

        /// <summary>
        /// Realizează căutarea în vecinătate variabilă pentru optimizarea traseului.
        /// </summary>
        /// <param name="distanceMatrix">Matricea de distanțe între orașe.</param>
        /// <param name="cityTour">Traseul orașelor inițial.</param>
        /// <param name="maxAttempts">Numărul maxim de încercări de îmbunătățire a soluției într-un vecinătate.</param>
        /// <param name="neighbourhoodSize">Dimensiunea vecinătății pentru mutarea 2-opt stochastic în căutarea locală.</param>
        /// <param name="iterations">Numărul total de iterații pentru căutarea în vecinătate variabilă.</param>
        /// <param name="cancellationToken">Token pentru anularea operației.</param>
        /// <param name="tspPlotModel">Modelul de grafic pentru afișarea traseului.</param>
        /// <returns>Traseul optimizat găsit în urma căutării în vecinătate variabilă.</returns>
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

                solution = LocalSearch(distanceMatrix, solution, maxAttempts, neighbourhoodSize); // Utilizează căutarea locală aici

                double currentDistance = DistanceCalc(distanceMatrix, solution);

                if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestSolution = solution.ToArray();

                    BestDistanceUpdated?.Invoke(bestDistance);

                    GraphPlotter.PlotTourWithColor(distanceMatrix, bestSolution, OxyColor.FromRgb(173, 216, 230), tspPlotModel);
                }

                count++;
                Console.WriteLine($"Iterația = {count} -> Distanța = {bestDistance}");
            }

            return bestSolution;
        }

        /// <summary>
        /// Calculează distanța totală a traseului dat.
        /// </summary>
        /// <param name="distanceMatrix">Matricea de distanțe între orașe.</param>
        /// <param name="cityTour">Traseul orașelor.</param>
        /// <returns>Distanța totală a traseului.</returns>
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
