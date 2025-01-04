
using Game.SceneObjects;
using Game.SceneObjects.Movement;

namespace Game.Navigation
{

    public class PlatformPathNavigator : PathNavigator
    {
        private PlatformPathFinder platformPathFinder => (PlatformPathFinder)pathFinder;

        protected override void CreatePathFinder()
        {
            pathFinder = new PlatformPathFinder(gameObject);
        }


        protected override Graph CreateGraph(TerrainNode startNode, TerrainNode endNode)
        {
            if (moveHandler.TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
            {
                GraphFactory graphFactory = new GraphFactory();
                return graphFactory.CreateGraph(GraphType.Platform, startNode, endNode, bounds, curMovementCollection);
            }

            return null;
        }


        /// <summary>
        /// Perform movement towards the curPathPoint
        /// Movement will either be moving on the ground or jumping
        /// </summary>
        protected override void PerformMovement()
        {
            //Movement
            if (curRoute.Type == EdgeType.Ground)
            {
                //Right Direction
                if (transform.position.x < curRoute.EndNode.Pos.x)
                    TriggerMovementEvent(EdgeType.Ground, 1f);

                //Left Direction
                else
                    TriggerMovementEvent(EdgeType.Ground, -1f);
            }

            //Jump
            else if (curRoute.Type == EdgeType.Jump)
            {
                JumpEdge jumpRoute = (JumpEdge)curRoute;

                //Move
                TriggerMovementEvent(EdgeType.Ground, 0);

                //Face the direction of the jump
                if (TryGetComponent(out SceneObject sceneObject))
                {
                    if ((sceneObject.IsFacingRightDirection() && jumpRoute.InitialVelocity < 0f) ||
                        !sceneObject.IsFacingRightDirection() && jumpRoute.InitialVelocity > 0f)
                    {
                        sceneObject.TurnAround();
                    }
                }

                TriggerMovementEvent(EdgeType.Jump, jumpRoute.JumpYInfluence);
            }
        }
    }
}

