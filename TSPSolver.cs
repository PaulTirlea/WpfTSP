using OxyPlot;
using System;
using System.Collections.Generic;
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
            int tourLength = tourList.Count;

            for (int i = 0; i < tourLength - 2; i++)
            {
                for (int j = i + 2; j < tourLength - 1; j++)
                {
                    for (int k = j + 2; k < tourLength; k++)
                    {
                        // Implementarea 3-opt: schimbă segmentele alese
                        var newTour = ReverseSegment(tourList, i, j);
                        newTour = ReverseSegment(newTour, j + 1, k);
                        newTour = ReverseSegment(newTour, k + 1, tourLength - 1);

                        double newDistance = DistanceCalc(distanceMatrix, newTour.ToArray());
                        double currentDistance = DistanceCalc(distanceMatrix, tourList.ToArray());

                        // Actualizează turul dacă soluția nouă este mai bună
                        if (newDistance < currentDistance)
                        {
                            tourList = newTour.ToList();
                        }
                    }
                }
            }

            return tourList.ToArray();
        }

        private static List<int> ReverseSegment(List<int> tour, int start, int end)
        {
            var reversedSegment = tour.GetRange(start, end - start + 1);
            reversedSegment.Reverse();
            tour.RemoveRange(start, end - start + 1);
            tour.InsertRange(start, reversedSegment);
            return tour;
        }


        // Funcție pentru efectuarea căutării locale
        public static int[] LocalSearch(double[,] distanceMatrix, int[] cityTour, int maxAttempts = 50, int neighbourhoodSize = 5, Action<double> distanceUpdateCallback = null)
        {
            var count = 0;
            var solution = cityTour.ToArray();
            double bestDistance = DistanceCalc(distanceMatrix, solution);

            // Încearcă îmbunătățirea soluției pentru un număr de încercări specificat
            while (count < maxAttempts)
            {
                var candidate = Stochastic2Opt(distanceMatrix, solution);

                // Adaugă primul oraș la sfârșit pentru a închide traseul
                candidate[candidate.Length - 1] = candidate[0];

                double candidateDistance = DistanceCalc(distanceMatrix, candidate);
                double currentDistance = DistanceCalc(distanceMatrix, solution);

                // Actualizează soluția dacă soluția candidată este mai bună
                if (candidateDistance < currentDistance)
                {
                    solution = candidate.ToArray();

                    // Verifică dacă soluția curentă este cea mai bună globală
                    if (candidateDistance < bestDistance)
                    {
                        bestDistance = candidateDistance;
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

            // Încearcă îmbunătățirea soluției pentru un număr de iterații specificat
            while (count < iterations)
            {
                // Verifică dacă trebuie să întrerupem execuția
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                // Aplică 2-opt și 3-opt alternativ pentru a îmbunătăți soluția curentă
                if (count % 2 == 0)
                {
                    solution = Stochastic2Opt(distanceMatrix, solution);
                }
                else
                {
                    solution = ThreeOpt(distanceMatrix, solution);
                }

                double currentDistance = DistanceCalc(distanceMatrix, solution);

                // Actualizează soluția globală dacă soluția curentă este mai bună
                if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestSolution = solution.ToArray();

                    // Invocă evenimentul de actualizare a celei mai bune distanțe
                    BestDistanceUpdated?.Invoke(bestDistance);

                    // Plotează turul curent cu o culoare specifică
                    GraphPlotter.PlotTourWithColor(distanceMatrix, bestSolution, OxyColor.FromRgb(173, 216, 230), tspPlotModel);
                }

                count++;
                Console.WriteLine($"Iterația = {count} -> Distanța = {bestDistance}");
            }

            // Afișează traseul final în consolă
            Console.WriteLine(GetTourString(bestSolution));

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
