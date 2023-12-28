using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class TerrainNodeFinder
{
    public struct Node
    {
        public Vector3 Pos;
        public bool HorizontalInsideTerrainCheck;
        public bool VerticalInsideTerrainCheck;
        public RaycastHit UpHit;
        public RaycastHit DownHit;
        public RaycastHit LeftHit;
        public RaycastHit RightHit;

        public Node(Vector3 pos)
        {
            this.Pos = pos;
            this.HorizontalInsideTerrainCheck = false;
            this.VerticalInsideTerrainCheck = false;
            this.UpHit = new RaycastHit();
            this.DownHit = new RaycastHit();
            this.LeftHit = new RaycastHit();
            this.RightHit = new RaycastHit();
        }
    }

    public List<List<Node>> TerrainNodes;


    public TerrainNodeFinder()
    {
        TerrainNodes = new List<List<Node>>();
    }


    /// <summary>
    /// Map out the terrain based on a given start point and target point
    /// The nodes will be spaced based upon the bounds provided
    /// Nodes will both be created vertacally as well as horizontally (2D)
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="targetPos"></param>
    /// <param name="bounds"></param>
    /// <param name="numRows"></param>
    /// <param name="numColumns"></param>
    /// <param name="isRight"></param>
    /// <param name="isUp"></param>
    public void MapTerrain(Vector3 startPos, Vector3 targetPos, float minYView, Bounds bounds)
    {
        TerrainNodes.Clear();

        bool isRight = (targetPos.x - startPos.x) > 0;
        bool isUp = (targetPos.y - startPos.y) > -bounds.size.y; //Temp: Eventally y should extend in both up and down directions

        //Move to starting point
        Vector3 curPos = startPos + Vector3.right * bounds.size.x * (isRight ? 1 : -1);

        //Columns
        int columnCount = 0;
        int numColumns = Mathf.Abs(Mathf.CeilToInt((targetPos.x - curPos.x) / bounds.size.x));

        //Rows
        float yDist = Mathf.Max((targetPos.y - curPos.y), minYView);
        int numRows = Mathf.Abs(Mathf.CeilToInt(yDist / bounds.size.y));


        while (columnCount <= numColumns)
        {
            List<Node> rowNodes = MapTerrainRows(curPos, bounds, numRows, isRight, isUp);

            //Add to List
            TerrainNodes.Add(rowNodes);

            //Update pos
            curPos += Vector3.right * bounds.size.x * (isRight ? 1 : -1);

            //Increment
            columnCount++;
        }
    }

    /// <summary>
    /// Map out each node within a given column based on the number of rows calculated before
    /// If a node from the previous column but same row encountered a wall, skip calculating due to being inside an object
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="bound"></param>
    /// <param name="numRows"></param>
    /// <param name="isRight"></param>
    /// <param name="isUp"></param>
    /// <returns></returns>
    private List<Node> MapTerrainRows(Vector3 startPoint, Bounds bound, int numRows, bool isRight, bool isUp)
    {
        int rowCount = 0;
        Vector3 curPos = startPoint;

        List<Node> rowNodes = new List<Node>();

        while (rowCount <= numRows)
        {
            //Create new node
            Node node = new Node(curPos);

            if (TerrainNodes.Count > 0)
            {
                List<Node> lastColumnNodes = TerrainNodes.Last();
                
                if (InsideHorizontalTerrainCheck(lastColumnNodes[rowCount], curPos, isRight, bound.size.x))
                {
                    node.HorizontalInsideTerrainCheck = true;
                }
               
                //else if (lastColumnNodes[rowCount].VerticalInsideTerrainCheck)
                //{
                //    //TODO: Later
                //}

                if (!node.HorizontalInsideTerrainCheck && !node.VerticalInsideTerrainCheck)
                {
                    GetRaycastDataForNode(ref node, curPos, bound.size.x, bound.size.y, isRight);                    
                }
            }

            else
            {
                GetRaycastDataForNode(ref node, curPos, bound.size.x, bound.size.y, isRight);
            }

            //Add to List
            rowNodes.Add(node);

            //Update pos
            curPos += Vector3.up * bound.size.y * (isUp ? 1 : -1);

            //Increment
            rowCount++;
        }

        return rowNodes;
    }


    private void GetRaycastDataForNode(ref Node node, Vector3 curPos, float distanceX, float distanceY, bool isRight)
    {
        RaycastHit hit;

        //UpCast
        if (Physics.Raycast(curPos, Vector3.up, out hit, distanceY, LayerMask.GetMask("Environment")))
        {
            node.UpHit = hit;
        }

        //DownCast
        if (Physics.Raycast(curPos, Vector3.down, out hit, distanceY, LayerMask.GetMask("Environment")))
        {
            node.DownHit = hit;
        }


        if (isRight)
        {
            //RightCast
            if (Physics.Raycast(curPos, Vector3.right, out hit, distanceX, LayerMask.GetMask("Environment")))
            {
                node.RightHit = hit;                
            }
        }

        else
        {
            //LeftCast
            if (Physics.Raycast(curPos, Vector3.left, out hit, distanceX, LayerMask.GetMask("Environment")))
            {
                node.DownHit = hit;
            }
        }
    }


    private bool InsideHorizontalTerrainCheck(Node preRowNode, Vector3 curPos, bool isRight, float dist)
    {
        Vector3 dir = Vector3.right * (isRight ? -1 : 1);

        if (preRowNode.HorizontalInsideTerrainCheck || ((isRight) ? preRowNode.RightHit.collider != null : preRowNode.LeftHit.collider != null))
        {
            if (Physics.Raycast(curPos, dir, dist, LayerMask.GetMask("Environment")))
                return false;

            else
                return true;
        }

        return false;
    }
}
