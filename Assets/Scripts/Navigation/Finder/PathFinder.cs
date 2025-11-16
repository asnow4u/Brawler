using Game.SceneObjects.Movement;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Navigation
{
    public abstract class PathFinder
    {
        protected GameObject go;
        protected List<GraphNode> visitedNodes = new List<GraphNode>();

        protected Graph curGraph;
        protected MovementCollection moveCollection;

        //Getters
        protected Bounds bounds => go.GetComponent<CapsuleCollider>().bounds;
        protected Rigidbody rb => go.GetComponent<Rigidbody>();

        public PathFinder(GameObject go)
        {
            this.go = go;
        }

        /// <summary>
        /// Setup finder
        /// Graph is what will be used to refference connections
        /// MovementCollection will be used to determine if connections are possible
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="collection"></param>
        public void Setup(Graph graph, MovementCollection collection)
        {
            curGraph = graph;
            moveCollection = collection;
        }

        public abstract Task<Edge> GetNextRoute(GraphNode node);

        protected abstract List<Edge> GetAvailableRoutes(GraphNode node);
    }
}
