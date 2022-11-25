using System;
using System.Collections.Generic;
using System.Drawing;

namespace Lawnmower
{
    public enum TerrainType { Grass = 0, Stone = 1, Fence = 2, Lawnmower = 3 };
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
                case TerrainType.Fence:
                    return ('H', ConsoleColor.DarkMagenta, ConsoleColor.DarkGreen);
                default: return ('x', ConsoleColor.DarkRed, ConsoleColor.Black);
            }
        }
    }

    class Lawnmower : Terrain
    {
        public int[] lastCoordinates;
        public Lawnmower(TerrainType terrainType, int[] coordinates) : base(terrainType, coordinates)
        {
        }

        public override (char appearance, ConsoleColor color, ConsoleColor bgColor) GetAppearance()
        {
            return ('@', ConsoleColor.Red, ConsoleColor.Green);
        }

        public Lawnmower MoveLawnmower(Terrain[,] terrain, int[] targetCoordinates)
        {
            terrain[coordinates[0], coordinates[1]] = new Terrain(TerrainType.Grass, coordinates);
            terrain[coordinates[0], coordinates[1]].isMowed = true;

            terrain[targetCoordinates[0], targetCoordinates[1]] = this;
            terrain[targetCoordinates[0], targetCoordinates[1]].coordinates = targetCoordinates;
            return (Lawnmower)terrain[targetCoordinates[0], targetCoordinates[1]];
        }

        public int[] FindPath(Terrain[,] terrain)
        {
            lastCoordinates = coordinates;
            int currentX = coordinates[0];
            int currentY = coordinates[1];



            /*
            if (terrain[currentX, currentY + 1].isMowable() && !terrain[currentX, currentY + 1].isMowed)
            {
                return new int[] { currentX, currentY + 1 };
            }
            else if (terrain[currentX, currentY - 1].isMowable() && !terrain[currentX, currentY - 1].isMowed)
            {
                return new int[] { currentX, currentY - 1 };
            }
            else if (terrain[currentX + 1, currentY].isMowable() && !terrain[currentX + 1, currentY].isMowed)
            {
                return new int[] { currentX + 1, currentY };
            }
            else if (terrain[currentX - 1, currentY].isMowable() && !terrain[currentX - 1, currentY].isMowed)
            {
                return new int[] { currentX - 1, currentY };
            }
            */
             
            /*
            else if (terrain[currentX, currentY + 1].isMowable() && lastCoordinates != new int[] { currentX, currentY + 1 })
            {
                return new int[] { currentX, currentY + 1 };
            }
            else if (terrain[currentX, currentY - 1].isMowable() && lastCoordinates != new int[] { currentX, currentY - 1 })
            {
                return new int[] { currentX, currentY - 1 };
            }
            else if (terrain[currentX + 1, currentY].isMowable() && lastCoordinates != new int[] { currentX + 1, currentY })
            {
                return new int[] { currentX + 1, currentY };
            }
            else if (terrain[currentX - 1, currentY].isMowable() && lastCoordinates != new int[] { currentX - 1, currentY })
            {
                return new int[] { currentX - 1, currentY };
            }
            */


            return new int[] {1,1};
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
                    if (terrain[x, y].terrainType == TerrainType.Grass && !terrain[x,y].isMowed)
                    {
                        grassTerrain.Add(terrain[x, y]);
                    }
                }
            }

            Terrain closestGrass = null;
            int diff = int.MaxValue;

            for (int i = 0; i < grassTerrain.Count; i++)
            {
                if (Math.Abs(grassTerrain[i].coordinates[0] - currentX) < diff)
                {
                    diff = Math.Abs(grassTerrain[i].coordinates[0] - currentX);
                    closestGrass = grassTerrain[i];
                }
                else if (Math.Abs(grassTerrain[i].coordinates[1] - currentY) < diff)
                {
                    diff = Math.Abs(grassTerrain[i].coordinates[1] - currentY);
                    closestGrass = grassTerrain[i];
                }
            }

            //stats
            Console.WriteLine("current pos: "+currentX +","+currentY +" last pos: "+lastCoordinates[0]+","+lastCoordinates[1]);
            if(closestGrass!= null)Console.WriteLine("closest to current: "+closestGrass.coordinates[0]+","+closestGrass.coordinates[1]+"\n\n");

            Console.WriteLine("\nClosest unmowed grass: ("+grassTerrain.Count+")");
            foreach (var item in grassTerrain)
            {
                Console.Write(item.coordinates[0]+","+item.coordinates[1]+"; ");
            }
            
            return null;
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Terrain[,] terrain = GenerateTerrain(6, 6);
            DrawTerrain(terrain);
            StartSimulation(terrain, PlaceLawnmower(terrain));
        }

        static Terrain[,] GenerateTerrain(int width, int length)
        {
            Terrain[,] terrain = new Terrain[width, length];
            Random r = new Random();

            //Generate grass & stones
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                for (int y = 0; y < terrain.GetLength(1); y++) { terrain[x, y] = new Terrain(r.Next(0, 101) > 90 ? TerrainType.Stone : TerrainType.Grass, new int[] { x, y }); }
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

        static Lawnmower PlaceLawnmower(Terrain[,] terrain)
        {
            Random r = new Random();
            int x = r.Next(1, terrain.GetUpperBound(1));
            int y = r.Next(1, terrain.GetUpperBound(0));
            terrain[x, y] = new Lawnmower(TerrainType.Lawnmower, new int[] { x, y });
            return terrain[x, y] as Lawnmower;
        }

        static void DrawTerrain(Terrain[,] terrain)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  ");
            for (int i = 0; i < terrain.GetLength(0); i++)
            {
                Console.Write(i+" ");
            }
            Console.WriteLine();
            for (int x = 0; x < terrain.GetLength(0); x++)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write((x) + "│");
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

        static void StartSimulation(Terrain[,] terrain, Lawnmower lawnmower)
        {
            int stepCounter = 0;
            while (stepCounter <= 50)
            {
                lawnmower = lawnmower.MoveLawnmower(terrain, lawnmower.FindPath(terrain));

                stepCounter++;
                DrawTerrain(terrain);
                lawnmower.FindClosestGrass(terrain);
                System.Threading.Thread.Sleep(150);
            }
        }
    }
}
