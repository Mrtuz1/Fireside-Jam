using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SanityVFXManager : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume sanityVolume;

    // Aktif efektlerin referansları
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private LensDistortion lensDistortion;

    private void Start()
    {
        // Eğer atanmadıysa bu objedeki Volume'u bulmaya çalış
        if (sanityVolume == null) sanityVolume = GetComponent<Volume>();

        // Volume profilinden efektleri çek
        if (sanityVolume != null && sanityVolume.profile != null)
        {
            sanityVolume.profile.TryGet(out vignette);
            sanityVolume.profile.TryGet(out chromatic);
            sanityVolume.profile.TryGet(out colorAdjustments);
            sanityVolume.profile.TryGet(out filmGrain);
            sanityVolume.profile.TryGet(out lensDistortion);
        }

        // GameManager olayına abone ol
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSanityChanged += UpdateEffects;
            // Başlangıç değerini uygula
            UpdateEffects(GameManager.Instance.sanity);
        }
    }

    private void OnDestroy()
    {
        // 1. Hafıza sızıntısını önlemek için abonelikten çık
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.OnSanityChanged -= UpdateEffects;
        }

        // 2. Unity'nin yarattığı geçici klon profili hafızadan sil ki Editor sapıtmasın! (HATA ÇÖZÜMÜ)
        if (sanityVolume != null && sanityVolume.HasInstantiatedProfile())
        {
            Destroy(sanityVolume.profile);
        }
    }

    private void UpdateEffects(int sanity)
    {
        // Gelen sanity değeri ne olursa olsun, tehlike oranını 
        // KESİNLİKLE 0 (güvenli) ile 1 (tehlike) arasında tut (Hataları önler).
        float dangerLevel = Mathf.Clamp01(1f - (sanity / 100f));

        // --- Efekt Güncellemeleri (.Override() kullanımı güvenlidir) ---

        if (vignette != null)
        {
            // Köşe kararması maks %50
            vignette.intensity.Override(dangerLevel * 0.5f);
        }

        if (colorAdjustments != null)
        {
            // Renklerin aşırı doygunlaşması (Halüsinasyon) maks 50
            colorAdjustments.saturation.Override(dangerLevel * 50f);
        }

        if (chromatic != null)
        {
            // Renk kayması maks 0.3
            chromatic.intensity.Override(dangerLevel * 0.3f);
        }

        if (filmGrain != null)
        {
            // Karıncalanma maks 1.0 (Tam karlanma)
            filmGrain.intensity.Override(dangerLevel * 1f);
        }

        if (lensDistortion != null)
        {
            // Lens bozulması: akıl sağlığı azaldıkça 0'dan -0.3'e gider
            lensDistortion.intensity.Override(-dangerLevel * 0.3f);
        }
    }
}