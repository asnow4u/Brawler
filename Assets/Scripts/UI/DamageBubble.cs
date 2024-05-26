using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class DamageBubble : MonoBehaviour
{
    private const float timer = 0.25f;
    private const float speed = 5f;

    public void Initialize(float damageValue)
    {
        TextMeshProUGUI textMesh = GetComponentInChildren<TextMeshProUGUI>();

        textMesh.text = (Mathf.Round(damageValue * 10f) * 0.1f).ToString();

        StartCoroutine(MovementTimer());
    }


    private IEnumerator MovementTimer()
    {
        float time = 0;

        while (time < timer)
        {
            transform.position += transform.up * speed * Time.deltaTime;
            time += Time.deltaTime;

            yield return null;
        }

        Destroy(gameObject);
    } 
}
