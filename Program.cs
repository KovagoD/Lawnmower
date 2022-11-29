using System;
using System.Collections.Generic;
using System.Linq;

namespace Lawnmower
{
    public enum TerrainType { Grass = 0, Stone = 1, Tree = 2, Fence = 3, Lawnmower = 4 };
    public enum Direction { South = 0, North = 1, East = 2, West = 3 };
    public enum MapType { Small, Normal, Large }
    public class Terrain
    {
        public int[] coordinates;
        public TerrainType terrainType;
        public bool isMowable() { return terrainType == TerrainType.Grass ? true : false; }
        public bool isMowed = false;

        public Terrain(TerrainType terrainType, int[] coordinates)
        {
            this.terrainType = terrainType;
            this.coordinates = coordinates;
        }
        public virtual (char appearance, ConsoleColor color, ConsoleColor bgColor) GetAppearance()
        {
            switch (terrainType)
            {
                case TerrainType.Grass:
                    return (isMowed ? 'v' : 'W', isMowed ? ConsoleColor.DarkGreen : ConsoleColor.Green, isMowed ? ConsoleColor.Green : ConsoleColor.DarkGreen);
                case TerrainType.Stone:
                    return ('O', ConsoleColor.Black, ConsoleColor.DarkGreen);
                case TerrainType.Tree:
                    return ('T', ConsoleColor.Black, ConsoleColor.DarkGreen);
                case TerrainType.Fence:
                    return ('H', ConsoleColor.DarkMagenta, ConsoleColor.DarkGreen);
                default: return ('x', ConsoleColor.DarkRed, ConsoleColor.Black);
            }
        }
    }

    class Lawnmower : Terrain
    {
        public Direction facingDirection = Direction.East;
        public int[][] lastCoordinates = new int[2][] { new int[] { 0, 0 }, new int[] { 0, 0 } };
        static int lastCoordIndex = 0;
        static int[] closestGrassCoordinates;

        public Lawnmower(TerrainType terrainType, int[] coordinates) : base(terrainType, coordinates) { }
        public override (char appearance, ConsoleColor color, ConsoleColor bgColor) GetAppearance()
        {
            char appearanceChar = '↑';
            switch (facingDirection)
            {
                case Direction.South:
                    appearanceChar = '↓';
                    break;
                case Direction.North:
                    appearanceChar = '↑';
                    break;
                case Direction.East:
                    appearanceChar = '>';
                    break;
                case Direction.West:
                    appearanceChar = '<';
                    break;
            }
            return (appearanceChar, ConsoleColor.Red, ConsoleColor.Green);
        }

        public Lawnmower Move(Terrain[,] terrain, int[] targetCoordinates)
        {
            lastCoordinates[lastCoordIndex] = coordinates;
            lastCoordIndex = lastCoordIndex == 1 ? 0 : lastCoordIndex + 1;

            terrain[coordinates[0], coordinates[1]] = new Terrain(TerrainType.Grass, coordinates);
            terrain[coordinates[0], coordinates[1]].isMowed = true;

            terrain[targetCoordinates[0], targetCoordinates[1]] = this;
            terrain[targetCoordinates[0], targetCoordinates[1]].coordinates = targetCoordinates;

            return (Lawnmower)terrain[targetCoordinates[0], targetCoordinates[1]];
        }

