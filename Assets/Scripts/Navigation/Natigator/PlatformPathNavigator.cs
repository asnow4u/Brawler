
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
            if (moveHandler != null)
            {
                GraphFactory graphFactory = new GraphFactory();
                return graphFactory.CreateGraph(GraphType.Platform, startNode, endNode, bounds, moveHandler.CurrentMovementCollection);
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
                if (TryGetComponent(out MovementHandler moveHandler))
                {
                    if ((moveHandler.IsFacingRightDirection && jumpRoute.InitialVelocity < 0f) ||
                        !moveHandler.IsFacingRightDirection && jumpRoute.InitialVelocity > 0f)
                    {
                        moveHandler.TurnAround();
                    }
                }

                TriggerMovementEvent(EdgeType.Jump, jumpRoute.JumpYInfluence);
            }
        }
    }
}

