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
    public float WiggleFrequency = 1;
    public float WiggleAmplitude = 10;
    public double NodeSeparation = 60;
    public int StampRadius = 20;
    public int StepsToDraw = 50;

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

    Dictionary<Cell, Microsoft.Msagl.Core.Layout.Node> celltoNode;

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

    Microsoft.Msagl.Core.Layout.GeometryGraph MazeToGraph(Cell[,] maze)
    {
        List<Microsoft.Msagl.Core.Layout.Node> nodes = new List<Microsoft.Msagl.Core.Layout.Node>();
        Microsoft.Msagl.Core.Layout.GeometryGraph graph = new Microsoft.Msagl.Core.Layout.GeometryGraph();
        celltoNode = new Dictionary<Cell, Microsoft.Msagl.Core.Layout.Node>();

        //Make Nodes
        foreach (Cell c in maze)
        {
            c.dijkstraVisited = false;//undo this for the wall drawing later
            //if (!c.isWall) continue;
            
            Microsoft.Msagl.Core.Layout.Node msNode = new Microsoft.Msagl.Core.Layout.Node(Microsoft.Msagl.Core.Geometry.Curves.CurveFactory.CreateRectangle(10, 10, new Microsoft.Msagl.Core.Geometry.Point()),
            c);
            graph.Nodes.Add(msNode);
            celltoNode[c] = msNode;
        }


        //Make Edges
        foreach (Cell c in maze)
        {
            if (!c.isWall) continue;
            foreach (Cell n in c.neighbors)
            {
                if (!n.isWall) continue;
                Microsoft.Msagl.Core.Layout.Node msNode = celltoNode[c];
                Microsoft.Msagl.Core.Layout.Node msNode2 = celltoNode[n];

                Microsoft.Msagl.Core.Layout.Edge e = new Microsoft.Msagl.Core.Layout.Edge(msNode, msNode2, 0, 0, 10);
                msNode.AddInEdge(e);
                msNode2.AddOutEdge(e);
                graph.Edges.Add(e);

                if (c.isWall && n.isWall)
                {
                    Microsoft.Msagl.Core.Layout.Edge e2 = new Microsoft.Msagl.Core.Layout.Edge(msNode, msNode2, 0, 0, 100);
                    msNode.AddInEdge(e2);
                    msNode2.AddOutEdge(e2);
                    graph.Edges.Add(e2);
                }
            }

        }

        //calc layout, apply positions to original TestNodeElements
        var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();
        settings.ScaleX = 10; //this.heightMap.GetLength(0);
        settings.ScaleY = 10; //this.heightMap.GetLength(1);
        settings.NodeSeparation = this.NodeSeparation;
        Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(graph, settings, null);

        // Move model to positive axis.
        graph.UpdateBoundingBox();
        graph.Translate(new Microsoft.Msagl.Core.Geometry.Point(-graph.Left, -graph.Bottom));

        //move nodes to actually be in heightmap positoins
        var scalex = this.heightMap.GetLength(0) / maze.GetLength(0);
        var scaley = this.heightMap.GetLength(1) / maze.GetLength(1);
        foreach (var n in graph.Nodes)
        {
            var newx = (0.5f + (n.UserData as Cell).location.x) * scalex;
            var newy = (0.5f + (n.UserData as Cell).location.y) * scaley;
            n.BoundingBox = new Microsoft.Msagl.Core.Geometry.Rectangle(new Microsoft.Msagl.Core.DataStructures.Size(1,1), new Microsoft.Msagl.Core.Geometry.Point(newx, newy));
        }

        return graph;
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

    void GenerateMazeTerrain( Microsoft.Msagl.Core.Layout.GeometryGraph graph, int n )
    {
        //drag a stamp across the screen until we hit all the nodes. If we aren't moving between adjacent nodes, pick it up (don't drag)
        Stack<Microsoft.Msagl.Core.Layout.Node> stack = new Stack<Microsoft.Msagl.Core.Layout.Node>();
        Stack<Microsoft.Msagl.Core.Layout.Node> lastNodes = new Stack<Microsoft.Msagl.Core.Layout.Node>();
        stack.Push(graph.Nodes[0]);
        Vector2Int stampPos = new Vector2Int((int)graph.Nodes[0].BoundingBox.Center.X, (int)graph.Nodes[0].BoundingBox.Center.Y);
        int l = 0;
        while ((n-- > 0) && stack.Count > 0)
        {
            var targetNode = stack.Peek();
            var targetCell = targetNode.UserData as Cell;

            Vector2Int targetPos = new Vector2Int((int)targetNode.BoundingBox.Center.X, (int)targetNode.BoundingBox.Center.Y);

            //once we reach target, push its neighbors and pop it.
            if (n == 0)
            {
                string target = string.Format("({0},{1})", targetCell.location.x, targetCell.location.y);
                string last = lastNodes.Count > 0 ? string.Format("({0},{1})", (lastNodes.Peek().UserData as Cell).location.x, (lastNodes.Peek().UserData as Cell).location.y) : "<NONE>";
                Debug.LogFormat("Going from {0} to {1}", last, target);
            }
            
            if (Vector2Int.Distance(stampPos, targetPos) < 10)
            {
                //stamp
                float remaining = Vector2Int.Distance(stampPos, targetPos);
                float total = (lastNodes.Count > 0) ? Vector2Int.Distance(new Vector2Int((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y), targetPos) : remaining;
                float sin_input_degrees = remaining / total * Mathf.PI;
                float wiggle_multiplier = Mathf.Sin(sin_input_degrees);
                float wiggleMagnitude = (Mathf.PerlinNoise(0, WiggleFrequency * (float)l++) - 0.5f) * WiggleAmplitude * wiggle_multiplier;
                var wiggle = Vector2.Perpendicular(targetPos - stampPos);
                wiggle.Normalize();
                wiggle *= wiggleMagnitude;

                for (var dx = -StampRadius; dx < StampRadius; dx++)
                {
                    for (var dy = -StampRadius; dy < StampRadius; dy++)
                    {
                        if (Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2) >= Mathf.Pow(StampRadius, 2)) continue; //stamp in a circle
                        Vector2Int pos = new Vector2Int(stampPos.x + dx + (int)wiggle.x, stampPos.y + dy + (int)wiggle.y);
                        pos.Clamp(Vector2Int.zero, new Vector2Int(heightMap.GetLength(0) - 1, heightMap.GetLength(1) - 1));
                        heightMap[pos.x, pos.y] = 0.002f * WallEccentricity;
                        int alpha_x = ConvertIndex(pos.x, this.heightMap.GetLength(0), this.alphaMaps.GetLength(0));
                        int alpha_y = ConvertIndex(pos.y, this.heightMap.GetLength(1), this.alphaMaps.GetLength(1));
                        alphaMaps[alpha_x, alpha_y, 1] = 0.75f;
                        alphaMaps[alpha_x, alpha_y, 0] = 0.25f;
                    }
                }

                targetCell.dijkstraVisited = true;
                bool bt = true;
                var tc = stack.Pop();
                stampPos.Set(targetPos.x, targetPos.y); //clip to target

                string target = string.Format("({0},{1})", targetCell.location.x, targetCell.location.y);
                Debug.LogFormat("Reached {0}", target);

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1) continue;
                        if (dx + targetCell.location.x >= this.MazeGenerator.GetCells().GetLength(0) || dx + targetCell.location.x < 0) continue;
                        if (dy + targetCell.location.y >= this.MazeGenerator.GetCells().GetLength(1) || dy + targetCell.location.y < 0) continue;
                        var c = this.MazeGenerator.GetCells()[dy + targetCell.location.y, dx + targetCell.location.x];

                        if (c.isWall && !c.dijkstraVisited)
                        {
                            string neighbor = string.Format("({0},{1})", c.location.x, c.location.y);
                            Debug.LogFormat("Chose neighbor {0}", neighbor);

                            stack.Push(tc);

                            lastNodes.Push(tc);
                            stack.Push(celltoNode[c]);
                            bt = false;
                            break;
                        }
                    }
                }

                //if we're out of neighbors to visit
                if (bt && lastNodes.Count > 0)
                {
                    if (stack.Count > 0)
                    {
                        string pop = string.Format("({0},{1})", (stack.Peek().UserData as Cell).location.x, (stack.Peek().UserData as Cell).location.y);
                        Debug.LogFormat("Cell {0} has no neighbors left, ploppin back to target {1} to check for ITS neighbors", target, pop);

                        var newloc = stack.Peek().BoundingBox.Center;
                        stampPos.Set((int)newloc.X, (int)newloc.Y);
                    }
                }
            }

            else
            {
                //if we're moving between adjacent cells, drag
                if (lastNodes.Count > 0 && ((lastNodes.Peek().UserData as Cell).location - targetCell.location).magnitude == 1)
                {
                    //stamp
                    float remaining = Vector2Int.Distance(stampPos, targetPos);
                    float total = Vector2Int.Distance(new Vector2Int((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y), targetPos);
                    float sin_input_degrees = remaining / total * Mathf.PI;
                    float wiggle_multiplier = Mathf.Sin(sin_input_degrees);
                    float wiggleMagnitude = (Mathf.PerlinNoise(0, WiggleFrequency * (float)l++) - 0.5f) * WiggleAmplitude * wiggle_multiplier;
                    var wiggle = Vector2.Perpendicular(targetPos - stampPos);
                    wiggle.Normalize();
                    wiggle *= wiggleMagnitude;

                    for (var dx = -StampRadius; dx < StampRadius; dx++)
                    {
                        for (var dy = -StampRadius; dy < StampRadius; dy++)
                        {
                            if (Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2) >= Mathf.Pow(StampRadius, 2)) continue; //stamp in a circle
                            Vector2Int pos = new Vector2Int(stampPos.x + dx + (int)wiggle.x, stampPos.y + dy + (int)wiggle.y);
                            pos.Clamp(Vector2Int.zero, new Vector2Int(heightMap.GetLength(0) - 1, heightMap.GetLength(1) - 1));
                            heightMap[pos.x, pos.y] = 0.002f * WallEccentricity;
                            int alpha_x = ConvertIndex(pos.x, this.heightMap.GetLength(0), this.alphaMaps.GetLength(0));
                            int alpha_y = ConvertIndex(pos.y, this.heightMap.GetLength(1), this.alphaMaps.GetLength(1));
                            alphaMaps[alpha_x, alpha_y, 1] = 1;
                            alphaMaps[alpha_x, alpha_y, 0] = 0;
                        }
                    }

                    //move
                    if (Vector2Int.Distance(stampPos, targetPos) < 11)
                    {
                        stampPos.Set(targetPos.x, targetPos.y);
                    }
                    else
                    {
                        Vector2 dir = targetPos - stampPos;
                        dir.Normalize();
                        stampPos += new Vector2Int((int)dir.x, (int)dir.y) * Mathf.Min(10, (int)remaining);
                    }
                }

                //if we're popping back in the stack bc we hit a dead ed, just tp
                else
                {
                    Debug.Log("Popped//non adjacent");
                    stampPos.Set((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y);
                }
            }

        }

    }

    void MakeGraphDebugCubes(Microsoft.Msagl.Core.Layout.GeometryGraph graph)
    {
        foreach (var n in graph.Nodes)
        {
            Debug.Log("Wall at (" + n.BoundingBox.Center.X +", "+n.BoundingBox.Center.Y + ")");
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

    void SetAlphaMapsToGrass()
    {
        for (var i = 0; i < this.alphaMaps.GetLength(0); i++)
        {
            for (var j = 0; j < this.alphaMaps.GetLength(1); j++)
            {
                this.alphaMaps[i, j, 0] = 1;
                this.alphaMaps[i, j, 1] = 0;
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


    public void RerollTerrain(int n) {
        ResetMaps();
        //Debug.Log("Heightmap size: " + heightMap.GetLength(0) + " x " + heightMap.GetLength(1));
        if (IncludeMaze) {
            //GenerateMazeTerrain(this.MazeGenerator.GetCells());
            //GenerateAlphaMaps(this.MazeGenerator.GetCells());
            //WarpHeightsAndAlphas();
            var g = MazeToGraph(this.MazeGenerator.GetCells());
            //MakeGraphDebugCubes(g);
            SetAlphaMapsToGrass();
            GenerateMazeTerrain(g, n);
        }
        //GenerateLandHeightMap();
        ApplyHeightMap();
        ApplyAlphaMaps();
    }

    // old chunky gridlike walls (for debug)
    public void ShowChunkyView()
    {
        GenerateMazeTerrain(this.MazeGenerator.GetCells());
        GenerateAlphaMaps(this.MazeGenerator.GetCells());
        ApplyHeightMap();
        ApplyAlphaMaps();
    }

    IEnumerator Trace()
    {
        var n = 0;
        while (n < StepsToDraw)
        {
            n += 5;
            RerollTerrain(n);
            yield return new WaitForSeconds(0.2f);
        }
    }

}