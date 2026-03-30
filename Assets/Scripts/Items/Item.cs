using UnityEngine;

[RequireComponent(typeof(Collider))]
internal abstract class Item : MonoBehaviour, IItem
{
    [SerializeField] private float mass;
    public float Mass => mass;

    private Collider interactionCollider;

    protected virtual void Awake()
    {
        if (mass == 0)
            Debug.LogError("Mass not set for " + name, this);

        interactionCollider = GetComponent<Collider>();
        interactionCollider.isTrigger = true;
    }

    public void EnableInteraction()
    {
        interactionCollider.enabled = true;
    }

    public void DisableInteraction()
    {
        interactionCollider.enabled = false;
    }
}

