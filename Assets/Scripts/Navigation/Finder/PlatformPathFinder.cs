using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Navigation
{
    public class PlatformPathFinder : PathFinder
    {
        private PlatformGraph platformGraph => (PlatformGraph)curGraph;

        public PlatformPathFinder(GameObject go) : base(go)
        { }


        /// <summary>
        /// From a given node calculate all available options through the graph
        /// Determine if option can reach targetNode
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="startNode"></param>
        /// <param name="targetNode"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public override async Task<Edge> GetNextRoute(GraphNode node)
        {
            if (!visitedNodes.Contains(node))
                visitedNodes.Add(node);

            List<Edge> availableRoutes = GetAvailableRoutes(node);

            //No path points found
            if (availableRoutes.Count == 0)
                return null;

            //One path point found
            else if (availableRoutes.Count == 1)
            {
                if (availableRoutes[0].Type == EdgeType.Jump)
                    PreventBacktracking(node);

                return availableRoutes[0];
            }

            //Multiple path points found
            else
            {
                int rand = UnityEngine.Random.Range(0, availableRoutes.Count);

                if (availableRoutes[rand].Type == EdgeType.Jump)
                    PreventBacktracking(node);

                return availableRoutes[rand];
            }
        }


        /// <summary>
        /// Makes it so all nodes from previous movement secion cant be jump to again
        /// Helps prevent back tracking
        /// </summary>
        /// <param name="node"></param>
        private void PreventBacktracking(GraphNode node)
        {
            foreach (GraphNode connectedNode in curGraph.GetAllConnectingNodes(node, new List<EdgeType>() { EdgeType.Ground }))
            {
                if (!visitedNodes.Contains(connectedNode))
                    visitedNodes.Add(connectedNode);
            }
        }


        /// <summary>
        /// Return a list of all possible movement and jump pathpoints
        /// </summary>
        /// <returns></returns>
        protected override List<Edge> GetAvailableRoutes(GraphNode node)
        {
            List<Edge> routes = new List<Edge>();

            //Get possible move points
            routes.AddRange(GetAvailableMovePathPoints(node));

            //Get possible jump points
            routes.AddRange(GetAvailableJumpPathPoints(node));

            return routes;
        }


        #region Movement Path Points

        /// <summary>
        /// Return a list of possible GroundEdges that are connected to the node provided
        /// </summary>
        /// <param name="pathPoint"></param>
        /// <returns></returns>
        private List<Edge> GetAvailableMovePathPoints(GraphNode node)
        {
            List<Edge> moveRoutes = new List<Edge>();

            foreach (Edge edge in node.GetEdgesOfType(EdgeType.Ground))
            {
                //Check if already visited
                if (!visitedNodes.Contains(edge.EndNode))
                {
                    //Check that graph endNode can be reached
                    if (curGraph.CheckForConnection(edge.EndNode, curGraph.EndNode, null, new List<GraphNode>(visitedNodes)))
                    {
                        moveRoutes.Add(edge);
                    }
                }
            }

            return moveRoutes;
        }


        #endregion


        #region Jump Path Points

        /// <summary>
        /// Return a list of possible jumpEdges
        /// If node is an edge node, then determine if any other node can be jumped to
        /// If node is not an edge node, then determine if any edge nodes can be jumped to
        /// Cant jump to a node that is connected by ground edges
        /// </summary>
        /// <param name="pathPoint"></param>
        /// <returns></returns>
        private List<Edge> GetAvailableJumpPathPoints(GraphNode node)
        {
            List<Edge> jumpRoutes = new List<Edge>();

            //Edge node (node with single ground connecing edge)
            if (IsEdgeNode(node))
            {
                foreach (JumpEdge edge in node.GetEdgesOfType(EdgeType.Jump))
                {
                    if (!visitedNodes.Contains(edge.EndNode))
                    {
                        //Check that endNode can be reached
                        if (curGraph.CheckForConnection(edge.EndNode, curGraph.EndNode, null, new List<GraphNode>(visitedNodes)))
                            jumpRoutes.Add(edge);
                    }
                }
            }

            //Non Edge Node
            else
            {
                foreach (JumpEdge edge in node.GetEdgesOfType(EdgeType.Jump))
                {
                    if (!visitedNodes.Contains(edge.EndNode))
                    {
                        //Determine if connecting node is an edge node
                        if (IsEdgeNode(edge.EndNode))
                        {
                            //Check that endNode can be reached
                            if (curGraph.CheckForConnection(edge.EndNode, curGraph.EndNode, null, new List<GraphNode>(visitedNodes)))
                                jumpRoutes.Add(edge);
                        }
                    }
                }
            }

            return jumpRoutes;
        }


        /// <summary>
        /// Returns wether the provided node is an edge node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        //NOTE: A edge node is a node that has only one connecting ground edge. All other edges are jumps
        private bool IsEdgeNode(GraphNode node)
        {
            if (node.GetEdgesOfType(EdgeType.Ground).Count == 1)
                return true;

            return false;
        }

        #endregion
    }
}
