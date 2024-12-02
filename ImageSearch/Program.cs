using System.Drawing;

namespace ImageSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: ImageSearch <image1> <image2> <nThreads> <algorithm>");
                return;
            }

            string largeImageFile = args[0];
            string smallImageFile = args[1];
            if (!int.TryParse(args[2], out int nThreads) || nThreads < 1)
            {
                Console.WriteLine("Invalid number of threads.");
                return;
            }
            string algorithm = args[3];

            if (algorithm != "exact" && algorithm != "euclidian")
            {
                Console.WriteLine("Invalid algorithm. Use 'exact' or 'euclidian'.");
                return;
            }

            if (!File.Exists(largeImageFile))
            {
                Console.WriteLine("Big picture does not exist.");
                return;
            }
            if (!File.Exists(smallImageFile))
            {
                Console.WriteLine("Small picture does not exist.");
                return;
            }

            Color[][] largeImage = LoadImage(largeImageFile);
            Color[][] smallImage = LoadImage(smallImageFile);

            if (largeImage == null || smallImage == null)
            {
                Console.WriteLine("Error loading images.");
                return;
            }


            List<(int, int)> results = new List<(int, int)>();

            if (algorithm == "exact")
            {
                results = SearchExact(largeImage, smallImage, nThreads);
            }
            else if (algorithm == "euclidian")
            {
                results = SearchEuclidian(largeImage, smallImage, nThreads);
            }

            if (results.Count == 0)
            {
                Console.WriteLine("No matches found.");
            }
            else
            {
                foreach (var result in results)
                {
                    Console.WriteLine($"{result.Item1},{result.Item2}");
                }
            }
        }

        static Color[][] LoadImage(string filePath)
        {
            try
            {
                Bitmap bitmap = new Bitmap(filePath);
                Color[][] pixels = new Color[bitmap.Height][];
                for (int y = 0; y < bitmap.Height; y++)
                {
                    pixels[y] = new Color[bitmap.Width];
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        pixels[y][x] = bitmap.GetPixel(x, y);
                    }
                }
                return pixels;
            }
            catch
            {
                return null;
            }
        }

        static List<(int, int)> SearchExact(Color[][] largeImage, Color[][] smallImage, int nThreads)
        {
            int largeHeight = largeImage.Length;
            int largeWidth = largeImage[0].Length;
            int smallHeight = smallImage.Length;
            int smallWidth = smallImage[0].Length;
            List<(int, int)> results = new List<(int, int)>();

            void SearchPart(int startY, int endY)
            {
                List<(int, int)> localResults = new List<(int, int)>();
                for (int y = startY; y <= endY - smallHeight; y++)
                {
                    for (int x = 0; x <= largeWidth - smallWidth; x++)
                    {
                        bool match = true;
                        for (int j = 0; j < smallHeight && match; j++)
                        {
                            for (int i = 0; i < smallWidth; i++)
                            {
                                if (largeImage[y + j][x + i] != smallImage[j][i])
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match)
                        {
                            localResults.Add((y, x));
                        }
                    }
                }
                lock (results)
                {
                    results.AddRange(localResults);
                }
            }

            int partSize = largeHeight / nThreads;
            int overlap = smallHeight - 1;
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < nThreads; i++)
            {
                int startY = i * partSize;
                int endY = (i == nThreads - 1) ? largeHeight - 1 : (i + 1) * partSize + overlap - 1;
                threads.Add(new Thread(() => SearchPart(startY, endY)));
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            return results;
        }

        static List<(int, int)> SearchEuclidian(Color[][] largeImage, Color[][] smallImage, int nThreads)
        {
            int largeHeight = largeImage.Length;
            int largeWidth = largeImage[0].Length;
            int smallHeight = smallImage.Length;
            int smallWidth = smallImage[0].Length;
            List<(int, int)> results = new List<(int, int)>();

            double tolerance = 1000; // Set an appropriate tolerance value

            void SearchPart(int startY, int endY)
            {
                List<(int, int)> localResults = new List<(int, int)>();
                for (int y = startY; y <= endY - smallHeight; y++)
                {
                    for (int x = 0; x <= largeWidth - smallWidth; x++)
                    {
                        double totalDistance = 0;
                        bool match = true;
                        for (int j = 0; j < smallHeight && match; j++)
                        {
                            for (int i = 0; i < smallWidth; i++)
                            {
                                Color c1 = largeImage[y + j][x + i];
                                Color c2 = smallImage[j][i];
                                double distance = Math.Sqrt(
                                    Math.Pow(c1.R - c2.R, 2) +
                                    Math.Pow(c1.G - c2.G, 2) +
                                    Math.Pow(c1.B - c2.B, 2));
                                totalDistance += distance;

                                // Early exit if the total distance exceeds tolerance
                                if (totalDistance > tolerance)
                                {
                                    match = false;
                                    break;
                                }
                            }
                        }
                        if (match && totalDistance <= tolerance)
                        {
                            localResults.Add((y, x));
                        }
                    }
                }
                lock (results)
                {
                    results.AddRange(localResults);
                }
            }

            int partSize = largeHeight / nThreads;
            int overlap = smallHeight - 1;
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < nThreads; i++)
            {
                int startY = i * partSize;
                int endY = (i == nThreads - 1) ? largeHeight - 1 : (i + 1) * partSize + overlap - 1;
                threads.Add(new Thread(() => SearchPart(startY, endY)));
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            return results;
        }
    }
}