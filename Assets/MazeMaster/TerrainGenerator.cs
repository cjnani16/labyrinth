using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ System.Serializable ]
public class TerrainGenerator : MonoBehaviour
{
    [Header("References")]
    public Terrain TerrainMain;
    public MazeGenerator MazeGenerator;

    //General Settings
    [Header("General Settings")]
    public bool debug = true;
    public bool autoupdate = true;
    public bool IncludeMaze = true;

    //Settings For Maze Terrain Generation
    [Header("Maze Terrain Gen Settings")]
    public float PathEccentricity = 5;
    public float WallEccentricity = 10;
    public float WallRefinement = 0.01f;
    public float WallAmplitude = 20.0f;

    //Settings for Land Generation
    [Header("Land Gen Settings")]
    public float HillsRefinement = 0.5f;
    public float HillsAmplitude = 2.0f;
    public int NumOctaves = 3;
    public float Lacunarity = 2.0f;
    public float Persistence = 0.5f;

    //Terrain Data
    float[,] heightMap;
    float[,,] alphaMaps;
    float PerlinSeed;

    //Helper Function
    int ConvertIndex(int x, int max, int othermax) {
        return Mathf.FloorToInt( Mathf.Clamp( ( (float) x / max) * othermax, 0, othermax - 1 ) );
    }

    void ResetMaps() {
        this.heightMap = new float[TerrainMain.terrainData.heightmapResolution,TerrainMain.terrainData.heightmapResolution];
        this.alphaMaps = new float [TerrainMain.terrainData.alphamapWidth,TerrainMain.terrainData.alphamapHeight, 2 ];
        this.PerlinSeed = Random.Range(-10000.0f, 10000.0f);
    }

    //Heightmap Interfaces
    void LoadHeightMap() {
        this.heightMap = TerrainMain.terrainData.GetHeights( 0, 0, TerrainMain.terrainData.heightmapResolution,TerrainMain.terrainData.heightmapResolution );
        if (debug) Debug.Log("Heightmaps have size: "+this.heightMap.GetLength(0)+", "+this.heightMap.GetLength(1)+".");
    }

    void ApplyHeightMap() {
        TerrainMain.terrainData.SetHeights( 0, 0, this.heightMap ) ;
    }

    //Maze Heightmap Generation
    void GenerateMazeTerrain( Cell [,] maze ) {
        for (var i = 0; i < this.heightMap.GetLength(0); i++) {
            for (var j = 0; j < this.heightMap.GetLength(1); j++) {
                int y = ConvertIndex(i, this.heightMap.GetLength(0), maze.GetLength(0));
                int x = ConvertIndex(j, this.heightMap.GetLength(1), maze.GetLength(1));
                Cell c  = maze[y,x];

                this.heightMap[i,j] = 0.01f * c.age * PathEccentricity;

                if (c.isWall) {

                    //Find the height of this cells tallest neighbor
                    float max_floor_height = 0;
                    for (int dx = -1; dx<=1; dx++) {
                        for (int dy=-1; dy<=1; dy++) {
                            if ( (dx==0&&dy==0) || (c.location.x+dx<0 || c.location.x+dx>=maze.GetLength(1)) || (c.location.y+dy<0 || c.location.y+dy>=maze.GetLength(0)) ) continue;

                            Cell otherCell = maze[c.location.y+dy,c.location.x+dx];

                            if (!otherCell.isWall) {
                                max_floor_height = Mathf.Max(max_floor_height, 0.01f * otherCell.age * PathEccentricity);
                            }
                        }
                    }
                    this.heightMap[i,j] = max_floor_height + 0.002f * WallEccentricity;
                }
            }
        }
    }

    //Alphamap Interfaces
    void LoadAlphaMaps () {
        this.alphaMaps = TerrainMain.terrainData.GetAlphamaps(0,0,TerrainMain.terrainData.alphamapWidth,TerrainMain.terrainData.alphamapHeight);
        if (debug) Debug.Log("Aplhas have size: "+this.alphaMaps.GetLength(0)+", "+this.alphaMaps.GetLength(1)+","+this.alphaMaps.GetLength(2));
    }
    void ApplyAlphaMaps () {
        TerrainMain.terrainData.SetAlphamaps(0,0,this.alphaMaps);
    }
    
    //Generate Texturemaps based on Maze Walls (Probably not needed after I have custom maze wall meshes)
    void GenerateAlphaMaps (Cell [,] maze) {
        for (var i = 0; i < this.alphaMaps.GetLength(0); i++) {
            for (var j = 0; j < this.alphaMaps.GetLength(1); j++) {
                int y = ConvertIndex(i, this.alphaMaps.GetLength(0), maze.GetLength(0));
                int x = ConvertIndex(j, this.alphaMaps.GetLength(1), maze.GetLength(1));
                Cell c  = maze[y,x];

                this.alphaMaps[i,j,0] = c.isWall? 0 : 1;
                this.alphaMaps[i,j,1] = c.isWall? 1 : 0;
            }
        }
    }

