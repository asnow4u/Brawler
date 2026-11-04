using UnityEngine;

public class DisapearingPlatform : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "Player")
        {
            Destroy(gameObject);
        }
    }
}
