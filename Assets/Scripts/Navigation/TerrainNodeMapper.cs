using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.PlayerSettings;

public class TerrainNodeMapper : MonoBehaviour
{
    public static TerrainNodeMapper Instance;
    public Dictionary<(int, int), TerrainNode> TerrainNodes;

    [SerializeField] private MeshCollider LevelMesh;
    [SerializeField] private float XViewBuffer;
    [SerializeField] private float YViewBuffer;
    [SerializeField] private float ScaleFactor;

    private int columnCount;
    private int rowCount;


    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        //TODO: Should be preformed from a manager (GameManager)
        InitializeMapTerrain();
    }



    #region Getter

    /// <summary>
    /// Get the column and row based on the pos provided
    /// Grab the node if it exists
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public bool TryGetClosetNodeTo(Vector3 pos, out TerrainNode node)
    {
        if (TerrainNodes.Count > 0)
        {
            int column = GetNodeColumnBy(pos);
            int row = GetNodeRowBy(pos);
                        
            node = TerrainNodes[(column, row)];
            return true;                          
        }

        node = null;
        return false;
    }


    /// <summary>
    /// Based on a given position gather all the nodes that are within the height / width distance of the position
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="height"></param>
    /// <param name="width"></param>
    /// <param name="nodes"></param>
    /// <returns></returns>
    public bool TryGetNodesInRegion(Vector3 pos, float height, float width, out List<List<TerrainNode>> nodes, params TerrainNodeType[] searchTypes)
    {
        if (TryCalculateNodeRegion(pos, height, width, out int maxColumn, out int minColumn, out int maxRow, out int minRow))
        {
            nodes = new List<List<TerrainNode>>();

            for (int i = minColumn; i < maxColumn; i++)
            {
                List<TerrainNode> rowNodes = new List<TerrainNode>();

                for (int j = minRow; j < maxRow; j++)
                {
                    if (searchTypes.Length > 0)
                    {
                        foreach (TerrainNodeType nodeType in searchTypes)
                        {
                            if (nodeType == TerrainNodes[(i, j)].Type)
                                rowNodes.Add(TerrainNodes[(i, j)]);
                        }
                    }

                    else
                        rowNodes.Add(TerrainNodes[(i, j)]);    
                }

                nodes.Add(rowNodes);
            }

            return true;        
        }

        nodes = null;
        return false;
    }


    public List<List<TerrainNode>> GetAllNodesBetween(Vector3 pos1, Vector3 pos2, params TerrainNodeType[] searchTypes)
    {
        List<List<TerrainNode>> nodes = new List<List<TerrainNode>>();

        int maxColumn;
        int minColumn;

        if (pos1.x > pos2.x)
        {
            maxColumn = GetNodeColumnBy(pos1);
            minColumn = GetNodeColumnBy(pos2);
        }

        else
        {
            maxColumn = GetNodeColumnBy(pos2);
            minColumn = GetNodeColumnBy(pos1);
        }

        //Clamp Columns inside terrain bounds
        maxColumn = Mathf.Clamp(maxColumn, -columnCount / 2, columnCount / 2);
        minColumn = Mathf.Clamp(minColumn, -columnCount / 2, columnCount / 2);

        for (int i = minColumn; i < maxColumn; i++)
        {
            List<TerrainNode> rowNodes = new List<TerrainNode>();

            for (int j = -rowCount / 2;  j < rowCount / 2; j++)
            {
                if (searchTypes.Length > 0)
                {
                    foreach (TerrainNodeType nodeType in searchTypes)
                    {
                        if (nodeType == TerrainNodes[(i, j)].Type)
                            rowNodes.Add(TerrainNodes[(i, j)]);
                    }
                }

                else
                    rowNodes.Add(TerrainNodes[(i, j)]);
            }

            nodes.Add(rowNodes);
        }

        return nodes;
    }

    #endregion


    private Vector3 CaluclateNodePos(int curColumn, int curRow)
    {
        return transform.position + new Vector3(curColumn * ScaleFactor, curRow * ScaleFactor, 0);
    }

    
    /// <summary>
    /// Get the column index for a given node based on position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private int GetNodeColumnBy(Vector3 pos)
    {
        return Mathf.RoundToInt((pos.x - transform.position.x) / ScaleFactor);
    }


    /// <summary>
    /// Get the row index for a given node based on position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private int GetNodeRowBy(Vector3 pos)
    {
        int column = GetNodeColumnBy(pos);

        return Mathf.RoundToInt((pos.y - transform.position.y) / ScaleFactor);
    }


    /// <summary>
    /// Based on a centerpoint, a desired height and desired width, calculate the min and max for columns and rows
    /// Min and max are the index values within the dictionary(TerrainNodes)
    /// </summary>
    /// <param name="centerPoint"></param>
    /// <param name="height"></param>
    /// <param name="width"></param>
    /// <param name="maxColumn"></param>
    /// <param name="minColumn"></param>
    /// <param name="maxRow"></param>
    /// <param name="minRow"></param>
    /// <returns></returns>
    private bool TryCalculateNodeRegion(Vector3 centerPoint, float height, float width, out int maxColumn, out int minColumn, out int maxRow, out int minRow)
    {
        maxColumn = 0;
        minColumn = 0;
        maxRow = 0;
        minRow = 0;

        int column = GetNodeColumnBy(centerPoint);
        int row = GetNodeRowBy(centerPoint);

        if (TerrainNodes.ContainsKey((column, row)))
        {
            int numColumns = Mathf.CeilToInt(width / ScaleFactor);
            int numRows = Mathf.CeilToInt(height / ScaleFactor);

            //Determin maxColumn
            maxColumn = column + Mathf.CeilToInt(numColumns / 2);
            if (!TerrainNodes.ContainsKey((maxColumn, row)))
                maxColumn = columnCount / 2;

            //Determine minColumn
            minColumn = column - Mathf.CeilToInt(numColumns / 2);
            if (!TerrainNodes.ContainsKey((minColumn, row)))
                minColumn = -columnCount / 2;

            //Detrmine maxRow
            maxRow = row + Mathf.CeilToInt(numRows / 2);
            if (!TerrainNodes.ContainsKey((column, maxRow)))
                maxRow = rowCount / 2;

            //Determine minRow
            minRow = row - Mathf.CeilToInt(numRows / 2);
            if (!TerrainNodes.ContainsKey((column, minRow)))
                minRow = -rowCount / 2;

            return true;
        }
        
        return false;
    }  


    /// <summary>
    /// Create a map of the terrain using nodes that determine where surfaces and walls are
    /// </summary>
    private void InitializeMapTerrain()
    {
        if (LevelMesh != null)
        {
            TerrainNodes = new Dictionary<(int, int), TerrainNode>();

            float height = LevelMesh.bounds.size.y;
            float width = LevelMesh.bounds.size.x;

            //Apply Buffer
            height += 2 * YViewBuffer;
            width += 2 * XViewBuffer;

            //Columns
            columnCount = Mathf.CeilToInt(width / ScaleFactor);

            //Rows        
            rowCount = Mathf.CeilToInt(height / ScaleFactor);

            int curColumn = -columnCount / 2;

            while (curColumn <= columnCount / 2)
            {
                int curRow = -rowCount / 2;

                while (curRow <= rowCount / 2)
                {
                    //Create new node
                    TerrainNode node = new TerrainNode(CaluclateNodePos(curColumn, curRow), curColumn, curRow);

                    PerformTerrainCast(node, curColumn, curRow);

                    TerrainNodes.Add((curColumn, curRow), node);

                    //Increment
                    curRow++;
                }

                //Increment
                curColumn++;
            }
        }
    }


    /// <summary>
    /// Redetermin raycasts of terrain nodes in a select region
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="height"></param>
    /// <param name="width"></param>
    public void RecalculateTerrainRegion(Vector3 pos, float height, float width)
    {
        if (TryCalculateNodeRegion(pos, height, width, out int maxColumn, out int minColumn, out int maxRow, out int minRow))
        {
            for (int i = minColumn; i < maxColumn; i++)
            {
                for (int j = minRow; j < maxRow; j++)
                {
                    TerrainNode node = TerrainNodes[(i, j)];                                    
                    PerformTerrainCast(node, i, j);
                }
            }
        }
    }



    /// <summary>
    /// Determine if node collides with terrain in any direction (up, down, left, right)
    /// Determine if node or previous nodes are inside the terrain
    /// </summary>
    /// <param name="node"></param>
    /// <param name="curColumn"></param>
    /// <param name="curRow"></param>
    private void PerformTerrainCast(TerrainNode node, int curColumn, int curRow)
    {
        node.PerformRaycastCheck(ScaleFactor);

        CheckInsideTerrain(node, curColumn, curRow);

        CheckForLedge(node, curColumn, curRow);
    }


    #region Inside Terrain Check

    /// <summary>
    /// Check both horizontal and vertical directions to determine if node is inside the terrain 
    /// </summary>
    /// <param name="node"></param>
    /// <param name="curColumn"></param>
    /// <param name="curRow"></param>
    private void CheckInsideTerrain(TerrainNode node, int curColumn, int curRow)
    {
        //Inside Horizontal Terrain Check
        if (TerrainNodes.ContainsKey((curColumn - 1, curRow)))
        {
            TerrainNode prevColumnNode = TerrainNodes[(curColumn - 1, curRow)];
            if (InsideHorizontalTerrainCheck(prevColumnNode, node))
            {
                node.Type = TerrainNodeType.Inside;
                node.ResetRaycasts();
            }
        }

        //Inside Vertical Terrain Check
        if (node.DownCollision != null)
        {
            int index = 1;

            while (TerrainNodes.ContainsKey((curColumn, curRow - index)))
            {
                TerrainNode prevRowNode = TerrainNodes[(curColumn, curRow - index)];

                if (InsideVerticalTerrainCheck(prevRowNode))
                {
                    prevRowNode.Type = TerrainNodeType.Inside;
                    prevRowNode.ResetRaycasts();

                    index++;
                }

                else
                    break;
            }
        }
    }


    /// <summary>
    /// Determine if curNode is inside the terrain
    /// Check is based on left to right of the terrain nodes
    /// Only need to check if previous columns node on the same row was inside the terrain or had collision to the terrain in the direction of the curNode
    /// CurNode will check to see if in the opposite dir collides with the terrain to see if its outside 
    /// </summary>
    /// <param name="preColumnNode"></param>
    /// <param name="curNode"></param>
    /// <returns></returns>
    private bool InsideHorizontalTerrainCheck(TerrainNode preColumnNode, TerrainNode curNode)
    {        
        if (preColumnNode.Type == TerrainNodeType.Inside || 
            (preColumnNode.Pos.x < curNode.Pos.x && preColumnNode.RightCollision != null))
        {
            //Determine direction of raycast (left or right)
            Vector3 dir = Vector3.left;

            if (Physics.Raycast(curNode.Pos, dir, ScaleFactor, LayerMask.GetMask("Environment")))
                return false;

            else
                return true;
        }

        return false;
    }


    /// <summary>
    /// Determine if a previous node is inside the terrain
    /// Check is based on top to bottom of terrain nodes when collision is detected
    /// Objective is to determine if the node has a cast upwards which would indicate out of the terrain
    /// </summary>
    /// <param name="preRowNode"></param>
    /// <param name="curNode"></param>
    /// <returns></returns>
    private bool InsideVerticalTerrainCheck(TerrainNode preRowNode)
    {
        if (preRowNode.UpCollision == null)
        {
            return true;
        }
        
        return false;
    }

    #endregion


    #region Ledge Check

    private void CheckForLedge(TerrainNode node, int curColumn, int curRow)
    {
        if (node.Type != TerrainNodeType.Inside)
        {
            if (TerrainNodes.ContainsKey((curColumn, curRow - 1)))
            {
                TerrainNode prevRowNode = TerrainNodes[(curColumn, curRow - 1)];

                if (node.RightCollision == null && prevRowNode.RightCollision != null) 
                { 
                    if (prevRowNode.RightCollision.SlopeGradiant == 0)
                    {
                        prevRowNode.Type = TerrainNodeType.WallLedge;                        
                    }
                }

                if (node.LeftCollision == null && prevRowNode.LeftCollision != null)
                {
                    if (prevRowNode.LeftCollision.SlopeGradiant == 0)
                    {
                        prevRowNode.Type = TerrainNodeType.WallLedge;
                    }
                }
            }

            CheckForSurfaceLedge(node, curColumn, curRow);            
        }
    }
  

    private void CheckForSurfaceLedge(TerrainNode node, int curColumn, int curRow)
    {
        //Check for surface ledge on left side
        if (TerrainNodes.ContainsKey((curColumn, curRow - 1)))
        {
            //Check that the prev rowNode is inside the terrain
            if (TerrainNodes[(curColumn, curRow - 1)].Type == TerrainNodeType.Inside)
            {
                if (TerrainNodes.ContainsKey((curColumn - 1, curRow - 1)))
                {
                    if (TerrainNodes[(curColumn - 1, curRow - 1)].Type == TerrainNodeType.WallLedge)
                    {
                        node.Type = TerrainNodeType.SurfaceLedge;
                    }
                }
            }
        }

        //Check for surface ledge on right side
        if (TerrainNodes.ContainsKey((curColumn, curRow - 1)))
        {
            if (TerrainNodes[(curColumn, curRow - 1)].Type == TerrainNodeType.WallLedge)
            {
                if (TerrainNodes.ContainsKey((curColumn - 1, curRow)))
                {
                    //Check that the prev rowNode is inside the terrain
                    if (TerrainNodes[(curColumn - 1, curRow)].DownCollision != null)
                    {                        
                        TerrainNodes[(curColumn - 1, curRow)].Type = TerrainNodeType.SurfaceLedge;                        
                    }
                }
            }
        }
    }


    #endregion


    private void OnDrawGizmos()
    {
        if (TerrainNodes != null && TerrainNodes.Count > 0)
        {
            //Draw Nodes
            foreach (TerrainNode node in TerrainNodes.Values)
            {
                switch (node.Type)
                {
                    case TerrainNodeType.Air:
                        Gizmos.color = Color.cyan;
                        break;

                    case TerrainNodeType.Surface:
                        Gizmos.color = Color.green;
                        break;

                    case TerrainNodeType.Wall:
                        Gizmos.color = Color.red;
                        break;

                    case TerrainNodeType.Ceiling:
                        Gizmos.color = Color.yellow;
                        break;

                    case TerrainNodeType.SurfaceWall:
                        Gizmos.color = Color.magenta;
                        break;

                    case TerrainNodeType.CeilngWall:
                        Gizmos.color = Color.magenta;
                        break;

                    case TerrainNodeType.SurfaceLedge:
                        Gizmos.color = Color.white;
                        break;

                    case TerrainNodeType.WallLedge:
                        Gizmos.color = Color.grey;
                        break;

                    case TerrainNodeType.Inside:
                        Gizmos.color = Color.black;
                        break;
                }

                Gizmos.DrawCube(node.Pos, Vector3.one * 0.1f);

                //Draw Collisions
                if (node.UpCollision != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(node.Pos, node.Pos + Vector3.up * ScaleFactor);
                }

                if (node.DownCollision != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(node.Pos, node.Pos - Vector3.up * ScaleFactor);
                        
                    Gizmos.color = Color.Lerp(Color.red, Color.green, node.DownCollision.SlopeGradiant / 90);
                    Gizmos.DrawSphere(node.DownCollision.CollisionPoint, 0.1f);
                }

                if (node.RightCollision != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(node.Pos, node.Pos + Vector3.right * ScaleFactor);

                    Gizmos.color = Color.Lerp(Color.red, Color.green, node.RightCollision.SlopeGradiant / 90);
                    Gizmos.DrawSphere(node.RightCollision.CollisionPoint, 0.1f);
                }

                if (node.LeftCollision != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(node.Pos, node.Pos - Vector3.right * ScaleFactor);

                    Gizmos.color = Color.Lerp(Color.red, Color.green, node.LeftCollision.SlopeGradiant / 90);
                    Gizmos.DrawSphere(node.LeftCollision.CollisionPoint, 0.1f);
                }                
            }
        }
    }




    //TEMP
    public bool Check;
    private void Update()
    {
        if (Check)
        {
            Check = false;
            RecalculateTerrainRegion(new Vector3(0, 200, 0), 50, 50);
        }
    }

}
