using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGiver
{
    public int width = 10;
    public int height = 20;

    public string seed = "SEED";
    public bool useRandomSeed = true;

    public int smoothIterations = 5;

    public int randomFillPercent = 45;


    public int[] stayRules = {4};
    public int[] birthRules = {5,6,7,8};
    public int[,] currentMap;

    public int[,,] current3DMap;

    public MapGiver(){
        currentMap = new int[width,height];
        GenerateNewMap();
    }

    public int[,,] MapHochziehen(int hight, int baseHight){
        current3DMap = new int[width,height,width];

        return current3DMap;
    }

    public int[,] GenerateNewMap(){
    
        currentMap = new int[width, height];
        RandomFillMap();
        
        for (int i = 0; i < smoothIterations; i++){
            SmoothMap();
        }     
        return currentMap;
    }

    private void RandomFillMap()
    {
        string currentSeed;
        if (useRandomSeed)
            currentSeed = DateTime.Now.ToString();
        else
            currentSeed = seed;

        System.Random pseudoRandom = new System.Random(currentSeed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    currentMap[x, y] = 1;
                }
                else
                {
                    currentMap[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }    
    }

    private void SmoothMap()
    {
        int[,] secMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);
                bool birth =false;
                bool stay = false;

                foreach(int i in birthRules){
                    if (neighbourWallTiles == i)
                        birth = true;
                }
                foreach(int i in stayRules){
                    if (neighbourWallTiles == i)
                        stay = true;
                }

                if(birth) {
                    secMap[x,y] = 1;
                } else if (stay) {
                    secMap[x,y] = currentMap[x,y];
                } else {
                    secMap[x,y] = 0;
                }
            }
        }
        currentMap = secMap;
    }

    private int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
                {
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += currentMap[neighbourX, neighbourY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;    
        }
}