        public int[] FindPath(Terrain[,] terrain)
        {
            List<int[]> availablePaths = new List<int[]>();
            int currentX = coordinates[0];
            int currentY = coordinates[1];

            int[] down = new int[] { currentX + 1, currentY };
            int[] up = new int[] { currentX - 1, currentY };
            int[] right = new int[] { currentX, currentY + 1 };
            int[] left = new int[] { currentX, currentY - 1 };

            int[][][] allDirections = new int[][][] {
                new int[][] { down, left, right, up },
                new int[][] { up, right, left, down },
                new int[][] { right, down, up, left },
                new int[][] { left, up, down, right }
            };

            int[][] directions = allDirections[(int)facingDirection];

            bool isDirectMovePossible = false;
            int[] closestGrassInRadius = null;

            foreach (var item in directions)
            {
                if (!isDirectMovePossible && terrain[item[0], item[1]] != null && terrain[item[0], item[1]].isMowable() && !terrain[item[0], item[1]].isMowed)
                {
                    closestGrassInRadius = item;
                    isDirectMovePossible = true;
                }
            }

            closestGrassCoordinates = isDirectMovePossible ? closestGrassInRadius : FindClosestGrass(terrain);
            if (!availablePaths.Contains(closestGrassCoordinates) && isDirectMovePossible) availablePaths.Add(closestGrassCoordinates);
            if (closestGrassCoordinates == null) return null;

            if (!isDirectMovePossible)
            {
                if (closestGrassCoordinates[0] > currentX && terrain[down[0], down[1]].isMowable()) { availablePaths.Add(down); }
                if (closestGrassCoordinates[0] < currentX && terrain[up[0], up[1]].isMowable()) { availablePaths.Add(up); }
                if (closestGrassCoordinates[1] > currentY && terrain[right[0], right[1]].isMowable()) { availablePaths.Add(right); }
                if (closestGrassCoordinates[1] < currentY && terrain[left[0], left[1]].isMowable()) { availablePaths.Add(left); }

                if (availablePaths.Count < 1)
                {
                    if (terrain[down[0], down[1]].isMowable()) availablePaths.Add(down);
                    if (terrain[up[0], up[1]].isMowable()) availablePaths.Add(up);
                    if (terrain[right[0], right[1]].isMowable()) availablePaths.Add(right);
                    if (terrain[left[0], left[1]].isMowable()) availablePaths.Add(left);
                }

                int lastCount = 0;
                for (int i = 0; i < lastCoordinates.Length; i++)
                {
                    for (int j = 0; j < availablePaths.Count; j++)
                    {
                        if (Enumerable.SequenceEqual(lastCoordinates[i], availablePaths[j])) lastCount++;
                    }
                }

                if (availablePaths.Count > lastCount)
                {
                    for (int i = 0; i < lastCoordinates.Length; i++)
                    {
                        for (int j = 0; j < availablePaths.Count; j++)
                        {
                            if (Enumerable.SequenceEqual(lastCoordinates[i], availablePaths[j])) availablePaths.RemoveAt(j);
                        }
                    }
                }
            }

            Random r = new Random();

            int[] path = null;
            if (availablePaths != null) { path = isDirectMovePossible ? availablePaths[0] : availablePaths[r.Next(0, availablePaths.Count)]; }

            if (path == down) facingDirection = Direction.South;
            else if (path == up) facingDirection = Direction.North;
            else if (path == right) facingDirection = Direction.East;
            else if (path == left) facingDirection = Direction.West;

            Console.WriteLine("\n\tFacing direction: " + facingDirection);
            Console.WriteLine("\t- Current position [{0},{1}]", coordinates[0], coordinates[1]);
            Console.WriteLine("\t- Closest position [{0},{1}]", closestGrassCoordinates[0], closestGrassCoordinates[1]);
            Console.WriteLine("\t- Direct move possible: " + isDirectMovePossible);

            Console.WriteLine("\n\tLast {0} coordinates:", lastCoordinates.Length);
            string last = "\t";
            foreach (var item in lastCoordinates) last += "[" + item[0] + "," + item[1] + "] ";
            Console.WriteLine(last);

            string available = "\t";
            Console.WriteLine("\n\tAvailable paths: ({0})", availablePaths.Count);
            foreach (var item in availablePaths) available += "[" + item[0] + "," + item[1] + "] ";
            Console.WriteLine(available);

            Console.WriteLine("\n\t- Selected path: " + path[0] + "," + path[1]);

            return path;
        }

        public int[] FindClosestGrass(Terrain[,] terrain)
        {
            List<Terrain> unmowedGrass = new List<Terrain>();
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    if (terrain[x, y].terrainType == TerrainType.Grass && !terrain[x, y].isMowed) unmowedGrass.Add(terrain[x, y]);
                }
            }
            Terrain closestGrass = null;
            int diff = int.MaxValue;
            for (int i = 0; i < unmowedGrass.Count; i++)
            {
                int distance = GetDistance(terrain, unmowedGrass[i].coordinates, coordinates);
                if (distance <= diff)
                {
                    diff = distance;
                    closestGrass = unmowedGrass[i];
                }
            }

