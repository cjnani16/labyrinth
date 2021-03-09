﻿using System.Collections;using System.Collections.Generic;using UnityEngine;using System.Linq;[ System.Serializable ]public class TerrainGenerator : MonoBehaviour{    [Header("References")]    public Terrain TerrainMain;    public MazeGenerator MazeGenerator;    //General Settings    [Header("General Settings")]    public bool debug = true;    public bool autoupdate = true;    public bool IncludeMaze = true;    public bool RealTime = false;    //Settings For Maze Terrain Generation    [Header("Maze Terrain Gen Settings")]    //public float PathEccentricity = 5;    //public float WallEccentricity = 10;    //public float WallRefinement = 0.01f;    public bool NodeWiggle = true;    public bool SineSmoothing = true;    public float WallHeight = 20.0f;    public float WiggleFrequency = 1;    public float WiggleAmplitude = 10;    public double NodeSeparation = 60;    public int StampRadius = 20;    public int StepsToDraw = 50;    public int StepSize = 1;    //Settings for Land Generation    [Header("Land Gen Settings")]    public bool IncludeLand = true;    public float HillsRefinement = 0.5f;    public float HillsAmplitude = 2.0f;    public int NumOctaves = 3;    public float Lacunarity = 2.0f;    public float Persistence = 0.5f;    public float MinGrassDensity = 4;    public float MaxGrassDensity = 20;    public float GrassFreq = 0.001f;    public float TextureWiggle = 0.01f;    public Gradient terrainTypes;    //Terrain Data    float[,] heightMap;    float[,,] alphaMaps;    float PerlinSeed;    Dictionary<Cell, Microsoft.Msagl.Core.Layout.Node> celltoNode;    //Helper Function    int ConvertIndex(int x, int max, int othermax) {        return Mathf.FloorToInt( Mathf.Clamp( ( (float) x / max) * othermax, 0, othermax - 1 ) );    }    void ResetMaps() {        this.heightMap = new float[TerrainMain.terrainData.heightmapResolution,TerrainMain.terrainData.heightmapResolution];        this.alphaMaps = new float [TerrainMain.terrainData.alphamapWidth,TerrainMain.terrainData.alphamapHeight, TerrainMain.terrainData.alphamapLayers ];        this.PerlinSeed = Random.Range(-10000.0f, 10000.0f);    }    //Heightmap Interfaces    void LoadHeightMap() {        this.heightMap = TerrainMain.terrainData.GetHeights( 0, 0, TerrainMain.terrainData.heightmapResolution,TerrainMain.terrainData.heightmapResolution );        if (debug) Debug.Log("Heightmaps have size: "+this.heightMap.GetLength(0)+", "+this.heightMap.GetLength(1)+".");    }    void ApplyHeightMap() {        TerrainMain.terrainData.SetHeights( 0, 0, this.heightMap ) ;    }    Microsoft.Msagl.Core.Layout.GeometryGraph MazeToGraph(Cell[,] maze)    {        List<Microsoft.Msagl.Core.Layout.Node> nodes = new List<Microsoft.Msagl.Core.Layout.Node>();        Microsoft.Msagl.Core.Layout.GeometryGraph graph = new Microsoft.Msagl.Core.Layout.GeometryGraph();        celltoNode = new Dictionary<Cell, Microsoft.Msagl.Core.Layout.Node>();        //Make Nodes        foreach (Cell c in maze)        {            c.dijkstraVisited = false;//undo this for the wall drawing later            //if (!c.isWall) continue;                        Microsoft.Msagl.Core.Layout.Node msNode = new Microsoft.Msagl.Core.Layout.Node(Microsoft.Msagl.Core.Geometry.Curves.CurveFactory.CreateRectangle(10, 10, new Microsoft.Msagl.Core.Geometry.Point()),            c);            graph.Nodes.Add(msNode);            celltoNode[c] = msNode;        }        //Make Edges        foreach (Cell c in maze)        {            //if (!c.isWall) continue;            for (int dx = -1; dx <= 1; dx++)            {                for (int dy = -1; dy <= 1; dy++)                {                    if (dx == 0 && dy == 0) continue;                    if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1) continue;                    if (dx + c.location.x >= this.MazeGenerator.GetCells().GetLength(0) || dx + c.location.x < 0) continue;                    if (dy + c.location.y >= this.MazeGenerator.GetCells().GetLength(1) || dy + c.location.y < 0) continue;                    var n = this.MazeGenerator.GetCells()[dy + c.location.y, dx + c.location.x];                    //if (!n.isWall) continue;                    Microsoft.Msagl.Core.Layout.Node msNode = celltoNode[c];                    Microsoft.Msagl.Core.Layout.Node msNode2 = celltoNode[n];                    Microsoft.Msagl.Core.Layout.Edge e = new Microsoft.Msagl.Core.Layout.Edge(msNode, msNode2, 0, 0, 10);                    msNode.AddInEdge(e);                    msNode2.AddOutEdge(e);                    graph.Edges.Add(e);                    if (n.isWall && c.isWall)                    {                        Microsoft.Msagl.Core.Layout.Edge e2 = new Microsoft.Msagl.Core.Layout.Edge(msNode, msNode2, 0, 0, 10);                        msNode.AddInEdge(e2);                        msNode2.AddOutEdge(e2);                        graph.Edges.Add(e2);                    }                }            }        }        //calc layout, apply positions to original TestNodeElements        var settings = new Microsoft.Msagl.Layout.MDS.MdsLayoutSettings();        settings.ScaleX = 10; //this.heightMap.GetLength(0);        settings.ScaleY = 10; //this.heightMap.GetLength(1);        settings.NodeSeparation = this.NodeSeparation;        Microsoft.Msagl.Miscellaneous.LayoutHelpers.CalculateLayout(graph, settings, null);        // Move model to positive axis.        graph.UpdateBoundingBox();        graph.Translate(new Microsoft.Msagl.Core.Geometry.Point(-graph.Left, -graph.Bottom));        //move nodes to actually be in heightmap positoins        var scalex = this.heightMap.GetLength(0) / graph.Right;        var scaley = this.heightMap.GetLength(1) / graph.Top;        foreach (var n in graph.Nodes)        {            var newx = n.BoundingBox.Center.X * scalex;            var newy = n.BoundingBox.Center.Y * scaley;            n.BoundingBox = new Microsoft.Msagl.Core.Geometry.Rectangle(new Microsoft.Msagl.Core.DataStructures.Size(1,1), new Microsoft.Msagl.Core.Geometry.Point(newx, newy));        }        if (NodeWiggle)        {            //calc dtcn            Dictionary<Microsoft.Msagl.Core.Layout.Node, float> distanceToClosestNeighbor = new Dictionary<Microsoft.Msagl.Core.Layout.Node, float>();            foreach (var n in graph.Nodes)            {                if (distanceToClosestNeighbor.ContainsKey(n)) continue;                float dtcn = float.MaxValue;                Cell c = n.UserData as Cell;                Microsoft.Msagl.Core.Layout.Node cn = n;                for (int dx = -1; dx <= 1; dx++)                {                    for (int dy = -1; dy <= 1; dy++)                    {                        if (dx == 0 && dy == 0) continue;                        if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1) continue;                        if (dx + c.location.x >= maze.GetLength(0) || dx + c.location.x < 0) continue;                        if (dy + c.location.y >= maze.GetLength(1) || dy + c.location.y < 0) continue;                        Cell nc = maze[dx + c.location.x, dy + c.location.y];                        var nn = celltoNode[nc];                        float d = (new Vector2((float)n.BoundingBox.Center.X, (float)n.BoundingBox.Center.Y) - new Vector2((float)nn.BoundingBox.Center.X, (float)nn.BoundingBox.Center.Y)).magnitude;                        if (d < dtcn)                        {                            cn = nn;                            dtcn = d;                        }                    }                }                //set this for both this node and the nn                distanceToClosestNeighbor[n] = dtcn;                distanceToClosestNeighbor[cn] = dtcn;            }            //node wiggle            var l = 0;            foreach (var n in graph.Nodes)            {                //add a random offset to to the nodes that is less than the separation tho                var dir = Random.Range(0, Mathf.PI * 2);                var offset = new Vector2(Mathf.Cos(dir), Mathf.Sin(dir));                offset *= Mathf.PerlinNoise(0, l * 0.0002f) * distanceToClosestNeighbor[n] * 0.08f;                var newx = Mathf.Clamp((float)n.BoundingBox.Center.X + offset.x, 0, this.heightMap.GetLength(0));                var newy = Mathf.Clamp((float)n.BoundingBox.Center.Y + offset.y, 0, this.heightMap.GetLength(0));                n.BoundingBox = new Microsoft.Msagl.Core.Geometry.Rectangle(new Microsoft.Msagl.Core.DataStructures.Size(1, 1), new Microsoft.Msagl.Core.Geometry.Point(newx, newy));            }        }        Debug.LogFormat("Top {0}  Bottom {1}  Left {2}  Right {3}", graph.Top, graph.Bottom, graph.Left, graph.Right);        return graph;    }    //Maze Heightmap Generation    void GenerateMazeTerrain( Microsoft.Msagl.Core.Layout.GeometryGraph graph)    {        //drag a stamp across the screen until we hit all the nodes. If we aren't moving between adjacent nodes, pick it up (don't drag)        Stack<Microsoft.Msagl.Core.Layout.Node> stack = new Stack<Microsoft.Msagl.Core.Layout.Node>();        Stack<Microsoft.Msagl.Core.Layout.Node> lastNodes = new Stack<Microsoft.Msagl.Core.Layout.Node>();        stack.Push(graph.Nodes[0]);        Vector2Int stampPos = new Vector2Int((int)graph.Nodes[0].BoundingBox.Center.X, (int)graph.Nodes[0].BoundingBox.Center.Y);        var oldheightMap = new float[this.heightMap.GetLength(0), this.heightMap.GetLength(1)];        System.Buffer.BlockCopy(this.heightMap, 0, oldheightMap, 0, this.heightMap.Length * sizeof(float));        int l = 0;        while (stack.Count > 0)        {            var targetNode = stack.Peek();            var targetCell = targetNode.UserData as Cell;            string sp = string.Format("({0},{1})", stampPos.x, stampPos.y);            if (debug) Debug.LogFormat("Stamp At {0}", sp);            Vector2Int targetPos = new Vector2Int((int)targetNode.BoundingBox.Center.X, (int)targetNode.BoundingBox.Center.Y);            string tp = string.Format("({0},{1})", targetPos.x, targetPos.y);            if (debug) Debug.LogFormat("Target At {0}", tp);            //once we reach target, push its neighbors and pop it.            if (Vector2Int.Distance(stampPos, targetPos) <= StepSize)            {                //stamp                float remaining = Vector2Int.Distance(stampPos, targetPos);                float total = (lastNodes.Count > 0) ? Vector2Int.Distance(new Vector2Int((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y), targetPos) : remaining;                float sin_input_degrees = remaining / total * Mathf.PI;                float wiggle_multiplier = SineSmoothing ? Mathf.Sin(sin_input_degrees) : 1;                float wiggleMagnitude = (Mathf.PerlinNoise(0, WiggleFrequency * (float)l++) - 0.5f) * WiggleAmplitude * wiggle_multiplier;                var wiggle = Vector2.Perpendicular(targetPos - stampPos);                wiggle.Normalize();                wiggle *= wiggleMagnitude;                for (var dx = -StampRadius; dx < StampRadius; dx++)                {                    for (var dy = -StampRadius; dy < StampRadius; dy++)                    {                        if (Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2) >= Mathf.Pow(StampRadius, 2)) continue; //stamp in a circle                        Vector2Int pos = new Vector2Int(stampPos.x + dx + (int)wiggle.x, stampPos.y + dy + (int)wiggle.y);                        pos.Clamp(Vector2Int.zero, new Vector2Int(heightMap.GetLength(0) - 1, heightMap.GetLength(1) - 1));                        heightMap[pos.x, pos.y] = oldheightMap[pos.x, pos.y] + 0.002f * WallHeight;                        int alpha_x = ConvertIndex(pos.x, this.heightMap.GetLength(0), this.alphaMaps.GetLength(0));                        int alpha_y = ConvertIndex(pos.y, this.heightMap.GetLength(1), this.alphaMaps.GetLength(1));                        alphaMaps[alpha_x, alpha_y, 0] = 1;                        for (int k = 1; k < alphaMaps.GetLength(2); k++)                        {                            alphaMaps[alpha_x, alpha_y, k] = 0;                        }                                            }                }                targetCell.dijkstraVisited = true;                bool bt = true;                var tc = stack.Pop();                stampPos.Set(targetPos.x, targetPos.y); //clip to target                string target = string.Format("({0},{1})/[{2},{3}]", targetCell.location.x, targetCell.location.y, targetPos.x, targetPos.y);                if (debug) Debug.LogFormat("Reached {0}", target);                for (int dx = -1; dx <= 1; dx++)                {                    for (int dy = -1; dy <= 1; dy++)                    {                        if (dx == 0 && dy == 0) continue;                        if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1) continue;                        if (dx + targetCell.location.x >= this.MazeGenerator.GetCells().GetLength(0) || dx + targetCell.location.x < 0) continue;                        if (dy + targetCell.location.y >= this.MazeGenerator.GetCells().GetLength(1) || dy + targetCell.location.y < 0) continue;                        var c = this.MazeGenerator.GetCells()[dy + targetCell.location.y, dx + targetCell.location.x];                        if (c.isWall && !c.dijkstraVisited)                        {                            string neighbor = string.Format("({0},{1})/[{2},{3}]", c.location.x, c.location.y, celltoNode[c].BoundingBox.Center.X, celltoNode[c].BoundingBox.Center.Y);                            if (debug) Debug.LogFormat("Chose neighbor {0}", neighbor);                            stack.Push(tc);                            lastNodes.Push(tc);                            stack.Push(celltoNode[c]);                            bt = false;                            break;                        }                        if (bt == false) break;                    }                }                //if we're out of neighbors to visit                if (bt && lastNodes.Count > 0)                {                    if (stack.Count > 0)                    {                        string pop = string.Format("({0},{1})", (stack.Peek().UserData as Cell).location.x, (stack.Peek().UserData as Cell).location.y);                        if (debug) Debug.LogFormat("Cell {0} has no neighbors left, ploppin back to target {1} to check for ITS neighbors", target, pop);                        var newloc = stack.Peek().BoundingBox.Center;                        stampPos.Set((int)newloc.X, (int)newloc.Y);                    }                }            }            else            {                //if we're moving between adjacent cells, drag                if (lastNodes.Count > 0 && ((lastNodes.Peek().UserData as Cell).location - targetCell.location).magnitude == 1)                {                    //stamp                    float remaining = Vector2Int.Distance(stampPos, targetPos);                    float total = Vector2Int.Distance(new Vector2Int((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y), targetPos);                    float sin_input_degrees = remaining / total * Mathf.PI;                    float wiggle_multiplier = SineSmoothing ? Mathf.Sin(sin_input_degrees) : 1;                    float wiggleMagnitude = (Mathf.PerlinNoise(0, WiggleFrequency * (float)l++) - 0.5f) * WiggleAmplitude * wiggle_multiplier;                    var wiggle = Vector2.Perpendicular(targetPos - stampPos);                    wiggle.Normalize();                    wiggle *= wiggleMagnitude;                    for (var dx = -StampRadius; dx < StampRadius; dx++)                    {                        for (var dy = -StampRadius; dy < StampRadius; dy++)                        {                            if (Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2) >= Mathf.Pow(StampRadius, 2)) continue; //stamp in a circle                            Vector2Int pos = new Vector2Int(stampPos.x + dx + (int)wiggle.x, stampPos.y + dy + (int)wiggle.y);                            pos.Clamp(Vector2Int.zero, new Vector2Int(heightMap.GetLength(0) - 1, heightMap.GetLength(1) - 1));                            heightMap[pos.x, pos.y] = oldheightMap[pos.x, pos.y] + 0.002f * WallHeight;                            int alpha_x = ConvertIndex(pos.x, this.heightMap.GetLength(0), this.alphaMaps.GetLength(0));                            int alpha_y = ConvertIndex(pos.y, this.heightMap.GetLength(1), this.alphaMaps.GetLength(1));                            alphaMaps[alpha_x, alpha_y, 0] = 1;                            for (int k = 1; k < alphaMaps.GetLength(2); k++)                            {                                alphaMaps[alpha_x, alpha_y, k] = 0;                            }                        }                    }                    //move                    Vector2 dir = targetPos - stampPos;                    dir.Normalize();                    var step = dir * Mathf.Min(StepSize, (int)remaining);                    stampPos += new Vector2Int((int)step.x, (int)step.y);                    if (debug) Debug.LogFormat("Moved stamp by {0}//{1} in dir {2} to {3}", Mathf.Min(StepSize, (int)remaining), string.Format("[{0},{1}]", step.x, step.y), dir, stampPos);                }                //if we're popping back in the stack bc we hit a dead ed, just tp                else                {                    if (debug) Debug.Log("Popped//non adjacent");                    stampPos.Set((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y);                }            }        }    }    //Maze HeightMap Generation as Coroutine (For realtime)    IEnumerator GenerateMazeTerrainRealtime(Microsoft.Msagl.Core.Layout.GeometryGraph graph)    {        //drag a stamp across the screen until we hit all the nodes. If we aren't moving between adjacent nodes, pick it up (don't drag)        Stack<Microsoft.Msagl.Core.Layout.Node> stack = new Stack<Microsoft.Msagl.Core.Layout.Node>();        Stack<Microsoft.Msagl.Core.Layout.Node> lastNodes = new Stack<Microsoft.Msagl.Core.Layout.Node>();        stack.Push(graph.Nodes[0]);        Vector2Int stampPos = new Vector2Int((int)graph.Nodes[0].BoundingBox.Center.X, (int)graph.Nodes[0].BoundingBox.Center.Y);        int l = 0;        int n = StepsToDraw;        var oldheightMap = new float[this.heightMap.GetLength(0), this.heightMap.GetLength(1)];        System.Buffer.BlockCopy(this.heightMap, 0, oldheightMap, 0, this.heightMap.Length * sizeof(float));        while ((n == -1 || n-- > 0) && stack.Count > 0)        {            var targetNode = stack.Peek();            var targetCell = targetNode.UserData as Cell;            string sp = string.Format("({0},{1})", stampPos.x, stampPos.y);            if (debug) Debug.LogFormat("Stamp At {0}", sp);            Vector2Int targetPos = new Vector2Int((int)targetNode.BoundingBox.Center.X, (int)targetNode.BoundingBox.Center.Y);            string tp = string.Format("({0},{1})", targetPos.x, targetPos.y);            if (debug) Debug.LogFormat("Target At {0}", tp);            //once we reach target, push its neighbors and pop it.            if (n == 0)            {                string target = string.Format("({0},{1})", targetCell.location.x, targetCell.location.y);                string last = lastNodes.Count > 0 ? string.Format("({0},{1})", (lastNodes.Peek().UserData as Cell).location.x, (lastNodes.Peek().UserData as Cell).location.y) : "<NONE>";                if (debug) Debug.LogFormat("Going from {0} to {1}", last, target);            }            if (Vector2Int.Distance(stampPos, targetPos) <= StepSize)            {                //stamp                float remaining = Vector2Int.Distance(stampPos, targetPos);                float total = (lastNodes.Count > 0) ? Vector2Int.Distance(new Vector2Int((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y), targetPos) : remaining;                float sin_input_degrees = remaining / total * Mathf.PI;                float wiggle_multiplier = SineSmoothing ? Mathf.Sin(sin_input_degrees) : 1;                float wiggleMagnitude = (Mathf.PerlinNoise(0, WiggleFrequency * (float)l++) - 0.5f) * WiggleAmplitude * wiggle_multiplier;                var wiggle = Vector2.Perpendicular(targetPos - stampPos);                wiggle.Normalize();                wiggle *= wiggleMagnitude;                for (var dx = -StampRadius; dx < StampRadius; dx++)                {                    for (var dy = -StampRadius; dy < StampRadius; dy++)                    {                        if (Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2) >= Mathf.Pow(StampRadius, 2)) continue; //stamp in a circle                        Vector2Int pos = new Vector2Int(stampPos.x + dx + (int)wiggle.x, stampPos.y + dy + (int)wiggle.y);                        pos.Clamp(Vector2Int.zero, new Vector2Int(heightMap.GetLength(0) - 1, heightMap.GetLength(1) - 1));                        heightMap[pos.x, pos.y] += 0.002f * WallHeight;                        int alpha_x = ConvertIndex(pos.x, this.heightMap.GetLength(0), this.alphaMaps.GetLength(0));                        int alpha_y = ConvertIndex(pos.y, this.heightMap.GetLength(1), this.alphaMaps.GetLength(1));                        alphaMaps[alpha_x, alpha_y, 1] = 0.75f;                        alphaMaps[alpha_x, alpha_y, 0] = 0.25f;                    }                }                targetCell.dijkstraVisited = true;                bool bt = true;                var tc = stack.Pop();                stampPos.Set(targetPos.x, targetPos.y); //clip to target                string target = string.Format("({0},{1})/[{2},{3}]", targetCell.location.x, targetCell.location.y, targetPos.x, targetPos.y);                if (debug) Debug.LogFormat("Reached {0}", target);                for (int dx = -1; dx <= 1; dx++)                {                    for (int dy = -1; dy <= 1; dy++)                    {                        if (dx == 0 && dy == 0) continue;                        if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1) continue;                        if (dx + targetCell.location.x >= this.MazeGenerator.GetCells().GetLength(0) || dx + targetCell.location.x < 0) continue;                        if (dy + targetCell.location.y >= this.MazeGenerator.GetCells().GetLength(1) || dy + targetCell.location.y < 0) continue;                        var c = this.MazeGenerator.GetCells()[dy + targetCell.location.y, dx + targetCell.location.x];                        if (c.isWall && !c.dijkstraVisited)                        {                            string neighbor = string.Format("({0},{1})/[{2},{3}]", c.location.x, c.location.y, celltoNode[c].BoundingBox.Center.X, celltoNode[c].BoundingBox.Center.Y);                            if (debug) Debug.LogFormat("Chose neighbor {0}", neighbor);                            stack.Push(tc);                            lastNodes.Push(tc);                            stack.Push(celltoNode[c]);                            bt = false;                            break;                        }                        if (bt == false) break;                    }                }                //if we're out of neighbors to visit                if (bt && lastNodes.Count > 0)                {                    if (stack.Count > 0)                    {                        string pop = string.Format("({0},{1})", (stack.Peek().UserData as Cell).location.x, (stack.Peek().UserData as Cell).location.y);                        if (debug) Debug.LogFormat("Cell {0} has no neighbors left, ploppin back to target {1} to check for ITS neighbors", target, pop);                        var newloc = stack.Peek().BoundingBox.Center;                        stampPos.Set((int)newloc.X, (int)newloc.Y);                    }                }            }            else            {                //if we're moving between adjacent cells, drag                if (lastNodes.Count > 0 && ((lastNodes.Peek().UserData as Cell).location - targetCell.location).magnitude == 1)                {                    //stamp                    float remaining = Vector2Int.Distance(stampPos, targetPos);                    float total = Vector2Int.Distance(new Vector2Int((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y), targetPos);                    float sin_input_degrees = remaining / total * Mathf.PI;                    float wiggle_multiplier = SineSmoothing ? Mathf.Sin(sin_input_degrees) : 1;                    float wiggleMagnitude = (Mathf.PerlinNoise(0, WiggleFrequency * (float)l++) - 0.5f) * WiggleAmplitude * wiggle_multiplier;                    var wiggle = Vector2.Perpendicular(targetPos - stampPos);                    wiggle.Normalize();                    wiggle *= wiggleMagnitude;                    for (var dx = -StampRadius; dx < StampRadius; dx++)                    {                        for (var dy = -StampRadius; dy < StampRadius; dy++)                        {                            if (Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2) >= Mathf.Pow(StampRadius, 2)) continue; //stamp in a circle                            Vector2Int pos = new Vector2Int(stampPos.x + dx + (int)wiggle.x, stampPos.y + dy + (int)wiggle.y);                            pos.Clamp(Vector2Int.zero, new Vector2Int(heightMap.GetLength(0) - 1, heightMap.GetLength(1) - 1));                            heightMap[pos.x, pos.y] = oldheightMap[pos.x, pos.y] + 0.002f * WallHeight;                            int alpha_x = ConvertIndex(pos.x, this.heightMap.GetLength(0), this.alphaMaps.GetLength(0));                            int alpha_y = ConvertIndex(pos.y, this.heightMap.GetLength(1), this.alphaMaps.GetLength(1));                            alphaMaps[alpha_x, alpha_y, 1] = 1;                            alphaMaps[alpha_x, alpha_y, 0] = 0;                        }                    }                    //move                    Vector2 dir = targetPos - stampPos;                    dir.Normalize();                    var step = dir * Mathf.Min(StepSize, (int)remaining);                    stampPos += new Vector2Int((int)step.x, (int)step.y);                    if (debug) Debug.LogFormat("Moved stamp by {0}//{1} in dir {2} to {3}", Mathf.Min(StepSize, (int)remaining), string.Format("[{0},{1}]", step.x, step.y), dir, stampPos);                }                //if we're popping back in the stack bc we hit a dead ed, just tp                else                {                    if (debug) Debug.Log("Popped//non adjacent");                    stampPos.Set((int)lastNodes.Peek().BoundingBox.Center.X, (int)lastNodes.Peek().BoundingBox.Center.Y);                }            }            ApplyHeightMap();            ApplyAlphaMaps();            yield return null;        }        ApplyHeightMap();        ApplyAlphaMaps();        yield break;    }    void PrintWallLocations(Microsoft.Msagl.Core.Layout.GeometryGraph graph)    {        foreach (var n in graph.Nodes)        {            Debug.Log("Wall at (" + n.BoundingBox.Center.X +", "+n.BoundingBox.Center.Y + ")");        }    }    //Alphamap Interfaces    void LoadAlphaMaps () {        this.alphaMaps = TerrainMain.terrainData.GetAlphamaps(0,0,TerrainMain.terrainData.alphamapWidth,TerrainMain.terrainData.alphamapHeight);        if (debug) Debug.Log("Aplhas have size: "+this.alphaMaps.GetLength(0)+", "+this.alphaMaps.GetLength(1)+","+this.alphaMaps.GetLength(2));    }    void ApplyAlphaMaps () {        TerrainMain.terrainData.SetAlphamaps(0,0,this.alphaMaps);    }        void SetAlphaMapsToGrass()    {        for (var i = 0; i < this.alphaMaps.GetLength(0); i++)        {            for (var j = 0; j < this.alphaMaps.GetLength(1); j++)            {                for (var k = 0; k < this.alphaMaps.GetLength(2); k++)                {                    this.alphaMaps[i, j, k] = (k == 1) ? 1 : 0;                }            }        }    }    //Underlying Naturalistic Heightmap Generation    void GenerateLandHeightMap() {        int rows = this.heightMap.GetLength(0);        int cols = this.heightMap.GetLength(1);        float [,] newHeightMap = new float [ rows, cols];        for (int i = 0; i < rows; i++) {            for (int j = 0; j < cols; j++) {                float normalized = 0;                                for (int o = 0; o < NumOctaves; o++) {                    normalized += (o==0?0f:-0.5f * HillsAmplitude * Mathf.Pow(Persistence, o) ) + Mathf.Clamp( Mathf.Pow(Persistence, o) * HillsAmplitude * Mathf.PerlinNoise(PerlinSeed +  i * HillsRefinement * Mathf.Pow(Lacunarity, o),  j * HillsRefinement * Mathf.Pow(Lacunarity, o) ), 0, 1 );                }                newHeightMap[i,j] = this.heightMap[i,j] + Mathf.Pow(normalized, 2);            }        }        this.heightMap = newHeightMap;        SetAlphaMaps();        SetDetailMaps();    }    void SetAlphaMaps()    {        var hmapMax = heightMap.Cast<float>().Max();        for (var i = 0; i < this.alphaMaps.GetLength(0); i++)        {            for (var j = 0; j < this.alphaMaps.GetLength(1); j++)            {                //sample heightmap                var hmap_sample_x = ConvertIndex(i, alphaMaps.GetLength(0), heightMap.GetLength(0));                var hmap_sample_y = ConvertIndex(j, alphaMaps.GetLength(1), heightMap.GetLength(1));                var h = this.heightMap[hmap_sample_x, hmap_sample_y] / hmapMax;                var alpha_val = terrainTypes.Evaluate(h);                for (var k = 0; k < alphaMaps.GetLength(2)-1; k++)                {                    this.alphaMaps[i, j, k+1] = (terrainTypes.colorKeys[k].color == alpha_val) ? 1 : 0;                }            }        }    }    void SetDetailMaps()    {        var hmapMax = heightMap.Cast<float>().Max();        var map = new int[TerrainMain.terrainData.detailWidth, TerrainMain.terrainData.detailHeight];        TerrainMain.terrainData.SetTreeInstances(new TreeInstance[0], false);        for (var i = 0; i < TerrainMain.terrainData.detailWidth; i++)        {            for (var j = 0; j < TerrainMain.terrainData.detailHeight; j++)            {                //sample heightmap                var hmap_sample_x = ConvertIndex(i, TerrainMain.terrainData.detailWidth, heightMap.GetLength(0));                var hmap_sample_y = ConvertIndex(j, TerrainMain.terrainData.detailHeight, heightMap.GetLength(1));                var h = this.heightMap[hmap_sample_x, hmap_sample_y] / hmapMax;                //wiggle the sample                h += hmapMax * TextureWiggle * Mathf.PerlinNoise(i * GrassFreq, j * GrassFreq);                var alpha_val = terrainTypes.Evaluate(h);                if (alpha_val == terrainTypes.colorKeys[2].color)                {                    map[i, j] = Mathf.RoundToInt((MaxGrassDensity - MinGrassDensity) * Mathf.PerlinNoise(i * GrassFreq, j * GrassFreq) + MinGrassDensity);                    var c = UnityEngine.Random.Range(0, 500);                    if (c >= 0 && c <= 1)                    {                        //maybe instantiate a tree?                        TreeInstance newtree = new TreeInstance()                        {                            position = new Vector3(j / (float)TerrainMain.terrainData.detailWidth, 0, i / (float)TerrainMain.terrainData.detailWidth),                            prototypeIndex = 0,                            widthScale = 1f,                            heightScale = 1f,                            color = Color.white,                            lightmapColor = Color.white                        };                        TerrainMain.AddTreeInstance(newtree);                        TerrainMain.Flush();                    }                }            }        }        // Assign the modified map back.        TerrainMain.terrainData.SetDetailLayer(0, 0, 0, map);    }    //Detail Densitymap Interfaces     //TODO: this needs to be warped as well. ugh. not worth...    void SetGrass(Cell [,] maze) {        // Get all of layer zero.        var map = TerrainMain.terrainData.GetDetailLayer(0, 0, TerrainMain.terrainData.detailWidth, TerrainMain.terrainData.detailHeight, 0);        Debug.Log("Detail Map Dims:"+TerrainMain.terrainData.detailWidth+"x"+TerrainMain.terrainData.detailHeight);        // For each pixel in the detail map...        for (var y = 0; y < TerrainMain.terrainData.detailHeight; y++)        {            for (var x = 0; x < TerrainMain.terrainData.detailWidth; x++)            {                // If the heightmap value is above the threshold then                // set density there to 0                if (maze[ConvertIndex(x, TerrainMain.terrainData.detailWidth, maze.GetLength(0)), ConvertIndex(y, TerrainMain.terrainData.detailHeight, maze.GetLength(1))].isWall)                {                    map[x, y] = 0;                }                else                {                    map[x,y] = 6;                }            }        }        // Assign the modified map back.        TerrainMain.terrainData.SetDetailLayer(0, 0, 0, map);    }    public void RerollTerrain() {        ResetMaps();        //Debug.Log("Heightmap size: " + heightMap.GetLength(0) + " x " + heightMap.GetLength(1));        if (IncludeLand)        {            GenerateLandHeightMap();            SetDetailMaps();        }        if (IncludeMaze) {            //GenerateMazeTerrain(this.MazeGenerator.GetCells());            //GenerateAlphaMaps(this.MazeGenerator.GetCells());            //WarpHeightsAndAlphas();            var g = MazeToGraph(this.MazeGenerator.GetCells());            //MakeGraphDebugCubes(g);            GenerateMazeTerrain(g);        }                ApplyHeightMap();        ApplyAlphaMaps();    }    public void RerollTerrainRealtime()    {        ResetMaps();        //Debug.Log("Heightmap size: " + heightMap.GetLength(0) + " x " + heightMap.GetLength(1));        SetAlphaMapsToGrass();        if (IncludeLand)        {            GenerateLandHeightMap();        }        if (IncludeMaze)        {            //GenerateMazeTerrain(this.MazeGenerator.GetCells());            //GenerateAlphaMaps(this.MazeGenerator.GetCells());            //WarpHeightsAndAlphas();            var g = MazeToGraph(this.MazeGenerator.GetCells());            //MakeGraphDebugCubes(g);            StartCoroutine("GenerateMazeTerrainRealtime", g);        }    }    // old chunky gridlike walls (for debug)    public void ShowChunkyView()    {        //GenerateMazeTerrain(this.MazeGenerator.GetCells());        //GenerateAlphaMaps(this.MazeGenerator.GetCells());        ApplyHeightMap();        ApplyAlphaMaps();    }}