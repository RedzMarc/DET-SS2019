using UnityEngine;
using System.Collections;
using System;

public class MapGenerator : MonoBehaviour
{

    public int width = 10;
    public int height = 20;

    public string seed;
    public bool useRandomSeed;

    public int conwayIterations;

    public int smoothIterations = 5;

    [Range(0, 100)]
    public int randomFillPercent;

    public int[,] map;

    public int chunkSize = 5;

    public Material textureAtlas, fluidTexture;



    [SerializeField] int[] stayRules;
    [SerializeField] int[] birthRules;

    void Start()
    {
        GenerateMap();
        for (int i = 0; i < (height / 5); i++)
        {
            for (int j = 0; j < (width / 5); j++)
            {
                int[,] mapPart = new int[5,5];
                for (int k = 0; k < 5; k++)
                {
                    for (int l = 0; l < 5; l++)
                    {
                        mapPart[k,l] = map[k+j*5,l+i*5];                    
                    }
                }
                BuildChunkAt(i,j,0,map);
            }
        }
    }


	private void BuildChunkAt(int x, int y, int z, int[,] map)
	{
		Vector3 chunkPosition = new Vector3(x*chunkSize, 
											y*chunkSize, 
											z*chunkSize);
					
		Chunk c;

        c = new Chunk(chunkPosition, textureAtlas, fluidTexture, map);
        c.chunk.transform.parent = this.transform;
        c.fluid.transform.parent = this.transform;		
	}

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
            //Conways();
        }

        if (Input.GetMouseButtonDown(1))
        {
            SmoothMap();
        }

    }

    public int[,] GenerateMap()
    {
        map = new int[width, height];
        RandomFillMap();
        
        for (int i = 0; i < smoothIterations; i++){
            SmoothMap();
        } 
        return map;
    }

    void Glider(int wi , int he)
    {
        int mx = wi;
        int my = he;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == mx && y == my + 1 ||
                   x == mx + 1 && y == my ||
                   x == mx - 1 && y == my - 1 ||
                   x == mx && y == my - 1 ||
                   x == mx + 1 && y == my - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = 0;
                }
            }
        }
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }
            }
        }
    }


    void Conways()
    {
        int[,] secMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {

                int neighbourWallTiles = GetSurroundingWallCount(x, y);
                if (neighbourWallTiles == 3 && map[x, y] == 0)
                    secMap[x, y] = 1;
                else if (neighbourWallTiles < 2 && map[x, y] == 1)
                    secMap[x, y] = 0;
                else if ((neighbourWallTiles == 2 || neighbourWallTiles == 3) && map[x, y] == 1)
                    secMap[x, y] = 1;
                else if (neighbourWallTiles >  3    && map[x, y] == 1)
                    secMap[x, y] = 0;
                else 
                    secMap[x,y] = map[x,y];
            }
        }
        map = secMap;
    }

    void SmoothMap()
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
                    secMap[x,y] = map[x,y];
                } else {
                    secMap[x,y] = 0;
                }
            }
        }
        map = secMap;
    }





    int GetSurroundingWallCount(int gridX, int gridY)
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
                        wallCount += map[neighbourX, neighbourY];
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


/*
    void OnDrawGizmos()
    {
        if (map != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
                    Vector3 pos = new Vector3(-width / 2 + x + .5f, 0, -height / 2 + y + .5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }
    }
 */

}