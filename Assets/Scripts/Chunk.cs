using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

/// <summary>
/// The serializable BlockData class contains block information that are to be saved to a file.
/// </summary>
[Serializable]
class BlockData
{
	public Block.BlockType[,,] matrix;
	
    /// <summary>
    /// Empty constructor
    /// </summary>
	public BlockData(){}

    /// <summary>
    /// The constrcutor initializes its matrix for storing all blocks of the given chunk.
    /// </summary>
    /// <param name="b">3D block array (i.e. chunk)</param>
	public BlockData(Block[,,] b)
	{
		matrix = new Block.BlockType[World.chunkSize,World.chunkSize,World.chunkSize];
		for(int z = 0; z < World.chunkSize; z++)
			for(int y = 0; y < World.chunkSize; y++)
				for(int x = 0; x < World.chunkSize; x++)
				{
					matrix[x,y,z] = b[x,y,z].blockType;
				}
	}
}

/// <summary>
/// Chunk class that takes care of storing the information of the chunk's blocks.
/// It renders the chunk and provides functionality for saving, loading and updating the chunk.
/// </summary>
public class Chunk
{
	public Material cubeMaterial;   // Materia for solid blocks
	public Material fluidMaterial;  // Material for transparent blocks
	public Block[,,] chunkData;     // 3D Array containing all blocks of the chunk
	public GameObject chunk;        // GameObject that holds the mesh of the solid parts of the chunk
	public GameObject fluid;        // GameObject that holds the mesh of the transparent parts, like water, of the chunk
	public enum ChunkStatus
    {
        DRAW,                       // DRAW: data of the chunk has been created and needs to be rendered next
        DONE                        // DONE: Trees have been built and the chunk has been rendered
    };
	public ChunkStatus status;      // Current state of the chunk
	public ChunkMB mb;              // The MonoBehaviour of the Chunk
	BlockData bd;                   // 
	public bool changed = false;    // If a chunk got modified (e.g. a block got destroyed by the player), set this to true to redraw the chunk upon the next update.
	bool treesCreated = false;      // 

    /// <summary>
    /// Creates a file name for the to be saved or loaded chunk based on its position. On Windows machines the data is saved in AppData\LocalLow\DefaultCompany.
    /// </summary>
    /// <param name="v">Position of the chunk</param>
    /// <returns>Returns the file name of the to be saved or loaded chunk</returns>
	string BuildChunkFileName(Vector3 v)
	{
		return Application.persistentDataPath + "/savedata/Chunk_" + 
								(int)v.x + "_" +
									(int)v.y + "_" +
										(int)v.z + 
										"_" + World.chunkSize +
										"_" + World.radius +
										".dat";
	}