    //Lateral Warping for Wiggly Maze Walls -- Heights and Alphas must be synced here in their noise sampling so they're all together.
    void WarpHeightsAndAlphas() {
        int rows = this.heightMap.GetLength(0);
        int cols = this.heightMap.GetLength(1);

        float [,] newHeightMap = new float [rows,cols];
        float [,,] oldAlphaMaps = this.alphaMaps;

        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {

                int offset_x = Mathf.RoundToInt( WallAmplitude * Mathf.PerlinNoise(PerlinSeed + i * WallRefinement, 0.69f));
                int offset_y = Mathf.RoundToInt( WallAmplitude * Mathf.PerlinNoise(PerlinSeed + 0.69f, j * WallRefinement));

                int source_i = (i + offset_y) % rows;
                int source_j = (j + offset_x) % cols;

                int alpha_i = ConvertIndex(i, rows, alphaMaps.GetLength(0));
                int alpha_j = ConvertIndex(j, cols, alphaMaps.GetLength(1));
                int alpha_source_i = ConvertIndex(source_i, rows, alphaMaps.GetLength(0));
                int alpha_source_j = ConvertIndex(source_j, cols, alphaMaps.GetLength(1));

                if ((i + offset_y) < rows && (j + offset_x) < cols && (i + offset_y) >= 0 && (j + offset_x) >= 0) {

                    newHeightMap[i,j] = this.heightMap[ source_i, source_j ];
                    this.alphaMaps[alpha_i,alpha_j,0] = oldAlphaMaps[ alpha_source_i, alpha_source_j, 0 ];
                    this.alphaMaps[alpha_i,alpha_j,1] = oldAlphaMaps[ alpha_source_i, alpha_source_j, 1 ];

                } else {

                    newHeightMap[i,j] = 0;
                    this.alphaMaps[alpha_i,alpha_j,0] = 0;
                    this.alphaMaps[alpha_i,alpha_j,1] = 1;

                }
            }
        }

        this.heightMap = newHeightMap;
    }

    //Underlying Naturalistic Heightmap Generation
    void GenerateLandHeightMap() {
        int rows = this.heightMap.GetLength(0);
        int cols = this.heightMap.GetLength(1);

        float [,] newHeightMap = new float [ rows, cols];
        for (int i = 0; i < rows; i++) {
            for (int j = 0; j < cols; j++) {
                float normalized = 0;
                

                for (int o = 0; o < NumOctaves; o++) {
                    normalized += (o==0?0f:-0.5f * HillsAmplitude * Mathf.Pow(Persistence, o) ) + Mathf.Clamp( Mathf.Pow(Persistence, o) * HillsAmplitude * Mathf.PerlinNoise(PerlinSeed +  i * HillsRefinement * Mathf.Pow(Lacunarity, o),  j * HillsRefinement * Mathf.Pow(Lacunarity, o) ), 0, 1 );
                }

                newHeightMap[i,j] = this.heightMap[i,j] + normalized;
            }
        }

        this.heightMap = newHeightMap;
    }

    //Detail Densitymap Interfaces 
    //TODO: this needs to be warped as well. ugh. not worth...
    void SetGrass(Cell [,] maze) {
        // Get all of layer zero.
        var map = TerrainMain.terrainData.GetDetailLayer(0, 0, TerrainMain.terrainData.detailWidth, TerrainMain.terrainData.detailHeight, 0);
        Debug.Log("Detail Map Dims:"+TerrainMain.terrainData.detailWidth+"x"+TerrainMain.terrainData.detailHeight);

        // For each pixel in the detail map...
        for (var y = 0; y < TerrainMain.terrainData.detailHeight; y++)
        {
            for (var x = 0; x < TerrainMain.terrainData.detailWidth; x++)
            {
                // If the heightmap value is above the threshold then
                // set density there to 0
                if (maze[ConvertIndex(x, TerrainMain.terrainData.detailWidth, maze.GetLength(0)), ConvertIndex(y, TerrainMain.terrainData.detailHeight, maze.GetLength(1))].isWall)
                {
                    map[x, y] = 0;
                }
                else
                {
                    map[x,y] = 6;
                }
            }
        }

        // Assign the modified map back.
        TerrainMain.terrainData.SetDetailLayer(0, 0, 0, map);
    }


    public void RerollTerrain() {
        ResetMaps();
        if (IncludeMaze) {
            GenerateMazeTerrain(this.MazeGenerator.GetCells());
            GenerateAlphaMaps(this.MazeGenerator.GetCells());
            WarpHeightsAndAlphas();
        }
        GenerateLandHeightMap();
        ApplyHeightMap();
        ApplyAlphaMaps();
    }
}
