using OxyPlot;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace WpfTSP
{
    public class TSPSolver
    {
        // Generatorul de numere aleatoare
        private static readonly Random random = new Random();

        // Eveniment care semnalează actualizarea celei mai bune distanțe
        public static event Action<double> BestDistanceUpdated;

        // Funcția pentru generarea soluției inițiale
        public static int[] SeedFunction(double[,] distanceMatrix)
        {
            // Generează o secvență de la 1 la numărul de orașe și o ordonează aleatoriu
            var sequence = Enumerable.Range(1, distanceMatrix.GetLength(0)).OrderBy(x => random.Next()).ToArray();
            // Concatenează primul oraș la sfârșit pentru a închide traseul
            return sequence.Concat(new[] { sequence[0] }).ToArray();
        }
        // Funcție pentru realizarea căutării aleatoare
        private static (int[] solution, double distance) RandomSearch(double[,] distanceMatrix)
        {
            int[] randomSolution = SeedFunction(distanceMatrix);
            double bestDistance = DistanceCalc(distanceMatrix, randomSolution);

            for (int i = 0; i < 100; i++)
            {
                int[] candidate = LocalSearch(distanceMatrix, Stochastic2Opt(distanceMatrix, randomSolution));

                candidate[candidate.Length - 1] = candidate[0];

                double candidateDistance = DistanceCalc(distanceMatrix, candidate);

                if (candidateDistance < bestDistance)
                {
                    bestDistance = candidateDistance;
                    randomSolution = candidate;
                }
            }

            return (randomSolution, bestDistance);
        }


        // Funcție pentru realizarea operației de 2-opt stochastic
        public static int[] Stochastic2Opt(double[,] distanceMatrix, int[] cityTour)
        {
            // Converteste array-ul într-o listă pentru a facilita operațiile de inversare
            var tourList = cityTour.ToList();
            int i, j;

            // Alege două poziții distincte aleatoare în tur
            do
            {
                i = random.Next(0, tourList.Count - 1);
                j = random.Next(0, tourList.Count - 1);
            } while (i == j || (i > 0 && j == i - 1) || (j < tourList.Count - 1 && i == j + 1));

            // Asigură că i < j
            if (i > j)
            {
                (i, j) = (j, i);
            }

            // Inversează segmentul turului între i și j
            tourList.Reverse(i, j - i + 1);

            // Converteste lista inapoi la array
            return tourList.ToArray();
        }

        public static int[] ThreeOpt(double[,] distanceMatrix, int[] cityTour)
        {
            var tourList = cityTour.ToList();
            int n = tourList.Count;

            for (int i = 0; i < n - 6; i++)
            {
                for (int j = i + 2; j < n - 4; j++)
                {
                    for (int k = j + 2; k < n - 2; k++)
                    {
                        var option1 = tourList.GetRange(i, j - i + 1);
                        var option2 = tourList.GetRange(j + 1, k - j);
                        var option3 = tourList.GetRange(k + 1, n - k - 1).Concat(tourList.GetRange(0, i + 1));

                        var newTour = option1.Concat(option2).Concat(option3).ToList();
                        newTour.Add(newTour[0]);

                        double currentDistance = DistanceCalc(distanceMatrix, tourList.ToArray());
                        double newDistance = DistanceCalc(distanceMatrix, newTour.ToArray());

                        if (newDistance < currentDistance)
                        {
                            // Adăugați orașul de pornire la începutul listei
                            tourList = newTour;
                            tourList.Insert(0, tourList[tourList.Count - 1]);
                            break; // Ieșiți din buclă dacă am găsit o îmbunătățire
                        }
                    }
                }
            }

            return tourList.ToArray();
        }



        // Funcție pentru efectuarea căutării locale
        public static int[] LocalSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 50, int neighbourhoodSize = 5, Action<double> distanceUpdateCallback = null)
        {
            var solution = cityTour.ToArray();
            double bestDistance = DistanceCalc(distanceMatrix, solution);
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                var candidate = Stochastic2Opt(distanceMatrix, solution);
                double candidateDistance = DistanceCalc(distanceMatrix, candidate);

                if (candidateDistance < bestDistance)
                {
                    // Adăugați orașul de pornire la sfârșitul listei
                    candidate[candidate.Length - 1] = candidate[0];

                    solution = candidate;
                    bestDistance = candidateDistance;
                    distanceUpdateCallback?.Invoke(bestDistance);
                    attempts = 0; // Resetează numărul de încercări dacă am găsit o îmbunătățire
                }

                attempts++;
            }

            return solution;
        }




        // Funcție pentru realizarea căutării VNS
        public static int[] VariableNeighborhoodSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 20, int neighbourhoodSize = 5, int iterations = 50, CancellationToken cancellationToken = default, PlotModel tspPlotModel = null)
        {
            var count = 0;
            var solution = cityTour.ToArray();
            var bestSolution = cityTour.ToArray();
            double bestDistance = double.MaxValue;

            // Use RandomSearch to get an initial solution
            var randomSearchResult = RandomSearch(distanceMatrix);
            bestSolution = randomSearchResult.solution;
            bestDistance = randomSearchResult.distance;

            // Continue with VNS iterations
            while (count < iterations)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                solution = LocalSearch(distanceMatrix, solution, maxAttempts, neighbourhoodSize);

                double currentDistance = DistanceCalc(distanceMatrix, solution);

                if (currentDistance >= bestDistance)
                {
                    solution = ThreeOpt(distanceMatrix, solution);
                    currentDistance = DistanceCalc(distanceMatrix, solution);
                }

                // Use RandomSearch again
                var randomSearchResultAgain = RandomSearch(distanceMatrix);
                var randomDistance = randomSearchResultAgain.distance;

                if (randomDistance < bestDistance)
                {
                    bestDistance = randomDistance;
                    bestSolution = randomSearchResultAgain.solution;
                }
                else if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestSolution = solution.ToArray();



                    GraphPlotter.PlotTourWithColor(distanceMatrix, bestSolution, OxyColor.FromRgb(173, 216, 230), tspPlotModel);
                    tspPlotModel.InvalidatePlot(true);
                }

                count++;
                Console.WriteLine($"Iteratia = {count} -> Distanta = {bestDistance}");
            }

            Console.WriteLine(GetTourString(bestSolution));
            BestDistanceUpdated?.Invoke(bestDistance);

            return bestSolution;
        }


        // Funcție pentru calcularea distanței turului dat
        public static double DistanceCalc(double[,] distanceMatrix, int[] cityTour)
        {
            // Verifică dacă turul este gol sau are un singur oraș
            if (cityTour == null || cityTour.Length == 0)
            {
                return 0;
            }

            double distance = 0;

            // Calculează distanța totală a turului
            for (int k = 0; k < cityTour.Length - 1; k++)
            {
                int m = k + 1;
                distance += distanceMatrix[cityTour[k] - 1, cityTour[m] - 1];
            }

            return distance;
        }

        // Funcție pentru obținerea unei reprezentări de șir a turului
        private static string GetTourString(int[] cityTour)
        {
            var resultBuilder = new StringBuilder("Traseul final: ");
            for (int i = 0; i < cityTour.Length; i++)
            {
                resultBuilder.Append($"{cityTour[i]} ");
            }
            return resultBuilder.ToString();
        }
    }
}
