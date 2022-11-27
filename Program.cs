using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lawnmower
{
    public enum TerrainType { Grass = 0, Stone = 1, Tree = 2, Fence = 3, Lawnmower = 4 };
    public enum Direction { South, North, East, West };
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
            int currentX = coordinates[0];
            int currentY = coordinates[1];

            int[] down = new int[] { currentX + 1, currentY };
            int[] up = new int[] { currentX - 1, currentY };
            int[] right = new int[] { currentX, currentY + 1 };
            int[] left = new int[] { currentX, currentY - 1 };

            int[][] southDirections = new int[][] { down, left, right, up };
            int[][] northDirections = new int[][] { up, right, left, down };
            int[][] eastDirections = new int[][] { right, down, up, left };
            int[][] westDirections = new int[][] { left, up, down, right };

            List<int[]> availablePaths = new List<int[]>();

            int[][] directions = southDirections;
            switch (facingDirection)
            {
                case Direction.South:
                    directions = southDirections;
                    break;
                case Direction.North:
                    directions = northDirections;
                    break;
                case Direction.East:
                    directions = eastDirections;
                    break;
                case Direction.West:
                    directions = westDirections;
                    break;
            }

            bool isDirectMovePossible = false;
            bool isTrying = true;

            Console.WriteLine("\ninaccessible coordinates: ({0})", inaccessibleCoordinates.Count);
            foreach (var item in inaccessibleCoordinates) { Console.Write("[{0},{1}]", item[0], item[1]); }

            Console.WriteLine("\nlast coordinates paths: ({0})", lastCoordinates.Length);
            foreach (var item in lastCoordinates) { Console.Write("[{0},{1}]", item[0], item[1]); }

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
                if (terrain[closestGrassCoordinates[0], closestGrassCoordinates[1]].tries < 6)
                {
                    terrain[closestGrassCoordinates[0], closestGrassCoordinates[1]].tries++;
                    isTrying = false;
                }
                else
                {
                    Console.WriteLine("try limit reached!");
                    if (!inaccessibleCoordinates.Contains(closestGrassCoordinates)) { inaccessibleCoordinates.Add(closestGrassCoordinates); }
                    triedCoordinates.Clear();
                    isTrying = true;
                }
            }
            else if (closestGrassCoordinates[0] == currentX && closestGrassCoordinates[1] == currentY)
            {
                if (inaccessibleCoordinates.Contains(closestGrassCoordinates)) { inaccessibleCoordinates.Remove(closestGrassCoordinates); }
                triedCoordinates.Clear();
                isTrying = true;
            }

            //check inaccessible coords
            for (int i = 0; i < inaccessibleCoordinates.Count; i++)
            {
                if (inaccessibleCoordinates[i][0] == coordinates[0] && inaccessibleCoordinates[i][1] == coordinates[1])
                {
                    inaccessibleCoordinates.RemoveAt(i);
                }
            }

            if (closestGrassCoordinates[0] > currentX && terrain[down[0], down[1]].isMowable()) { availablePaths.Add(down); Console.WriteLine("-1-"); }
            if (closestGrassCoordinates[0] < currentX && terrain[up[0], up[1]].isMowable()) { availablePaths.Add(up); Console.WriteLine("-2-"); }
            if (closestGrassCoordinates[1] > currentY && terrain[right[0], right[1]].isMowable()) { availablePaths.Add(right); Console.WriteLine("-3-"); }
            if (closestGrassCoordinates[1] < currentY && terrain[left[0], left[1]].isMowable()) { availablePaths.Add(left); Console.WriteLine("-4-"); }

            if (availablePaths.Count == 0)
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
                        if (lastCoordinates[i][0] == availablePaths[j][0] && lastCoordinates[i][1] == availablePaths[j][1])
                        {
                            lastCount++;
                        }
                    }
                }
                if (availablePaths.Count > lastCount)
                {
                    for (int i = 0; i < lastCoordinates.Length; i++)
                    {
                        for (int j = 0; j < availablePaths.Count; j++)
                        {
                            if (lastCoordinates[i][0] == availablePaths[j][0] && lastCoordinates[i][1] == availablePaths[j][1])
                            {
                                availablePaths.RemoveAt(j);
                            }
                        }
                    }
                }
            

            Console.WriteLine(availablePaths.Count + " paths");
            Console.WriteLine("\n\tcurrent pos: " + coordinates[0] + "," + coordinates[1] + " closest pos: "
                + closestGrassCoordinates[0] + "," + closestGrassCoordinates[1] +
                " tries: " + terrain[closestGrassCoordinates[0], closestGrassCoordinates[1]].tries +
                " is directr move poss: " + isDirectMovePossible + " is on target: " + isTrying);


            /*

            if (availablePaths.Count < 0 || availablePaths == null)
            {
                if (terrain[down[0], down[1]].isMowable()) { availablePaths.Add(down); }
                if (terrain[up[0], up[1]].isMowable()) { availablePaths.Add(up); }
                if (terrain[right[0], right[1]].isMowable()) { availablePaths.Add(right); }
                if (terrain[left[0], left[1]].isMowable()) { availablePaths.Add(left); }
            }

                if (closestGrassCoordinates != null)
                {
                    foreach (var item in directions)
                    {
                        if (terrain[item[0], item[1]] != null && terrain[item[0], item[1]].isMowable())
                        {
                            if (!availablePaths.Contains(item)) availablePaths.Add(item);
                            isDirectMovePossible = true;
                        }
                    }

                    if (availablePaths.Count > 1 && !isMovingToTarget)
                    {
                        for (int i = 0; i < lastCoordinates.Length; i++)
                        {
                            for (int j = 0; j < availablePaths.Count; j++)
                            {
                                if (lastCoordinates[i][0] == availablePaths[j][0] && lastCoordinates[i][1] == availablePaths[j][1])
                                {
                                    availablePaths.RemoveAt(j);
                                }
                            }
                        }

                        for (int i = 0; i < triedCoordinates.Count; i++)
                        {
                            for (int j = 0; j < availablePaths.Count; j++)
                            {
                                if (triedCoordinates[i][0] == availablePaths[j][0] && triedCoordinates[i][1] == availablePaths[j][1])
                                {
                                    availablePaths.RemoveAt(j);
                                }
                            }
                        }

                    }
                }
            */

            Console.WriteLine("\navailable paths: ({0})", availablePaths.Count);
            foreach (var item in availablePaths) { Console.Write("[{0},{1}]", item[0], item[1]); }


            Random r = new Random();

            int[] path = !isTrying ? availablePaths[r.Next(0, availablePaths.Count)] : availablePaths[0];

            //else if (availablePaths.Count > 1) { path = availablePaths[r.Next(0, availablePaths.Count)]; }

            /*
            if (availablePaths.Count > 0)
            {
                path = isDirectMovePossible ? availablePaths[0] : availablePaths[r.Next(0, availablePaths.Count)];
                //if (!triedCoordinates.Contains(path)) { triedCoordinates.Add(path); }
            }
            */

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
                    if (terrain[x, y].terrainType == TerrainType.Grass && !terrain[x, y].isMowed && !inaccessibleCoordinates.Contains(terrain[x, y].coordinates))
                    {
                        grassTerrain.Add(terrain[x, y]);
                    }
                }
            }
            Terrain closestGrass = null;
            double diff = int.MaxValue;

            for (int i = 0; i < grassTerrain.Count; i++)
            {
                double tmp = Math.Pow(grassTerrain[i].coordinates[0] - coordinates[0], 2) + Math.Pow(grassTerrain[i].coordinates[1] - coordinates[1], 2);
                if (tmp <= diff)
                {
                    diff = tmp;
                    closestGrass = grassTerrain[i];
                }
            }

            return closestGrass != null ? closestGrass.coordinates : null;
        }

        bool ContainsCoordinate(int[][] array, int[] coordinate)
        {
            foreach (var item in array)
            {
                if (item[0] == coordinate[0] && item[1] == coordinate[1]) { return true; }
            }
            return false;
        }

        bool ContainsCoordinate(List<int[]> list, int[] coordinate)
        {
            foreach (var item in list)
            {
                if (item[0] == coordinate[0] && item[1] == coordinate[1]) { return true; }
            }
            return false;
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
                    StartSimulation(currentTerrain, PlaceLawnmower(currentTerrain), false);
                    break;
                case 2:
                    currentTerrain = (Terrain[,])mainTerrain.Clone();
                    isNewMap = false;
                    StartSimulation(currentTerrain, PlaceLawnmower(currentTerrain), true);
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
                    obstacleRate = 40;
                    break;
                case MapType.Normal:
                    minMapSize = 7;
                    maxMapSize = 10;
                    obstacleRate = 80;
                    break;
                case MapType.Hard:
                    minMapSize = 10;
                    maxMapSize = 15;
                    obstacleRate = 80;
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
                    if (chance > obstacleRate) { type = (TerrainType)r.Next(1, 3); }
                    if (x > 1 && x < terrain.GetLength(0) - 2 && y > 1 && y < terrain.GetLength(1) - 2)
                    {
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
                    int x = r.Next(1, terrain.GetLength(0) - 1);
                    int y = r.Next(1, terrain.GetLength(1) - 1);
                    if (terrain[x, y].isMowable())
                    {
                        terrain[x, y] = new Lawnmower(TerrainType.Lawnmower, new int[] { x, y });
                        starterCoordinates = terrain[x, y].coordinates;
                        (terrain[x, y] as Lawnmower).facingDirection = (Direction)r.Next(0, 5);
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

        static void StartSimulation(Terrain[,] terrain, Lawnmower lawnmower, bool isManualStep)
        {
            CountGrassAndObstacles(terrain);

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
