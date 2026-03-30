using Game.SceneObjects;
using UnityEngine;

//NOTE: Solid is not yet implemented. Would want to make a seperate class for it having killzone be an abstract class
public enum KillZoneType { Left, Right, Top, LeftSolid, RightSolid, TopSolid }

public class KillZone : MonoBehaviour
{
    protected KillZoneType type;
    protected Transform target;
    protected GameObject deathVFX;

    public virtual void Initialize(KillZoneType type, Transform objTransform, GameObject deathVFX = null) 
    {
        this.type = type;   
        this.target = objTransform;
        this.deathVFX = deathVFX;
    }


    protected virtual void Update()
    {
        CheckForKill();
    }


    /// <summary>
    /// Check if the objects transform has passed the killzone and should be destroyed
    /// </summary>
    private void CheckForKill()
    {
        if (target != null)
        {
            if (type == KillZoneType.Left && target.position.x < transform.position.x)
                KillObject();

            else if (type == KillZoneType.Right && target.position.x > transform.position.x)
                KillObject();

            else if (type == KillZoneType.Top && target.position.y > transform.position.y)
                KillObject();
        }
    }


    /// <summary>
    /// Kill the object that has passed the killzone
    /// </summary>
    private void KillObject()
    {
        //if (target.TryGetComponent(out SceneObject sceneObject))
        //{
        //    if (sceneObject is Player player)
        //    {
        //        //TODO: Handle player death
        //    }
        //    else
        //    {
        //        SpawnDeathVFX();       
        //        Destroy(sceneObject.gameObject);
        //    }
        //}
    }


    /// <summary>
    /// Spawn the death VFX at the killzone position
    /// </summary>
    private void SpawnDeathVFX()
    {
        if (deathVFX == null)
            return;

        switch (type)
        {
            case KillZoneType.Left:
                Instantiate(deathVFX, target.position, Quaternion.Euler(-90, 0, 0));
                break;

            case KillZoneType.Right:
                Instantiate(deathVFX, target.position, Quaternion.Euler(-90, 180, 0));
                break;
        }
    }
}
