using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lawnmower
{
    public enum TerrainType { Grass = 0, Stone = 1, Tree = 2, Fence = 3, Lawnmower = 4 };
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
        public int[] lastCoordinates = new int[] { 0, 0 };
        public Lawnmower(TerrainType terrainType, int[] coordinates) : base(terrainType, coordinates)
        {
        }

        public override (char appearance, ConsoleColor color, ConsoleColor bgColor) GetAppearance()
        {
            return ('@', ConsoleColor.Red, ConsoleColor.Green);
        }

        public Lawnmower MoveLawnmower(Terrain[,] terrain, int[] targetCoordinates)
        {
            lastCoordinates = coordinates;
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

            int[] closestGrassCoordinates = FindClosestGrass(terrain);
            if (closestGrassCoordinates != null)
            {
                List<int[]> availablePaths = new List<int[]>();

                if (closestGrassCoordinates[0] > currentX && terrain[currentX + 1, currentY].isMowable()) { availablePaths.Add(new int[] { currentX + 1, currentY }); } // down
                if (closestGrassCoordinates[0] < currentX && terrain[currentX - 1, currentY].isMowable()) { availablePaths.Add(new int[] { currentX - 1, currentY }); } // up
                if (closestGrassCoordinates[1] > currentY && terrain[currentX, currentY + 1].isMowable()) { availablePaths.Add(new int[] { currentX, currentY + 1 }); } // right
                if (closestGrassCoordinates[1] < currentY && terrain[currentX, currentY - 1].isMowable()) { availablePaths.Add(new int[] { currentX, currentY - 1 }); } // left

                if (availablePaths.Count < 1)
                {
                    if (terrain[currentX + 1, currentY].isMowable()) { availablePaths.Add(new int[] { currentX + 1, currentY }); }
                    if (terrain[currentX - 1, currentY].isMowable()) { availablePaths.Add(new int[] { currentX - 1, currentY }); }
                    if (terrain[currentX, currentY + 1].isMowable()) { availablePaths.Add(new int[] { currentX, currentY + 1 }); }
                    if (terrain[currentX, currentY - 1].isMowable()) { availablePaths.Add(new int[] { currentX, currentY - 1 }); }
                }

                //Console.WriteLine("\n\n---CLOSEST: " + closestGrassCoordinates[0] + "," + closestGrassCoordinates[1]);
                Console.WriteLine("available paths:");
                if (availablePaths.Count > 1) { availablePaths.Remove(lastCoordinates); }

                foreach (var item in availablePaths)
                {
                    Console.WriteLine("\tpath: " + item[0] + "," + item[1]);
                }


                Random r = new Random();
                return availablePaths.Count > 0 ? availablePaths[r.Next(0, availablePaths.Count)] : null;
            }
            return null;
        }

        public int[] FindClosestGrass(Terrain[,] terrain)
        {
            int currentX = coordinates[0];
            int currentY = coordinates[1];

            List<Terrain> grassTerrain = new List<Terrain>();
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    if (terrain[x, y].terrainType == TerrainType.Grass && !terrain[x, y].isMowed)
                    {
                        grassTerrain.Add(terrain[x, y]);
                    }
                }
            }

            Terrain closestGrass = null;
            double diff = int.MaxValue;

            for (int i = 0; i < grassTerrain.Count; i++)
            {
                double tmp = Math.Pow(grassTerrain[i].coordinates[0] - currentX, 2) + Math.Pow(grassTerrain[i].coordinates[1] - currentY, 2);
                if (tmp <= diff)
                {
                    diff = tmp;
                    closestGrass = grassTerrain[i];
                }
            }

            /*
            Console.WriteLine("current pos: " + currentX + "," + currentY + " last pos: " + lastCoordinates[0] + "," + lastCoordinates[1]);
            if (closestGrass != null) Console.WriteLine("closest to current: " + closestGrass.coordinates[0] + "," + closestGrass.coordinates[1] + "\n");


            Console.WriteLine(diff + " legjobb érték");
            //stats
            Console.WriteLine("Closest unmowed grass: (" + grassTerrain.Count + ")");
            foreach (var item in grassTerrain)
            {
                Console.Write(item.coordinates[0] + "," + item.coordinates[1] + "; ");
            }
            */

            return closestGrass != null ? closestGrass.coordinates : null;
        }
    }

    internal class Program
    {
        static int minMapSize = 5, maxMapSize = 21;
        static int obstacleCount = 0;
        static int grassCount = 0;
        static Random r = new Random();

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

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("\n");

                if (selectedOption == 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + "Generate new map");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(" Generate new map");
                }

                if (selectedOption == 1)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + (isNewMap ? "Start simulation" : "Restart simulation"));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine((isNewMap ? " Start simulation" : " Restart simulation"));
                }

                if (selectedOption == 2)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + (isNewMap ? "Start simulation with manual stepping" : "Restart simulation with manual stepping"));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine((isNewMap ? " Start simulation with manual stepping" : " Restart simulation with manual stepping"));
                }

                if (selectedOption == 3)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(selectMarker + " Start from the same position: [{0}]", isStartFromSameCoords);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(" Start from the same position: [{0}]", isStartFromSameCoords);
                }



                if (selectedOption == 4)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n" + selectMarker + " Exit");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("\n Exit");
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        if (selectedOption > 0) { selectedOption--; }
                        break;
                    case ConsoleKey.DownArrow:
                        if (selectedOption < 4) { selectedOption++; }
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
                    isStartFromSameCoords = !isStartFromSameCoords;
                    Menu(currentTerrain);
                    break;
                case 4:
                    Environment.Exit(0);
                    break;
            }
        }

        static Terrain[,] GenerateTerrain()
        {
            isNewMap = true;
            Terrain[,] terrain = new Terrain[r.Next(minMapSize, maxMapSize), r.Next(minMapSize, maxMapSize)];

            //Generate grass & stones
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++)
                {
                    int chance = r.Next(0, 101);
                    TerrainType type = TerrainType.Grass;
                    if (chance > 90) { type = (TerrainType)r.Next(1, 3); }
                    terrain[x, y] = new Terrain(type, new int[] { x, y });
                }
            }

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
                    int x = r.Next(1, terrain.GetUpperBound(0));
                    int y = r.Next(1, terrain.GetUpperBound(1));
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
                    lawnmower = lawnmower.MoveLawnmower(terrain, path);
                    Console.WriteLine("\n---PATH: " + path[0] + "," + path[1]);
                    stepCounter++;
                    Console.WriteLine("\nsteps: " + stepCounter);
                }
                else
                {
                    Console.WriteLine("PATH NULL!");
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