            return closestGrass != null ? closestGrass.coordinates : null;
        }
        public int GetDistance(Terrain[,] terrain, int[] x, int[] y)
        {
            int nextXLeft = y[0];
            int nextXRight = y[0];
            if (x[0] < y[0])
            {
                nextXLeft = x[0];
                nextXRight = terrain.GetLength(0) + x[0];
            }
            else if (x[0] > y[0])
            {
                nextXLeft = terrain.GetLength(0) + x[0];
                nextXRight = x[0];
            }
            int distanceX = Math.Min(nextXLeft - y[0], nextXRight - y[0]);

            int nextYDown = y[1];
            int nextYUp = y[1];
            if (x[1] < y[1])
            {
                nextYDown = x[1];
                nextYUp = terrain.GetLength(1) + x[1];
            }
            else if (x[1] > y[1])
            {
                nextYDown = terrain.GetLength(1) + x[1];
                nextYUp = x[1];
            }
            int distanceY = Math.Min(nextYUp - y[1], nextYDown - y[1]);

            return Math.Abs(distanceX) + Math.Abs(distanceY);
        }
    }

    internal class Program
    {
        static Random r = new Random();
        static int selectedOption = 0;

        static bool isNewMap = true;
        static bool isStartFromSameCoords = true;
        static MapType mapSize = MapType.Small;
        static Terrain[,] mainTerrain;
        static int[] starterCoordinates;

        static int obstacleCount = 0;
        static int grassCount = 0;

        static void Main(string[] args) { Menu(mainTerrain); }

        static void Menu(Terrain[,] currentTerrain)
        {
            ConsoleKey key;
            Console.CursorVisible = false;
            string selectMarker = " -> ";

            if (currentTerrain == null) currentTerrain = GenerateTerrain();
            if (mainTerrain == null) mainTerrain = (Terrain[,])currentTerrain.Clone();

            do
            {
                Console.Clear();
                DrawTerrain(currentTerrain);
                Console.WriteLine("\n");

                if (selectedOption == 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + "Generate new map");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("     Generate new map");
                }

                if (selectedOption == 1)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + (isNewMap ? "Start simulation" : "Restart simulation"));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine((isNewMap ? "     Start simulation" : "     Restart simulation"));
                }

                if (selectedOption == 2)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + (isNewMap ? "Start simulation with manual stepping" : "Restart simulation with manual stepping"));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine((isNewMap ? "     Start simulation with manual stepping" : "     Restart simulation with manual stepping"));
                }
                if (selectedOption == 3)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + "Map size: [{0}]", mapSize);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("     Map size: [{0}]", mapSize);
                }

                if (selectedOption == 4 && !isNewMap)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(selectMarker + "Start from the same position: ");
                    Console.ForegroundColor = isStartFromSameCoords ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.Write("[{0}]", isStartFromSameCoords);
                }
                else if (!isNewMap)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("     Start from the same position: ");
                    Console.ForegroundColor = isStartFromSameCoords ? ConsoleColor.DarkGreen : ConsoleColor.DarkRed;
                    Console.Write("[{0}]", isStartFromSameCoords);
                }

                if (selectedOption == 5)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\n" + selectMarker + "Exit");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("\n\n     Exit");
                }

                key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedOption > 0) selectedOption--;
                        if (selectedOption == 4 && isNewMap) selectedOption = 3;
                        break;
                    case ConsoleKey.DownArrow:
                        if (selectedOption < 5) selectedOption++;
                        if (selectedOption == 4 && isNewMap) selectedOption = 5;
                        break;
                }
            } while (key != ConsoleKey.Enter);

            switch (selectedOption)
            {
                case 0:
                    mainTerrain = GenerateTerrain();
                    currentTerrain = mainTerrain;
                    Menu(currentTerrain);
                    break;
                case 1:
                    currentTerrain = (Terrain[,])mainTerrain.Clone();
                    isNewMap = false;
                    StartSimulation(currentTerrain, false);
                    break;
                case 2:
                    currentTerrain = (Terrain[,])mainTerrain.Clone();
                    isNewMap = false;
                    StartSimulation(currentTerrain, true);
                    break;
                case 3:
                    mapSize = (int)mapSize < 2 ? mapSize + 1 : 0;
                    mainTerrain = GenerateTerrain();
                    currentTerrain = mainTerrain;
                    Menu(currentTerrain);
                    break;
                case 4:
                    isStartFromSameCoords = !isStartFromSameCoords;
                    Menu(currentTerrain);
                    break;
                case 5:
                    Environment.Exit(0);
                    break;
            }
        }

        static Terrain[,] GenerateTerrain()
        {
            isNewMap = true;
            int minMapSize = 0;
            int maxMapSize = 0;
            int obstacleRate = 0;

            switch (mapSize)
            {
                case MapType.Small:
                    minMapSize = 7;
                    maxMapSize = 11;
                    obstacleRate = 25;
                    break;
                case MapType.Normal:
                    minMapSize = 11;
                    maxMapSize = 16;
                    obstacleRate = 15;
                    break;
                case MapType.Large:
                    minMapSize = 16;
                    maxMapSize = 21;
                    obstacleRate = 30;
                    break;
            }

            Terrain[,] terrain = new Terrain[r.Next(minMapSize, maxMapSize), r.Next(minMapSize, maxMapSize)];

            //Generate fence
            int xUpperBound = terrain.GetUpperBound(0);
            int yUpperBound = terrain.GetUpperBound(1);

            for (int y = 0; y <= yUpperBound; y++)
            {
                terrain[0, y] = new Terrain(TerrainType.Fence, new int[] { 0, y });
                terrain[xUpperBound, y] = new Terrain(TerrainType.Fence, new int[] { xUpperBound, y });
            }

            for (int x = 0; x <= xUpperBound; x++)
            {
                terrain[x, 0] = new Terrain(TerrainType.Fence, new int[] { x, 0 });
                terrain[x, yUpperBound] = new Terrain(TerrainType.Fence, new int[] { x, yUpperBound });
            }

            //Generate grass & stones
            int[] lastObstacleCoord = new int[] { 0, 0 };

            for (int x = 1; x < terrain.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < terrain.GetLength(1) - 1; y++)
                {
                    int chance = r.Next(0, 101);
                    TerrainType type = TerrainType.Grass;

                    if (lastObstacleCoord[0] != x + 1 && lastObstacleCoord[0] != x - 1 && lastObstacleCoord[1] != y + 1 && lastObstacleCoord[1] != y - 1)
                    {
                        if (chance < obstacleRate)
                        {
                            lastObstacleCoord = new int[] { x, y };
                            type = (TerrainType)r.Next(1, 3);
                        }
                    }

                    terrain[x, y] = new Terrain(type, new int[] { x, y });
                }
            }

            return terrain;
        }

        static void CountGrassAndObstacles(Terrain[,] terrain)
        {
            grassCount = 0;
            obstacleCount = 0;

            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    if (terrain[x, y].terrainType == TerrainType.Grass) grassCount++;
                    else if (terrain[x, y].terrainType == TerrainType.Stone || terrain[x, y].terrainType == TerrainType.Tree) obstacleCount++;
                }
            }
        }

        static Lawnmower PlaceLawnmower(Terrain[,] terrain)
        {
            if (starterCoordinates == null || !isStartFromSameCoords)
            {
                do
                {
                    int x = r.Next(1, terrain.GetLength(0) - 2);
                    int y = r.Next(1, terrain.GetLength(1) - 2);
                    if (terrain[x, y].terrainType == TerrainType.Grass)
                    {
                        terrain[x, y] = new Lawnmower(TerrainType.Lawnmower, new int[] { x, y });
                        starterCoordinates = terrain[x, y].coordinates;
                        return terrain[x, y] as Lawnmower;
                    }
                } while (true);
            }
            else
            {
                terrain[starterCoordinates[0], starterCoordinates[1]] = new Lawnmower(TerrainType.Lawnmower, starterCoordinates);
                return terrain[starterCoordinates[0], starterCoordinates[1]] as Lawnmower;
            }
        }

        static void DrawTerrain(Terrain[,] terrain)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\n\t   ");
            for (int i = 0; i < terrain.GetLength(1); i++) { Console.Write(i + (i >= 10 ? "" : " ")); }
            Console.WriteLine();

            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("\t" + (x) + (x >= 10 ? "|" : " |"));
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    if (terrain[x, y] != null)
                    {
                        (char, ConsoleColor, ConsoleColor) tmp = terrain[x, y].GetAppearance();
                        Console.ForegroundColor = tmp.Item2;
                        Console.BackgroundColor = tmp.Item3;
                        Console.Write(string.Format("{0} ", tmp.Item1));
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.Black;
                }
                Console.WriteLine();
            }
        }

        static void StartSimulation(Terrain[,] terrain, bool isManualStep)
        {
            CountGrassAndObstacles(terrain);
            Lawnmower lawnmower = PlaceLawnmower(terrain);
            lawnmower.facingDirection = (Direction)r.Next(0, 4);

            int stepCounter = 0;
            bool isActive = true;
            do
            {
                Console.Clear();
                DrawTerrain(terrain);

                int[] path = lawnmower.FindPath(terrain);
                if (path != null)
                {
                    lawnmower = lawnmower.Move(terrain, path);
                    stepCounter++;
                }
                else isActive = false;

                if (isManualStep && isActive)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("\t Press 'ESC' to stop the simulation.");
                    Console.ForegroundColor = ConsoleColor.White;

                    if (Console.ReadKey(true).Key == ConsoleKey.Escape) isActive = false;
                }
            } while (isActive);

            double optimalPath = 100 - (100 * (stepCounter - grassCount)) / grassCount;
            Console.WriteLine("\n\tSimulation ended.\n\tThis path was {0:0.00}% optimal {1} grass in {2} steps", optimalPath, grassCount, stepCounter);
            Console.ReadKey();
            Menu(terrain);
        }
    }
}
