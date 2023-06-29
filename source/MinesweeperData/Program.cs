using System.Diagnostics.Tracing;
using System.Security;
using System.Transactions;
using System.IO;

namespace MinesweeperData
{
    internal class Program
    {
        // x and y size of the grid. 30x16 is the default for expert Minesweeper
        static readonly Vector SIZE = new Vector(30, 16);
        // Number of mines in the grid. 99 is the default for expert Minesweeper
        const int MINE_COUNT = 99;
        // Number of simulations to run
        const int SIMULATION_AMOUNT = 1000000;
        // Relative/absolute path of where the output will be written to
        const string OUTPUT_FILE = @"output.txt";


        public enum State
        {
            None, Mine, CheckedMine
        }
        public struct Vector
        {
            public int x, y;

            public Vector(int x, int y)
            {
                this.x = x; this.y = y;
            }
        }
        public struct Pattern : IComparable<Pattern>
        {
            public readonly Vector[] pattern;
            public int count = 1;

            public Pattern(Vector[] pattern)
            {
                this.pattern = pattern;
            }

            int IComparable<Pattern>.CompareTo(Pattern other)
                => -count.CompareTo(other.count);
        }

        static void Main(string[] args)
        {
            if (MINE_COUNT > (SIZE.x * SIZE.y))
            {
                Console.WriteLine("Error: More mines than spaces.");
                return;
            }

            Console.WriteLine("Getting data...");
            List<Pattern> patterns = GetData(SIZE, MINE_COUNT, SIMULATION_AMOUNT);

            Console.WriteLine("Sorting data...");
            patterns.Sort();

            Console.WriteLine("Formatting data...");
            string output = "";
            foreach (Pattern pattern in patterns)
            {
                output += PrintPattern(pattern, SIMULATION_AMOUNT) + '\n';
            }

            Console.WriteLine("Writing File...");
            File.WriteAllText(OUTPUT_FILE, output);

            Console.WriteLine($"Output successfully written to {Path.GetFullPath(OUTPUT_FILE)}.");
        }

        public static string PrintPattern(Pattern p, int amount)
        {
            // Get min/max
            Vector boundariesMin = new Vector(int.MaxValue, int.MaxValue);
            Vector boundariesMax = new Vector(int.MinValue, int.MinValue);
            foreach (Vector pos in p.pattern)
            {
                if (pos.x < boundariesMin.x)
                    boundariesMin.x = pos.x;
                if (pos.y < boundariesMin.y)
                    boundariesMin.y = pos.y;

                if (pos.x > boundariesMax.x)
                    boundariesMax.x = pos.x;
                if (pos.y > boundariesMax.y)
                    boundariesMax.y = pos.y;
            }

            // Get grid
            Vector size = new Vector(boundariesMax.x - boundariesMin.x + 1, boundariesMax.y - boundariesMin.y + 1);
            bool[,] grid = new bool[size.x, size.y];
            foreach (Vector pos in p.pattern)
            {
                grid[pos.x - boundariesMin.x, pos.y - boundariesMin.y] = true;
            }


            // Print grid
            string output = "";
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    output += grid[x, y] ? 'X' : ' ';
                }
                output += '\n';
            }

            output += $"Count: {p.count}\n" +
                $"Prevalence: {p.count / (decimal)amount}\n";
            return output;
        }

        public static List<Pattern> GetData(Vector size, int mineCount, int amount)
        {
            var data = new List<Pattern>();

            for (int i = 0; i < amount; i++)
            {
                State[,] grid = GenerateGrid(size, mineCount);

                List<Vector[]> patterns = GetPatterns(grid);
                foreach (Vector[] pattern in patterns)
                    AddPattern(pattern);
            }

            return data;

            void AddPattern(Vector[] pattern)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (PatternEquals(data[i].pattern, pattern))
                    {
                        // can't just do data[i].count++ because C# is stupid
                        Pattern temp = data[i];
                        temp.count++;
                        data[i] = temp;

                        return;
                    }
                }

                data.Add(new Pattern(pattern));
            }
        }

        public static bool PatternEquals(Vector[] p1, Vector[] p2)
        {
            if (p1.Length != p2.Length)
                return false;

            for (int i = 0; i < p1.Length; i++)
            {
                if (p1[i].x != p2[i].x || p1[i].y != p2[i].y)
                    return false;
            }

            return true;
        }

        public static State[,] GenerateGrid(Vector size, int mineCount)
        {
            Random rand = new Random();

            State[,] grid = new State[size.x, size.y];

            for (int i = 0; i < mineCount; i++)
            {
                while (!TryPlaceMine()) { }
            }

            return grid;

            bool TryPlaceMine()
            {
                int x = rand.Next(size.x);
                int y = rand.Next(size.y);

                bool alreadyPlaced = grid[x, y] == State.None;
                grid[x, y] = State.Mine;
                return alreadyPlaced;
            }
        }

        public static List<Vector[]> GetPatterns(State[,] grid)
        {
            var patterns = new List<Vector[]>();

            for (int y = 0; y < grid.GetLength(1); y++)
            {
                for (int x = 0; x < grid.GetLength(0); x++)
                {
                    if (grid[x, y] == State.Mine)
                        patterns.Add(GetPattern(grid, new Vector(x, y)));
                }
            }

            return patterns;
        }

        public static Vector[] GetPattern(State[,] grid, Vector pos)
        {
            var pattern = new List<Vector>();

            GetNeighbors(pos);

            return pattern.ToArray();

            void GetNeighbors(Vector currentPos)
            {
                Vector[] neighbors = { new Vector(currentPos.x - 1, currentPos.y), new Vector(currentPos.x + 1, currentPos.y), new Vector(currentPos.x, currentPos.y + 1), new Vector(currentPos.x, currentPos.y - 1) };
                
                grid[currentPos.x, currentPos.y] = State.CheckedMine;
                pattern.Add(new Vector(currentPos.x - pos.x, currentPos.y - pos.y));

                foreach (Vector neighbor in neighbors)
                {
                    if (grid.TryGetValue(neighbor.x, neighbor.y, State.None) == State.Mine)
                    {
                        GetNeighbors(neighbor);
                    }
                }
            }
        }
    }

    public static class Tools
    {
        public static T TryGetValue<T>(this T[,] array, int x, int y, T onOutOfBounds = default)
            => (x < 0 || y < 0 || x >= array.GetLength(0) || y >= array.GetLength(1)) ? onOutOfBounds : array[x, y];
    }
}