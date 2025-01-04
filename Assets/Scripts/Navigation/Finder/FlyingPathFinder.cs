using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Navigation 
{ 
    public class FlyingPathFinder : PathFinder
    {
        public FlyingPathFinder(GameObject go) : base(go)
        { }

        public override Task<Edge> GetNextRoute(GraphNode node)
        {
            throw new System.NotImplementedException();
        }

        protected override List<Edge> GetAvailableRoutes(GraphNode node)
        {
            throw new System.NotImplementedException();
        }
    }
}
