using System.Collections;
using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    public Light[] lights;                 // drag your two point lights here
    public float averageInterval = 120f;   // mean seconds between flicker events
    public float flickerDuration = 0.6f;   // how long each event lasts

    float[] baseIntensity;

    void Start()
    {
        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
        StartCoroutine(FlickerLoop());
    }

    IEnumerator FlickerLoop()
    {
        while (true)
        {
            // Exponential distribution: random gaps, mean = averageInterval
            float wait = -Mathf.Log(Mathf.Max(1f - Random.value, 1e-6f)) * averageInterval;
            yield return new WaitForSeconds(wait);

            float t = 0f;
            while (t < flickerDuration)
            {
                for (int i = 0; i < lights.Length; i++)
                    lights[i].intensity = baseIntensity[i] * Random.Range(0f, 1f);

                float step = Random.Range(0.03f, 0.1f);
                yield return new WaitForSeconds(step);
                t += step;
            }

            for (int i = 0; i < lights.Length; i++)
                lights[i].intensity = baseIntensity[i];
        }
    }

    void OnDisable()
    {
        if (baseIntensity == null) return;
        for (int i = 0; i < lights.Length; i++)
            lights[i].intensity = baseIntensity[i];
    }
}