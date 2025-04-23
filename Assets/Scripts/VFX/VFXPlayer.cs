using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class VFXPlayer : MonoBehaviour
{
    [SerializeField] private bool playOnAwake;
    [SerializeField] private bool destroyWhenFinished;

    private VisualEffect vfx;

    void Start()
    {
        vfx = GetComponent<VisualEffect>();

        if (playOnAwake)
            PlayVFX();
    }

    private void Update()
    {
        if (destroyWhenFinished && vfx.aliveParticleCount == 0)
        {
            Destroy(gameObject);
        }
    }


    public void PlayVFX()
    {
        vfx.Play();
    }


    public void StopVFX()
    {
        vfx.Stop();
    }

}