    /// <summary>
    /// Loads chunk data from file.
    /// </summary>
    /// <returns>Returns true if the file to be loaded exists</returns>
	private bool Load()
	{
		string chunkFile = BuildChunkFileName(chunk.transform.position);
		if(File.Exists(chunkFile))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(chunkFile, FileMode.Open);
			bd = new BlockData();
			bd = (BlockData) bf.Deserialize(file);
			file.Close();
			return true;
		}
		return false;
	}

    /// <summary>
    /// Writes chunk data to file.
    /// </summary>
	public void Save()
	{
		string chunkFile = BuildChunkFileName(chunk.transform.position);
		
		if(!File.Exists(chunkFile))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(chunkFile));
		}
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Open(chunkFile, FileMode.OpenOrCreate);
		bd = new BlockData(chunkData);
		bf.Serialize(file, bd);
		file.Close();
	}

    /// <summary>
    /// If a block was destroyed upon player interaction, trigger the process of dropping sand for each sand block.
    /// </summary>
	public void UpdateChunk()
	{
		for(int z = 0; z < World.chunkSize; z++)
			for(int y = 0; y < World.chunkSize; y++)
				for(int x = 0; x < World.chunkSize; x++)
				{
					if(chunkData[x,y,z].blockType == Block.BlockType.SAND)
					{
						mb.StartCoroutine(mb.Drop(chunkData[x,y,z], 
										Block.BlockType.SAND, 
										20));
					}
				}
	}

    /// <summary>
    /// Builds the chunk from scatch or loads it from file. This functions does not draw the chunk.
    /// </summary>
	private void BuildChunk()
	{
		bool dataFromFile = false;
        // Commented load functionality, because this may cause issues while changing the underlying code (saved files may not represent the current state of the project)
        //dataFromFile = Load();

        chunkData = new Block[World.chunkSize,World.chunkSize,World.chunkSize];
		for(int z = 0; z < World.chunkSize; z++)
			for(int y = 0; y < World.chunkSize; y++)
				for(int x = 0; x < World.chunkSize; x++)	
				{
					Vector3 pos = new Vector3(x,y,z);
					int worldX = (int)(x + chunk.transform.position.x);
					int worldY = (int)(y + chunk.transform.position.y);
					int worldZ = (int)(z + chunk.transform.position.z);

                    // Load chunk from file
					if(dataFromFile)
					{
						chunkData[x,y,z] = new Block(bd.matrix[x, y, z], pos, 
						                chunk.gameObject, this);
						continue;
					}

					int surfaceHeight = Utils.GenerateHeight(10,10);  //(worldX,worldZ);
					
                    // Place bedrock at height 0
					if(worldY == 0)
						chunkData[x,y,z] = new Block(Block.BlockType.BEDROCK, pos, 
						                chunk.gameObject, this);


				
                    /* Place Diamond, Redstone or Stone at certain heights and probabilities
					else if(worldY <= Utils.GenerateStoneHeight(worldX,worldZ))
					{
						if(Utils.fBM3D(worldX, worldY, worldZ, 0.01f, 2) < 0.4f && worldY < 40)
							chunkData[x,y,z] = new Block(Block.BlockType.DIAMOND, pos, 
						                chunk.gameObject, this);
						else if(Utils.fBM3D(worldX, worldY, worldZ, 0.03f, 3) < 0.41f && worldY < 20)
							chunkData[x,y,z] = new Block(Block.BlockType.REDSTONE, pos, 
						                chunk.gameObject, this);
						else
							chunkData[x,y,z] = new Block(Block.BlockType.STONE, pos, 
						                chunk.gameObject, this);
					}
					*/

                    //Place trunks of a tree or grass blocks on the surface
					else if(worldY == surfaceHeight)
					{
						/*
						if(Utils.fBM3D(worldX, worldY, worldZ, 0.4f, 2) < 0.4f)
							chunkData[x,y,z] = new Block(Block.BlockType.WOODBASE, pos, 
						                chunk.gameObject, this);
						else
						*/
							System.Random pseudoRandom = new System.Random(Time.time.ToString().GetHashCode());

							if((pseudoRandom.Next(0, 100) < 50)){
								chunkData[x,y,z] = new Block(Block.BlockType.BEDROCK, pos, chunk.gameObject, this);
							}else{
								chunkData[x,y,z] = new Block(Block.BlockType.SAND, pos, chunk.gameObject, this);
							}
					}
					


                    // Place dirt blocks
					else if(worldY < surfaceHeight)
						chunkData[x,y,z] = new Block(Block.BlockType.DIRT, pos, 
						                chunk.gameObject, this);
                    /*  Place water blocks below height 65
					else if(worldY < 65)
						chunkData[x,y,z] = new Block(Block.BlockType.WATER, pos, 
						                fluid.gameObject, this);
					*/
                    // Place air blocks
					else
					{
						chunkData[x,y,z] = new Block(Block.BlockType.AIR, pos, 
						                chunk.gameObject, this);
					}

                    /* Create caves
					if(chunkData[x,y,z].blockType != Block.BlockType.WATER && Utils.fBM3D(worldX, worldY, worldZ, 0.1f, 3) < 0.42f)
						chunkData[x,y,z] = new Block(Block.BlockType.AIR, pos, 
						                chunk.gameObject, this);
					
					status = ChunkStatus.DRAW;
					*/
				}
	}



