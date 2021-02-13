using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell {
    public Vector2Int location;
    public bool isWall, dijkstraVisited, inBase;
    public float age;
    public List<Cell> neighbors;

    public Cell () {
        this.location = new Vector2Int(0,0);
        this.isWall = false;
        this.dijkstraVisited = false;
        this.age = 0;
        this.inBase = false;
        this.neighbors = new List<Cell>();
    }

    public Cell (Vector2Int location, int age, bool wall, bool dv) {
        this.location = location;
        this.isWall = wall;
        this.age = age;
        this.dijkstraVisited = dv;
        this.inBase = false;
        this.neighbors = new List<Cell>();
    }
}

class Maze {
    public Cell[,] cells;
    public List<Vector3Int> bases;
    public int rows, cols, nBases;
    bool debug;

    public Maze(int rows, int cols, int nBases, bool debug) {
        this.rows = rows;
        this.cols = cols;
        this.nBases = nBases;
        this.bases = new List<Vector3Int>();
        this.debug = debug;

        //populate list of cells
        this.cells = new Cell[rows, cols];
        for (int r=0; r<rows; r++) {
            for (int c=0; c<cols; c++) {
                this.cells[r, c] = new Cell(new Vector2Int(c,r),0,true,false);
            }
        }

        //give each cell neighbors
        for (int r=0; r<rows; r++) {
            for (int c=0; c<cols; c++) {
                for (int dx = -2; dx <=2; dx+=2) {
                    for (int dy = -2; dy<=2; dy+=2) {
                        ref Cell cell = ref cells[r, c];

                        if (dx==0 && dy==0) continue;
                        if (dx!=0 && dy!=0) continue;
                        if (cell.location.y + dy < 0 || cell.location.y+dy >= this.rows) continue;
                        if (cell.location.x + dx < 0 || cell.location.x+dx >= this.cols) continue;
                        cell.neighbors.Add(this.cells[cell.location.y + dy, cell.location.x + dx]);
                    }
                }
            }
        }
        
        this.Bases();
        this.Dijkstra();
        this.BaseAges();
    }

    private void Bases() {
        if (this.debug) Debug.Log("Placing Bases...");

        for (int b=0; b<nBases; b++) {
            int baseSize = Random.Range(3,10);
            int baseRow = Random.Range(0,this.rows-baseSize-1);
            int baseCol = Random.Range(0,this.cols-baseSize-1);

            for (int dr = 0; dr <baseSize; dr++) {
                for (int dc = 0; dc<baseSize; dc++) {
                    if (dr%baseSize==0 || dc%baseSize==0) continue;
                    this.cells[baseRow+dr, baseCol+dc].dijkstraVisited=true;
                    this.cells[baseRow+dr, baseCol+dc].isWall=false;
                    this.cells[baseRow+dr, baseCol+dc].inBase=true;
                    this.cells[baseRow+dr, baseCol+dc].age = baseSize;
                }
            }

            this.bases.Add(new Vector3Int(baseCol,baseRow,baseSize));
        }

        if (this.debug) Debug.Log("Bases placed.");
    }

    private void BaseAges() {
        if (this.debug) Debug.Log("Assigning Base ages...");
        foreach (Vector3Int b in this.bases) {
            float sum = 0;
            float n=0;

            //find avg age of perimeter
            for (int dr = 0; dr <b.z; dr++) {
                for (int dc = 0; dc<b.z; dc++) {
                    if (dr%b.z!=0 && dc%b.z!=0) continue;
                    sum += this.cells[b.y+dr, b.x+dc].age;
                    n++;
                }
            }
            
            sum = Mathf.Round(sum * 100/n)/100;

            //assign this to interior
            for (int dr = 0; dr <b.z; dr++) {
                for (int dc = 0; dc<b.z; dc++) {
                    if (dr%b.z==0 || dc%b.z==0) continue;
                    this.cells[b.y+dr, b.x+dc].age = sum;
                }
            }
        }
        if (this.debug) Debug.Log("Base ages assigned!");
    }

