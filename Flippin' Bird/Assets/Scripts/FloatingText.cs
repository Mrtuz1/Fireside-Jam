using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    [Header("Settings")]
    public float floatSpeed = 50f; // UI üzerinde aşağı gitme hızı
    public float fadeDuration = 1.5f;

    private TMP_Text textMesh;
    private RectTransform rectTransform;

    public void Setup(string text, Color color)
    {
        textMesh = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
        
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
        }

        StartCoroutine(FloatAndFade());
    }

    private IEnumerator FloatAndFade()
    {
        float elapsed = 0f;
        if (textMesh == null) yield break;

        Color startColor = textMesh.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            // RectTransform ile UI üzerinde aşağı doğru hareket ettir
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition += Vector2.down * floatSpeed * Time.deltaTime;
            }

            // Saydamlığı (Alpha) yavaşça 0'a çek
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, t);
            textMesh.color = newColor;

            yield return null;
        }

        // Animasyon bitince objeyi yok et
        Destroy(gameObject);
    }
}
