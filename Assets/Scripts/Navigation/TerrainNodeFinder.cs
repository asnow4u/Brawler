using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainNodeFinder : MonoBehaviour
{
    public static TerrainNodeFinder Instance;
    public List<List<TerrainNode>> TerrainNodes = new List<List<TerrainNode>>();

    [SerializeField] private MeshCollider LevelMesh;
    [SerializeField] private float XViewBuffer;
    [SerializeField] private float YViewBuffer;
    [SerializeField] private float ScaleFactor;


    private void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(Instance);

        InitializeMapTerrain();
    }


    #region Getter

    public bool TryGetClosetNodeTo(Vector3 pos, out TerrainNode node)
    {
        if (TerrainNodes.Count > 0)
        {
            int column = Mathf.RoundToInt((pos.x - transform.position.x) / ScaleFactor) + ((TerrainNodes.Count - 1) / 2);
            
            if (TerrainNodes.Count > column)
            {
                int row = Mathf.RoundToInt((pos.y - transform.position.y) / ScaleFactor) + ((TerrainNodes[column].Count -1) / 2);
                if (TerrainNodes[column].Count > row)
                {
                    node = TerrainNodes[column][row];
                    return true;
                }
            }
        }

        node = null;
        return false;
    }


    //TODO:
    public bool TryGetNodesInRegion(Vector3 pos, float height, float width, out List<List<TerrainNode>> nodes)
    {
        nodes = null;
        return false;
    }


    //TODO:
    public bool TryGetNodesInCameraView(out List<List<TerrainNode>> nodes)
    {
        nodes = null;
        return false;
    }

    #endregion 


    #region Terrain Mapping

    private Vector3 CaluclateNodePos(int curColumn, int curRow)
    {
        return transform.position + new Vector3(curColumn * ScaleFactor, curRow * ScaleFactor, 0);
    }


    [ContextMenu("Map Terrain")]
    private void InitializeMapTerrain()
    {
        TerrainNodes.Clear();

        if (LevelMesh != null)
        {
            float height = LevelMesh.bounds.size.y;
            float width = LevelMesh.bounds.size.x;

            //Apply Buffer
            height += 2 * YViewBuffer;
            width += 2 * XViewBuffer;

            //Columns
            int numColumns = Mathf.CeilToInt(width / ScaleFactor);

            //Rows        
            int numRows = Mathf.CeilToInt(height / ScaleFactor);

            int curColumn = -numColumns / 2;

            while (curColumn <= numColumns / 2)
            {
                List<TerrainNode> rowNodes = MapTerrainRows(numRows, curColumn);

                //Add to List
                TerrainNodes.Add(rowNodes);

                //Increment
                curColumn++;
            }
        }
    }


    private List<TerrainNode> MapTerrainRows(int numRows, int curColumn)
    {
        List<TerrainNode> rowNodes = new List<TerrainNode>();

        int curRow = -numRows / 2;

        while (curRow <= numRows / 2)
        {
            //Create new node
            TerrainNode node = new TerrainNode(CaluclateNodePos(curColumn, curRow));

            //If previous column exists, check if previous row node was inside terrain and check if cur row node is still inside terrain
            if (TerrainNodes.Count > 0)
            {
                List<TerrainNode> lastColumnNodes = TerrainNodes.Last();

                if (InsideHorizontalTerrainCheck(lastColumnNodes[curRow + numRows / 2], node))
                {
                    node.InsideTerrain = true;
                }

                //TODO: InsideVerticalTerrainCheck

                if (!node.InsideTerrain)
                {
                    node.PerformRaycastCheck(ScaleFactor);
                }
            }

            else
            {
                node.PerformRaycastCheck(ScaleFactor);                
            }

            //Add to List
            rowNodes.Add(node);

            //Increment
            curRow++;
        }

        return rowNodes;
    }


    /// <summary>
    /// Determine if curNode is also inside the terrain
    /// Only need to check if previous columns node on the same row was inside the terrain or had collision to the terrain in the direction of the curNode
    /// CurNode will check to see if in the oposite dir it collides with the terrain to see if its outside 
    /// </summary>
    /// <param name="preColumnNode"></param>
    /// <param name="curNode"></param>
    /// <returns></returns>
    private bool InsideHorizontalTerrainCheck(TerrainNode preColumnNode, TerrainNode curNode)
    {        
        if (preColumnNode.InsideTerrain 
            || (preColumnNode.Pos.x < curNode.Pos.x && preColumnNode.RightCollision != null) //Previous Column node hit raycast to the right
            || (preColumnNode.Pos.x > curNode.Pos.x && preColumnNode.LeftCollision != null)) //Previous Column node hit raycast to the left                                                                                              
        {
            //Determine direction of raycast (left or right)
            Vector3 dir = Vector3.right;
            if (preColumnNode.Pos.x < curNode.Pos.x)
                dir *= -1;

            if (Physics.Raycast(curNode.Pos, dir, ScaleFactor, LayerMask.GetMask("Environment")))
                return false;

            else
                return true;
        }

        return false;
    }

    #endregion


    private void OnDrawGizmos()
    {
        if (TerrainNodes.Count > 0)
        {
            //Draw Nodes
            foreach (List<TerrainNode> rowNodes in TerrainNodes)
            {
                foreach (TerrainNode node in rowNodes)
                {
                    if (node.InsideTerrain)
                        Gizmos.color = Color.black;
                    else
                        Gizmos.color = Color.red;

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
    }
}
