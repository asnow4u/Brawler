
using Game.SceneObjects.Movement;

namespace Game.Navigation
{
    public class FlyingPathNavigator : PathNavigator
    {
        protected override void CreatePathFinder()
        {
            pathFinder = new FlyingPathFinder(gameObject);
        }


        protected override Graph CreateGraph(TerrainNode startNode, TerrainNode endNode)
        {
            if (moveHandler != null)
            {
                GraphFactory graphFactory = new GraphFactory();
                return graphFactory.CreateGraph(GraphType.Flying, startNode, endNode, bounds, moveHandler.CurrentMovementCollection);
            }

            return null;
        }

        protected override void PerformMovement()
        {
        
        }
    }
}
