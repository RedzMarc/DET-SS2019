using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Realtime.Messaging.Internal;

/// <summary>
/// The world MonoBehavior is in charge of creating, updating and destroying chunks based on the player's location.
/// These mechanisms are completed with the help of Coroutines (IEnumerator methods). https://docs.unity3d.com/Manual/Coroutines.html
/// </summary>
public class CaveWorld : MonoBehaviour
{   

    public int caveWidth = 10;
    public int caveHeight = 20; 
	public int caveDepth = 20;

	public GameObject player;
	public Material textureAtlas;
	public Material fluidTexture;
	public static int columnHeight = 16;
	public static int chunkSize = 5; //ursprünglich 8
	public static int radius = 3;
	public static uint maxCoroutines = 1000;
	public static ConcurrentDictionary<string, Chunk> chunks;
	public static List<string> toRemove = new List<string>();

	public static bool firstbuild = true;

	public static CoroutineQueue queue;

	public Vector3 lastbuildPos;

    MapGiver mapGiver = new MapGiver();
	Automaton automaton = new Automaton();

    /// <summary>
    /// Creates a name for the chunk based on its position
    /// </summary>
    /// <param name="v">Position of tje chunk</param>
    /// <returns>Returns a string witht he chunk's name</returns>
	public static string BuildChunkName(Vector3 v)
	{
		return (int)v.x + "_" + 
			         (int)v.y + "_" + 
			         (int)v.z;
	}

    /// <summary>
    /// Creates a name for the column based on its position
    /// </summary>
    /// <param name="v">Position of the column</param>
    /// <returns>Returns a string witht he column's name</returns>
	public static string BuildColumnName(Vector3 v)
	{
		return (int)v.x + "_" + (int)v.z;
	}

    /// <summary>
    /// Get block based on world coordinates
    /// </summary>
    /// <param name="pos">Rough position of the block to be returned</param>
    /// <returns>Returns the block related to the input position</returns>
	public static Block GetWorldBlock(Vector3 pos)
    {
        int cx, cy, cz;

        if (pos.x < 0)
            cx = (int)((Mathf.Round(pos.x - chunkSize) + 1) / (float)chunkSize) * chunkSize;
        else
            cx = (int)(Mathf.Round(pos.x) / (float)chunkSize) * chunkSize;

        if (pos.y < 0)
            cy = (int)((Mathf.Round(pos.y - chunkSize) + 1) / (float)chunkSize) * chunkSize;
        else
            cy = (int)(Mathf.Round(pos.y) / (float)chunkSize) * chunkSize;

        if (pos.z < 0)
            cz = (int)((Mathf.Round(pos.z - chunkSize) + 1) / (float)chunkSize) * chunkSize;
        else
            cz = (int)(Mathf.Round(pos.z) / (float)chunkSize) * chunkSize;

        int blx = (int)Mathf.Abs((float)Mathf.Round(pos.x) - cx);
        int bly = (int)Mathf.Abs((float)Mathf.Round(pos.y) - cy);
        int blz = (int)Mathf.Abs((float)Mathf.Round(pos.z) - cz);

        string cn = BuildChunkName(new Vector3(cx, cy, cz));
        Chunk c;
        if (chunks.TryGetValue(cn, out c))
        {

            return c.chunkData[blx, bly, blz];
        }
        else
            return null;
    }

    /// <summary>
    /// Instantiates a new chunk at a specified location.
    /// </summary>
    /// <param name="x">y position of the chunk</param>
    /// <param name="y">y position of the chunk</param>
    /// <param name="z">z position of the chunk</param>
	private void BuildChunkAt(int x, int y, int z)
	{
		Vector3 chunkPosition = new Vector3(x*chunkSize, 
											y*chunkSize, 
											z*chunkSize);
					
		string n = BuildChunkName(chunkPosition);
		Chunk c;

		if(!chunks.TryGetValue(n, out c))
		{
			c = new Chunk(chunkPosition, textureAtlas, fluidTexture);
			c.chunk.transform.parent = this.transform;
			c.fluid.transform.parent = this.transform;
			chunks.TryAdd(c.chunk.name, c);
		}
	}