    private void Dijkstra() {
        if (this.debug) Debug.Log("Starting Dijkstra...");
        Stack<Cell> stack = new Stack<Cell>();
        Cell currentCell = this.cells[1, 1];
        currentCell.dijkstraVisited=true;
        stack.Push(currentCell);
        int steps=0;
        float peakAge=0;
        currentCell.isWall = false;
        currentCell.age = steps;

        while (stack.Count != 0) {
            currentCell = stack.Pop();

            List<Cell> unvisitedNeighbors = new List<Cell>();
            foreach (Cell c in currentCell.neighbors) {
                if (!c.dijkstraVisited) unvisitedNeighbors.Add(c);
            }
            
            if (unvisitedNeighbors.Count>0) {
                stack.Push(currentCell);
                int n = Random.Range(0,unvisitedNeighbors.Count);
                unvisitedNeighbors[n].dijkstraVisited=true;
                stack.Push(unvisitedNeighbors[n]);
                
                steps++;
                
                //remove wall from in between
                Vector2 diff = ((Vector2)unvisitedNeighbors[n].location-currentCell.location);
                diff.Normalize();
                Vector2Int cellBetween = currentCell.location + Vector2Int.RoundToInt(diff);
                this.cells[cellBetween.y, cellBetween.x].age=steps;
                this.cells[cellBetween.y, cellBetween.x].isWall=false;
                
                steps++;
                unvisitedNeighbors[n].age = steps;
                unvisitedNeighbors[n].isWall=false;
                peakAge = Mathf.Max(steps, peakAge);
                
            } else {
                steps-=2;
            }
        }
        //reset visited on each cell and put ages in range 0,1
        for (int r=0; r<rows; r++) {
            for (int c=0; c<cols; c++) {
                ref Cell cell = ref this.cells[r, c];

                cell.dijkstraVisited = false;
                if (cell.inBase) continue;
                cell.age/=peakAge;
                cell.age = Mathf.Round(cell.age * 100f) / 100f;
            }
        }
        
        if (this.debug) Debug.Log("Dijkstra complete!");
    }
}

[System.Serializable]
public class MazeGenerator : MonoBehaviour
{
    private Maze maze;

    //General Settings
    public bool RemakeMaze = false, autoupdate = true, debug = true;

    //Maze Configuration
    public int rows, cols, nBases;

    //Gui Overlay (Minimap) Configuration
    public bool DrawMazeGUI;
    public Texture boxTexture,wallTexture;
    public int boxSize = 10;
    Vector2 scrollPosition = Vector2.zero;

    public Cell[,] GetCells() {
        if (this.maze is null) GenerateMaze();
        return this.maze.cells;
    }
    public void GenerateMaze() {
        this.maze = new Maze(rows,cols,nBases,debug);
    }

    void OnGUI() {
        if (!DrawMazeGUI) return;

        if (this.maze==null) return;

        //scrollPosition = GUI.BeginScrollView(new Rect(300, 300, 3000, 1400), scrollPosition, new Rect(0, 0, boxSize*cols+50, boxSize*rows+50));
        for (int r=0; r<rows; r++) {
            for (int c=0; c<cols; c++) {
                ref Cell cell = ref this.maze.cells [r, c];

                Texture t = cell.isWall?wallTexture:boxTexture;
                    
                if (cell.isWall) {
                    GUI.Box(new Rect(cell.location.y*boxSize, cell.location.x*boxSize, boxSize, boxSize), t);//new GUIContent(""+c.age, t));
                } else {
                    GUI.Box(new Rect(cell.location.y*boxSize, cell.location.x*boxSize, boxSize, boxSize), ""+cell.age);//new GUIContent(""+c.age, t));
                }

            }
        }
        //GUI.EndScrollView();
    }
}
