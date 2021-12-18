using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FistController : MonoBehaviour
{
    public GameObject player;

    public float punchTime;
    public float punchDist;

    private bool fistLaunch;
    private bool fistReturn;

    private Vector3 dir;
    private Vector3 originalPos;
    private float currentPunchTime;

    // Start is called before the first frame update
    void Start()
    {
        fistLaunch = false;
        fistReturn = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (fistLaunch)
        {

          currentPunchTime += Time.deltaTime;

          if (currentPunchTime > punchTime)
          {
              currentPunchTime = punchTime;
              fistLaunch = false;
              fistReturn = true;
          }

          float increment = currentPunchTime / punchTime;
          increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
          transform.position = Vector3.Lerp(originalPos, originalPos + dir * punchDist, increment);
        }

        if (fistReturn)
        {

          currentPunchTime -= Time.deltaTime;

          if (currentPunchTime < 0f)
          {
              currentPunchTime = 0f;
              fistReturn = false;
              transform.parent = player.transform;
          }

          float increment = currentPunchTime / punchTime;
          increment = Mathf.Sin(increment * Mathf.PI * 0.5f); //https://chicounity3d.wordpress.com/2014/05/23/how-to-lerp-like-a-pro/
          transform.position = Vector3.Lerp(player.transform.position, originalPos + dir * punchDist, increment);
        }
    }

    public void LaunchFist(){

      transform.parent = null;
      dir = new Vector3(1, 0, 0); //TODO: switch to direction towards mouse
      originalPos = transform.position;
      currentPunchTime = 0f;
      fistLaunch = true;

    }
}