private void BuildChunk(int[,] map)
	{
        chunkData = new Block[World.chunkSize,World.chunkSize,World.chunkSize];
		for(int z = 0; z < World.chunkSize; z++)
			for(int y = 0; y < World.chunkSize; y++)
				for(int x = 0; x < World.chunkSize; x++)	
				{
					Vector3 pos = new Vector3(x,y,z);
					int worldX = (int)(x + chunk.transform.position.x);
					int worldY = (int)(y + chunk.transform.position.y);
					int worldZ = (int)(z + chunk.transform.position.z);


					int surfaceHeight = Utils.GenerateHeight(10,10);  //(worldX,worldZ);
					
					chunkData[x,y,z] = new Block( ((map[x,y] == 0)? Block.BlockType.AIR : Block.BlockType.DIRT), pos, chunk.gameObject, this);

				}
	}


private void BuildChunk(int[,,] map)
	{
        chunkData = new Block[World.chunkSize,World.chunkSize,World.chunkSize];
		for(int z = 0; z < World.chunkSize; z++)
			for(int y = 0; y < World.chunkSize; y++)
				for(int x = 0; x < World.chunkSize; x++)	
				{
					Vector3 pos = new Vector3(x,y,z);
					int worldX = (int)(x + chunk.transform.position.x);
					int worldY = (int)(y + chunk.transform.position.y);
					int worldZ = (int)(z + chunk.transform.position.z);


					int surfaceHeight = Utils.GenerateHeight(10,10);  //(worldX,worldZ);
					
					chunkData[x,y,z] = new Block( ((map[x,y,z] == 0)? Block.BlockType.AIR : Block.BlockType.DIRT), pos, chunk.gameObject, this);

				}
	}


    /// <summary>
    /// Redraws this chunk by destroying all mesh and collision components and then creating new ones.
    /// </summary>
	public void Redraw()
	{
		GameObject.DestroyImmediate(chunk.GetComponent<MeshFilter>());
		GameObject.DestroyImmediate(chunk.GetComponent<MeshRenderer>());
		GameObject.DestroyImmediate(chunk.GetComponent<Collider>());
		GameObject.DestroyImmediate(fluid.GetComponent<MeshFilter>());
		GameObject.DestroyImmediate(fluid.GetComponent<MeshRenderer>());
		GameObject.DestroyImmediate(fluid.GetComponent<Collider>());
		DrawChunk();
	}

    /// <summary>
    /// Draws the chunk. If trees are not created yet, create them.
    /// The draw process creates meshes for all blocks and then combines them to a solid and a transparent mesh.
    /// </summary>
	public void DrawChunk()
	{
		if(!treesCreated)
		{
			for(int z = 0; z < World.chunkSize; z++)
				for(int y = 0; y < World.chunkSize; y++)
					for(int x = 0; x < World.chunkSize; x++)
					{
						BuildTrees(chunkData[x,y,z],x,y,z);
					}
			treesCreated = true;		
		}
		for(int z = 0; z < World.chunkSize; z++)
			for(int y = 0; y < World.chunkSize; y++)
				for(int x = 0; x < World.chunkSize; x++)
				{
					chunkData[x,y,z].Draw();
				}

        // Prepare solid chunk mesh
		CombineQuads(chunk.gameObject, cubeMaterial);
		MeshCollider collider = chunk.gameObject.AddComponent(typeof(MeshCollider)) as MeshCollider;
		collider.sharedMesh = chunk.transform.GetComponent<MeshFilter>().mesh;

        // Prepare transparent chunk mesh
		CombineQuads(fluid.gameObject, fluidMaterial);

		status = ChunkStatus.DONE;
	}

    /// <summary>
    /// Trunks are already place within the BuildChunk method.
    /// For each trunk, build a tree.
    /// </summary>
    /// <param name="trunk">Woodbase block as a trunk of the to be created tree</param>
    /// <param name="x">x position of the block</param>
    /// <param name="y">y position of the block</param>
    /// <param name="z">z position of the block</param>
	private void BuildTrees(Block trunk, int x, int y, int z)
	{
        // Do not build a tree if there is no woodbase
		if(trunk.blockType != Block.BlockType.WOODBASE) return;

		Block t = trunk.GetBlock(x, y+1, z);
		if(t != null)
		{
			t.SetType(Block.BlockType.WOOD);		
		    Block t1 = t.GetBlock(x, y+2, z);
		    if(t1 != null)
		    {
			    t1.SetType(Block.BlockType.WOOD);

				for(int i = -1; i <= 1; i++)
					for(int j = -1; j <= 1; j++)
						for(int k = 3; k <= 4; k++)
					{
						Block t2 = trunk.GetBlock(x+i, y+k, z+j);

						if(t2 != null)
						{
							t2.SetType(Block.BlockType.LEAVES);
						}
						else return;
					}
				Block t3 = t1.GetBlock(x, y+5, z);
				if(t3 != null)
				{
					t3.SetType(Block.BlockType.LEAVES);
				}
			}
		}
	}

    /// <summary>
    /// Empty constructor.
    /// </summary>
	public Chunk(){}

	/// <summary>
    /// Initializes a chunk by providing a position, a material for blocks and a material for partially transparent blocks.
    /// </summary>
    /// <param name="position">Position of the chunk</param>
    /// <param name="c">The material for the solid blocks of the chunk</param>
    /// <param name="t">The material for the transparent blocks of the chunk</param>
	public Chunk (Vector3 position, Material c, Material t)
    {
        // Create GameObjects holding the chunk's meshes
		chunk = new GameObject(World.BuildChunkName(position));         // solid chunk mesh, e.g. dirt blocks
		chunk.transform.position = position;
		fluid = new GameObject(World.BuildChunkName(position)+"_F");    // transparent chunk mesh, e.g. water blocks
		fluid.transform.position = position;

		mb = chunk.AddComponent<ChunkMB>();                             // Adds the chunk's Monobehaviour
		mb.SetOwner(this);
		cubeMaterial = c;
		fluidMaterial = t;
		BuildChunk();                                                   // Start building the chunk
	}

		public Chunk (Vector3 position, Material c, Material t, int[,] map)
    {
        // Create GameObjects holding the chunk's meshes
		chunk = new GameObject(World.BuildChunkName(position));         // solid chunk mesh, e.g. dirt blocks
		chunk.transform.position = position;
		fluid = new GameObject(World.BuildChunkName(position)+"_F");    // transparent chunk mesh, e.g. water blocks
		fluid.transform.position = position;

		mb = chunk.AddComponent<ChunkMB>();                             // Adds the chunk's Monobehaviour
		mb.SetOwner(this);
		cubeMaterial = c;
		fluidMaterial = t;
		BuildChunk(map);                                                   // Start building the chunk
	}
	
		public Chunk (Vector3 position, Material c, Material t, int[,,] map)
    {
        // Create GameObjects holding the chunk's meshes
		chunk = new GameObject(World.BuildChunkName(position));         // solid chunk mesh, e.g. dirt blocks
		chunk.transform.position = position;
		fluid = new GameObject(World.BuildChunkName(position)+"_F");    // transparent chunk mesh, e.g. water blocks
		fluid.transform.position = position;

		mb = chunk.AddComponent<ChunkMB>();                             // Adds the chunk's Monobehaviour
		mb.SetOwner(this);
		cubeMaterial = c;
		fluidMaterial = t;
		BuildChunk(map);                                                   // Start building the chunk
	}

	public void CombineQuads(GameObject o, Material m)
	{
		// 1. Combine all children meshes
		MeshFilter[] meshFilters = o.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
        int i = 0;
        while (i < meshFilters.Length) {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            i++;
        }

        // 2. Create a new mesh on the parent object
        MeshFilter mf = (MeshFilter) o.gameObject.AddComponent(typeof(MeshFilter));
        mf.mesh = new Mesh();

        // 3. Add combined meshes on children as the parent's mesh
        mf.mesh.CombineMeshes(combine);

        // 4. Create a renderer for the parent
		MeshRenderer renderer = o.gameObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;
		renderer.material = m;

		// 5. Delete all uncombined children
		foreach (Transform quad in o.transform) {
     		GameObject.Destroy(quad.gameObject);
 		}
	}
}
