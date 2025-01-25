
namespace Game.SceneObjects
{
    public class TestDummy : Enemy
    {
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (CurGroundedState == GroundedState.Airborn)
            {
                MovementInputHandler.PerformMovement(new UnityEngine.Vector2(0, 0));
            }

            else
            {
                MovementInputHandler.PerformMovement(new UnityEngine.Vector2(0, 0));
            }
        }
    }
}