    	private void BuildChunkAt(int x, int y, int z, int[,] map)
	{
		Vector3 chunkPosition = new Vector3(x*chunkSize, 
											y*chunkSize, 
											z*chunkSize);
					
		Chunk c;
        string n = BuildChunkName(chunkPosition);

        if(!chunks.TryGetValue(n, out c))
		{
			c = new Chunk(chunkPosition, textureAtlas, fluidTexture, map);
			c.chunk.transform.parent = this.transform;
			c.fluid.transform.parent = this.transform;
			chunks.TryAdd(c.chunk.name, c);
		}	
	}

	private void BuildChunkAt(int x, int y, int z, int[,,] map)
	{
		Vector3 chunkPosition = new Vector3(x*chunkSize, 
											y*chunkSize, 
											z*chunkSize);
					
		Chunk c;
        string n = BuildChunkName(chunkPosition);

        if(!chunks.TryGetValue(n, out c))
		{
			c = new Chunk(chunkPosition, textureAtlas, fluidTexture, map);
			c.chunk.transform.parent = this.transform;
			c.fluid.transform.parent = this.transform;
			chunks.TryAdd(c.chunk.name, c);
		}	
	}

    /// <summary>
    /// Coroutine to to recursively build chunks of the world depending on some location and a radius.
    /// </summary>
    /// <param name="x">x position</param>
    /// <param name="y">y position</param>
    /// <param name="z">z position</param>
    /// <param name="startrad">Starting radius (is necessary for recursive calls of this function)</param>
    /// <param name="rad">Desired radius</param>
    /// <returns></returns>
	IEnumerator BuildRecursiveWorld(int x, int y, int z, int startrad, int rad)
	{
		int nextrad = rad-1;
		if(rad <= 0 || y < 0 || y > columnHeight) yield break;
		// Build chunk front
		BuildChunkAt(x,y,z+1);
		queue.Run(BuildRecursiveWorld(x,y,z+1,rad,nextrad));
		yield return null;

		// Build chunk back
		BuildChunkAt(x,y,z-1);
		queue.Run(BuildRecursiveWorld(x,y,z-1,rad,nextrad));
		yield return null;
		
		// Build chunk left
		BuildChunkAt(x-1,y,z);
		queue.Run(BuildRecursiveWorld(x-1,y,z,rad,nextrad));
		yield return null;

		// Build chunk right
		BuildChunkAt(x+1,y,z);
		queue.Run(BuildRecursiveWorld(x+1,y,z,rad,nextrad));
		yield return null;
		
		// Build chunk up
		BuildChunkAt(x,y+1,z);
		queue.Run(BuildRecursiveWorld(x,y+1,z,rad,nextrad));
		yield return null;
		
		// Build chunk down
		BuildChunkAt(x,y-1,z);
		queue.Run(BuildRecursiveWorld(x,y-1,z,rad,nextrad));
		yield return null;
	}

    /// <summary>
    /// Coroutine to render chunks that are in the DRAW state. Adds chunks to the toRemove list, which are outside the player's radius.
    /// </summary>
    /// <returns></returns>
	IEnumerator DrawChunks()
	{
		toRemove.Clear();
		foreach(KeyValuePair<string, Chunk> c in chunks)
		{
			if(c.Value.status == Chunk.ChunkStatus.DRAW) 
			{
				c.Value.DrawChunk();
			}
			if(c.Value.chunk && Vector3.Distance(player.transform.position,
								c.Value.chunk.transform.position) > radius*chunkSize)
				toRemove.Add(c.Key);

			yield return null;
		}
	}

    /// <summary>
    /// Coroutine to save and then to unload unused chunks.
    /// </summary>
    /// <returns></returns>
	IEnumerator RemoveOldChunks()
	{
		for(int i = 0; i < toRemove.Count; i++)
		{
			string n = toRemove[i];
			Chunk c;
			if(chunks.TryGetValue(n, out c))
			{
				Destroy(c.chunk);
				c.Save();
				chunks.TryRemove(n, out c);
				yield return null;
			}
		}
	}

