using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Lawnmower
{
    public enum TerrainType { Grass = 0, Stone = 1, Tree = 2, Fence = 3, Lawnmower = 4 };
    public enum Direction { South = 0, North = 1, East = 2, West = 3 };
    public enum MapType { Easy, Normal, Hard }
    public class Terrain
    {
        public int tries = 0;
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
        static int[] closestGrassCoordinates;

        List<int[]> triedCoordinates = new List<int[]>();
        List<int[]> inaccessibleCoordinates = new List<int[]>();
        static int lastCoordIndex = 0;

        public Lawnmower(TerrainType terrainType, int[] coordinates) : base(terrainType, coordinates)
        {
        }

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
            bool isTrying = true;
            bool isSet = false;
            int[] closestGrassInRadius = null;



            foreach (var item in directions)
            {
                if (!isSet && terrain[item[0], item[1]] != null && terrain[item[0], item[1]].isMowable() && !terrain[item[0], item[1]].isMowed)
                {
                    closestGrassInRadius = item;
                    isSet = true;
                }
            }

            closestGrassCoordinates = isSet ? closestGrassInRadius : FindClosestGrass(terrain);
            if (!availablePaths.Contains(closestGrassCoordinates) && isSet) availablePaths.Add(closestGrassCoordinates);
            if (closestGrassCoordinates == null) { return null; }

            if (closestGrassCoordinates[0] != currentX && closestGrassCoordinates[1] != currentY)
            {
                Console.WriteLine("try");
                Terrain closestGrass = terrain[closestGrassCoordinates[0], closestGrassCoordinates[1]];
                if (closestGrass.tries < 6)
                {
                    terrain[closestGrassCoordinates[0], closestGrassCoordinates[1]].tries++;
                    isTrying = true;
                }
                else
                {
                    Console.WriteLine("try limit reached!");
                    if (!inaccessibleCoordinates.Contains(closestGrassCoordinates)) { inaccessibleCoordinates.Add(closestGrassCoordinates); }
                    triedCoordinates.Clear();
                    isTrying = false;
                }
            }
            else if (closestGrassCoordinates[0] == currentX && closestGrassCoordinates[1] == currentY)
            {
                triedCoordinates.Clear();
                isTrying = false;
            }

            //check inaccessible coords
            for (int i = 0; i < inaccessibleCoordinates.Count; i++)
            {
                if (Enumerable.SequenceEqual(inaccessibleCoordinates[i], coordinates)) { inaccessibleCoordinates.RemoveAt(i); }
            }

            if (closestGrassCoordinates[0] > currentX && terrain[down[0], down[1]].isMowable()) { availablePaths.Add(down); Console.WriteLine("-1-"); }
            if (closestGrassCoordinates[0] < currentX && terrain[up[0], up[1]].isMowable()) { availablePaths.Add(up); Console.WriteLine("-2-"); }
            if (closestGrassCoordinates[1] > currentY && terrain[right[0], right[1]].isMowable()) { availablePaths.Add(right); Console.WriteLine("-3-"); }
            if (closestGrassCoordinates[1] < currentY && terrain[left[0], left[1]].isMowable()) { availablePaths.Add(left); Console.WriteLine("-4-"); }

            if (availablePaths.Count < 1)
            {
                if (terrain[down[0], down[1]].isMowable()) { availablePaths.Add(down); }
                if (terrain[up[0], up[1]].isMowable()) { availablePaths.Add(up); }
                if (terrain[right[0], right[1]].isMowable()) { availablePaths.Add(right); }
                if (terrain[left[0], left[1]].isMowable()) { availablePaths.Add(left); }
            }

            int lastCount = 0;
            for (int i = 0; i < lastCoordinates.Length; i++)
            {
                for (int j = 0; j < availablePaths.Count; j++)
                {
                    if (Enumerable.SequenceEqual(lastCoordinates[i], availablePaths[j])) { lastCount++; }
                }
            }
            if (availablePaths.Count > lastCount)
            {
                for (int i = 0; i < lastCoordinates.Length; i++)
                {
                    for (int j = 0; j < availablePaths.Count; j++)
                    {
                        if (Enumerable.SequenceEqual(lastCoordinates[i], availablePaths[j])) { availablePaths.RemoveAt(j); }
                    }
                }
            }

            Console.WriteLine("\n\tcurrent pos: " + coordinates[0] + "," + coordinates[1] + " closest pos: "
                + closestGrassCoordinates[0] + "," + closestGrassCoordinates[1] +
                "\ntries: " + terrain[closestGrassCoordinates[0], closestGrassCoordinates[1]].tries +
                " is directr move poss: " + isDirectMovePossible + " is on target: " + isTrying);

            Console.WriteLine("\ninaccessible coordinates: ({0})", inaccessibleCoordinates.Count);
            foreach (var item in inaccessibleCoordinates) { Console.Write("[{0},{1}]", item[0], item[1]); }
            Console.WriteLine("\nlast coordinates paths: ({0})", lastCoordinates.Length);
            foreach (var item in lastCoordinates) { Console.Write("[{0},{1}]", item[0], item[1]); }
            Console.WriteLine("\navailable paths: ({0})", availablePaths.Count);
            foreach (var item in availablePaths) { Console.Write("[{0},{1}]", item[0], item[1]); }

            Random r = new Random();

            int[] path = null;
            if (availablePaths != null) { path = !isTrying ? availablePaths[r.Next(0, availablePaths.Count)] : availablePaths[0]; }
            if (path == down) { facingDirection = Direction.South; }
            else if (path == up) { facingDirection = Direction.North; }
            else if (path == right) { facingDirection = Direction.East; }
            else if (path == left) { facingDirection = Direction.West; }

            Console.WriteLine("\nfacing direction: " + facingDirection);

            Console.WriteLine("final path: " + path);
            return path;
        }

        public int[] FindClosestGrass(Terrain[,] terrain)
        {
            List<Terrain> grassTerrain = new List<Terrain>();
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    //&& !inaccessibleCoordinates.Contains(terrain[x, y].coordinates)
                    if (terrain[x, y].terrainType == TerrainType.Grass && !terrain[x, y].isMowed)
                    {
                        grassTerrain.Add(terrain[x, y]);
                    }
                }
            }
            Terrain closestGrass = null;
            int diff = int.MaxValue;

            Console.WriteLine();
            Console.WriteLine("\tPossible coordinates ");
            for (int i = 0; i < grassTerrain.Count; i++)
            {
                int distance = GetDistance(terrain, grassTerrain[i].coordinates, coordinates);
                if (distance <= diff)
                {
                    diff = distance;
                    closestGrass = grassTerrain[i];
                }
            }

            return closestGrass != null ? closestGrass.coordinates : null;
        }
        public int GetDistance(Terrain[,] terrain, int[] x, int[] y)
        {
            int x1 = x[0];
            int x2 = x[1];

            int y1 = y[0];
            int y2 = y[1];

            var nextXLeft = y1;
            var nextXRight = y1;
            if (x1 < y1)
            {
                nextXLeft = x1;
                nextXRight = terrain.GetLength(0) + x1;
            }
            else if (x1 > y1)
            {
                nextXLeft = terrain.GetLength(0) + x1;
                nextXRight = x1;
            }

            var distanceLeft = nextXLeft - y1;
            var distanceRight = nextXRight - y1;

            // use the Absolute only for comparing which is shorter
            var distanceX = Math.Abs(distanceRight) < Math.Abs(distanceLeft) ? distanceRight : distanceLeft;

            // And finally we want the smallest of both possible distances
            distanceX = Math.Min(distanceLeft, distanceRight);

            // Repeat the same for the Y axis
            var nextYDown = y2;
            var nextYUp = y2;
            if (x2 < y2)
            {
                nextYDown = x2;
                nextYUp = terrain.GetLength(1) + x2;
            }
            else if (x2 > y2)
            {
                nextYDown = terrain.GetLength(1) + x2;
                nextYUp = x2;
            }

            var distanceDown = nextYDown - y2;
            var distanceUp = nextYUp - y2;

            var distanceY = Math.Abs(distanceUp) < Math.Abs(distanceDown) ? distanceUp : distanceDown;

            return Math.Abs(distanceX) + Math.Abs(distanceY);

        }
    }

    internal class Program
    {
        static int obstacleCount = 0;
        static int grassCount = 0;
        static Random r = new Random();

        static MapType difficulty = MapType.Normal;
        static Terrain[,] mainTerrain;
        static int[] starterCoordinates;

        static bool isNewMap = true;
        static bool isStartFromSameCoords = true;

        static int selectedOption = 0;
        static void Main(string[] args)
        {
            Menu(mainTerrain);
        }

        static void Menu(Terrain[,] currentTerrain)
        {
            ConsoleKey key;
            Console.CursorVisible = false;
            string selectMarker = " -> ";

            if (currentTerrain == null) { currentTerrain = GenerateTerrain(); }
            if (mainTerrain == null) { mainTerrain = (Terrain[,])currentTerrain.Clone(); }

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
                    Console.WriteLine(selectMarker + "Terrain difficulty: [{0}]", difficulty);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("     Terrain difficulty: [{0}]", difficulty);
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
                        if (selectedOption > 0) { selectedOption--; }
                        if (selectedOption == 4 && isNewMap) { selectedOption = 3; }
                        break;
                    case ConsoleKey.DownArrow:
                        if (selectedOption < 5) { selectedOption++; }
                        if (selectedOption == 4 && isNewMap) { selectedOption = 5; }
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
                    difficulty = (int)difficulty < 2 ? difficulty + 1 : 0;
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

            switch (difficulty)
            {
                case MapType.Easy:
                    minMapSize = 5;
                    maxMapSize = 7;
                    obstacleRate = 5;
                    break;
                case MapType.Normal:
                    minMapSize = 7;
                    maxMapSize = 10;
                    obstacleRate = 10;
                    break;
                case MapType.Hard:
                    minMapSize = 10;
                    maxMapSize = 15;
                    obstacleRate = 15;
                    break;
            }

            Terrain[,] terrain = new Terrain[r.Next(minMapSize, maxMapSize), r.Next(minMapSize, maxMapSize)];

            int xUpperBound = terrain.GetUpperBound(0);
            int yUpperBound = terrain.GetUpperBound(1);

            //Generate fence
            for (int y = 0; y <= terrain.GetUpperBound(1); y++)
            {
                terrain[0, y] = new Terrain(TerrainType.Fence, new int[] { 0, y });
                terrain[xUpperBound, y] = new Terrain(TerrainType.Fence, new int[] { xUpperBound, y });
            }

            for (int x = 0; x <= terrain.GetUpperBound(0); x++)
            {
                terrain[x, 0] = new Terrain(TerrainType.Fence, new int[] { x, 0 });
                terrain[x, yUpperBound] = new Terrain(TerrainType.Fence, new int[] { x, yUpperBound });
            }

            //Generate grass & stones
            for (int x = 1; x < terrain.GetLength(0) - 1; x++)
            {
                for (int y = 1; y < terrain.GetLength(1) - 1; y++)
                {
                    int chance = r.Next(0, 101);
                    TerrainType type = TerrainType.Grass;

                    if (x > 1 && x < terrain.GetLength(0) - 2 && y > 1 && y < terrain.GetLength(1) - 2)
                    {

                    }
                    //&& terrain[x - 1, y].isMowable() && terrain[x, y - 1].isMowable() && terrain[x - 1, y - 1].isMowable()
                    if (chance < obstacleRate)
                    { type = (TerrainType)r.Next(1, 3); }

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
                    if (terrain[x, y].terrainType == TerrainType.Grass) { grassCount++; }
                    else if (terrain[x, y].terrainType == TerrainType.Stone || terrain[x, y].terrainType == TerrainType.Tree) { obstacleCount++; }
                }
            }
        }

        static Lawnmower PlaceLawnmower(Terrain[,] terrain)
        {
            do
            {
                if (!isStartFromSameCoords || starterCoordinates == null)
                {
                    int x = r.Next(1, terrain.GetLength(0) - 2);
                    int y = r.Next(1, terrain.GetLength(1) - 2);
                    if (terrain[x, y].isMowable())
                    {
                        terrain[x, y] = new Lawnmower(TerrainType.Lawnmower, new int[] { x, y });
                        starterCoordinates = terrain[x, y].coordinates;
                        return terrain[x, y] as Lawnmower;
                    }
                }
                else
                {
                    terrain[starterCoordinates[0], starterCoordinates[1]] = new Lawnmower(TerrainType.Lawnmower, starterCoordinates);
                    return terrain[starterCoordinates[0], starterCoordinates[1]] as Lawnmower;
                }
            } while (true);
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
                    Console.WriteLine("\t---PATH: " + path[0] + "," + path[1]);
                    stepCounter++;
                }
                else
                {
                    isActive = false;
                }

                if (isManualStep)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("\n\n Press 'ESC' to stop the simulation.");
                    Console.ForegroundColor = ConsoleColor.White;

                    if (Console.ReadKey(true).Key == ConsoleKey.Escape) { isActive = false; }
                }
            } while (isActive);

            double optimalPath = 100 - (100 * (stepCounter - grassCount)) / grassCount;
            Console.WriteLine("The path was {0:0.00}% optimal {1} grass, {2} obstacles in {3} steps", optimalPath, grassCount, obstacleCount, stepCounter);
            Console.ReadKey();
            Menu(terrain);
        }
    }
}
