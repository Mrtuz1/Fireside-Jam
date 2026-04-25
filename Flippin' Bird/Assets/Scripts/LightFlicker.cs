using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LightFlicker : MonoBehaviour
{
    private Light2D light2D;
    private float baseIntensity;

    [Header("Settings")]
    public float flickerInterval = 25f;
    public int flickerCount = 3;
    public float flickerDuration = 0.1f;
    
    private float timer;

    void Awake()
    {
        light2D = GetComponent<Light2D>();
        if (light2D != null)
        {
            baseIntensity = light2D.intensity;
        }
    }

    void Start()
    {
        timer = flickerInterval;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            StartCoroutine(FlickerRoutine());
            timer = flickerInterval;
        }
    }

    IEnumerator FlickerRoutine()
    {
        if (light2D == null) yield break;

        for (int i = 0; i < flickerCount; i++)
        {
            // Sön
            light2D.intensity = 0f;
            yield return new WaitForSeconds(flickerDuration);
            
            // Yan
            light2D.intensity = baseIntensity * Random.Range(0.8f, 1.1f);
            yield return new WaitForSeconds(flickerDuration * 0.5f);
        }

        // Son olarak tam yan
        light2D.intensity = baseIntensity;
    }
}