    /// <summary>
    /// Builds chunks that are inside the player's radius.
    /// </summary>
	public void BuildNearPlayer()
	{
        // Stop the coroutine of building the world, because it is getting replaced
		StopCoroutine("BuildRecursiveWorld");
		queue.Run(BuildRecursiveWorld((int)(player.transform.position.x/chunkSize),
											(int)(player.transform.position.y/chunkSize),
											(int)(player.transform.position.z/chunkSize), radius, radius));
	}

	/// <summary>
    /// Unity lifecycle start method. Initializes the world and its first chunk and triggers the building of further chunks.
    /// Player is disabled during Start() to avoid him falling through the floor. Chunks are built using coroutines.
    /// </summary>
	void Start ()
    {
		Vector3 ppos = player.transform.position;
		player.transform.position = new Vector3(ppos.x,
											Utils.GenerateHeight(ppos.x,ppos.z) + 1,
											ppos.z);
		lastbuildPos = player.transform.position;
		player.SetActive(false);

		firstbuild = true;
		chunks = new ConcurrentDictionary<string, Chunk>();
		this.transform.position = Vector3.zero;
		this.transform.rotation = Quaternion.identity;	

        World.chunks = chunks;
		queue = new CoroutineQueue(maxCoroutines, StartCoroutine);

        mapGiver.width = caveWidth;
        mapGiver.height = caveHeight;
        mapGiver.GenerateNewMap();
        int[,] caveMap = mapGiver.currentMap;
		int[,,] cave = new int[caveWidth,caveHeight,chunkSize]; 

		for (int i = 0; i < caveWidth; i++)
		{
			for (int j = 0; j < caveHeight; j++)
			{
				for (int k = 0; k < chunkSize; k++)
				{
					cave[i,j,k] = caveMap[i,j];
				}
			}
		}

        for (int i = 0; i < caveWidth / chunkSize; i++)
        {
            for (int j = 0; j < caveHeight / chunkSize; j++)
            {
				for (int k = 0; k < chunkSize / chunkSize; k++)
				{
                int[,,] chunckMap = new int[chunkSize,chunkSize,chunkSize];
					for (int l = 0; l < chunkSize; l++)
					{
						for (int m = 0; m < chunkSize; m++)
						{
							for(int n = 0; n < chunkSize; n++){
								chunckMap[l,m,n] = cave[i*chunkSize+l,j*chunkSize+m,k*chunkSize+n];
							}
						}
					}
                BuildChunkAt(i,j,k,chunckMap);
				}
            }
        }

		queue.Run(DrawChunks());

		/*
		automaton.CreateNew();
		int[,,] cave3D = automaton.cells;


		for (int i = 0; i < caveWidth / chunkSize; i++)
        {
            for (int j = 0; j < caveHeight / chunkSize; j++)
            {
				for (int m = 0; m < caveDepth / chunkSize; m++)
				{
					int[,,] chunckMap = new int[chunkSize,chunkSize,chunkSize];
					for (int k = 0; k < chunkSize; k++)
					{
						for (int l = 0; l < chunkSize; l++)
						{
							for (int n = 0; n < chunkSize; n++)
							{
								
								chunckMap[k,l,n] = cave3D[i*chunkSize+k,j*chunkSize+l,m*chunkSize+n];
							}
						}
					}

                	BuildChunkAt(i,j,m,chunckMap);
				}
            }
        }
		*/

	}
	
    /// <summary>
    /// Unity lifecycle update method. Actviates the player's GameObject. Updates chunks based on the player's position.
    /// </summary>

	void Update ()
    {
        // Determine whether to build/load more chunks around the player's location
		Vector3 movement = lastbuildPos - player.transform.position;

		if(true)//movement.magnitude > chunkSize )
		{
			lastbuildPos = player.transform.position;
			BuildNearPlayer();
		}

        // Activate the player's GameObject
		if(!player.activeSelf)
		{
			player.SetActive(true);	
			firstbuild = false;
		}

        // Draw new chunks and removed deprecated chunks
		queue.Run(DrawChunks());
		queue.Run(RemoveOldChunks());
	}
}
