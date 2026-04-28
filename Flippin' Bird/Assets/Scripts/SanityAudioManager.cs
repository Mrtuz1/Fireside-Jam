using UnityEngine;
using System.Collections;

public class SanityAudioManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float checkInterval = 20f;
    [SerializeField] private float triggerChance = 0.5f;
    [SerializeField] private int sanityThreshold = 50;
    
    [SerializeField] private GameObject horrorLight;
    [SerializeField] private float soundDuration = 5f;
    
    private float timer;
    private Coroutine lightCoroutine;

    private void Start()
    {
        timer = checkInterval;
        if (horrorLight != null) horrorLight.SetActive(false);
    }

    private void Update()
    {
        // Sadece gün aktifse ve akıl sağlığı düşükse çalışsın
        if (GameManager.Instance == null || !GameManager.Instance.isDayActive)
            return;

        if (GameManager.Instance.sanity >= sanityThreshold)
        {
            // Eşik değerinin üstündeysek timer'ı donduruyoruz
            timer = checkInterval; 
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = checkInterval;
            CheckAndPlaySanitySound();
        }
    }

    private void CheckAndPlaySanitySound()
    {
        float roll = Random.value;
        if (roll <= triggerChance)
        {
            // "1", "2" veya "3" isimli seslerden birini rastgele seç
            string randomSound = Random.Range(1, 4).ToString(); 
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayOneShot(randomSound);
                Debug.Log($"[SanityAudioManager] Oynatılan Ses: {randomSound}");
                
                if (horrorLight != null)
                {
                    if (lightCoroutine != null) StopCoroutine(lightCoroutine);
                    lightCoroutine = StartCoroutine(ToggleHorrorLight());
                }
            }
        }
        else
        {
            Debug.Log("[SanityAudioManager] Sanity düşük ama %50 şans gelmedi, bu tur ses çalmayacak.");
        }
    }

    private IEnumerator ToggleHorrorLight()
    {
        Debug.Log("[SanityAudioManager] Işık AÇILDI");
        horrorLight.SetActive(true);
        yield return new WaitForSeconds(soundDuration);
        horrorLight.SetActive(false);
        Debug.Log("[SanityAudioManager] Işık KAPATILDI");
        lightCoroutine = null;
    }
}
