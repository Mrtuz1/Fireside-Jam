using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
public class SanityVFXManager : MonoBehaviour
{
    [Header("Volume Reference")]
    [SerializeField] private Volume sanityVolume;
    private Vignette vignette;
    private ChromaticAberration chromatic;
    private ColorAdjustments colorAdjustments;
    private FilmGrain filmGrain;
    private LensDistortion lensDistortion;
    private void Start()
    {
        if (sanityVolume == null) sanityVolume = GetComponent<Volume>();
        if (sanityVolume != null && sanityVolume.profile != null)
        {
            sanityVolume.profile.TryGet(out vignette);
            sanityVolume.profile.TryGet(out chromatic);
            sanityVolume.profile.TryGet(out colorAdjustments);
            sanityVolume.profile.TryGet(out filmGrain);
            sanityVolume.profile.TryGet(out lensDistortion);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSanityChanged += UpdateEffects;
            UpdateEffects(GameManager.Instance.sanity);
        }
    }
    private void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnSanityChanged -= UpdateEffects;
    }
    private void UpdateEffects(int sanity)
    {
        // 0 (güvende) ile 1 (maksimum tehlike) arası oran
        float dangerLevel = 1f - (sanity / 100f);
        if (vignette != null)
        {
            // İsteğin: Maksimum %50 (0.5f) kararma
            vignette.intensity.value = dangerLevel * 0.5f;
        }
        if (colorAdjustments != null)
        {
            // İsteğin: Akıl sağlığı düştükçe renklerin aşırı patlaması (Max 100)
            colorAdjustments.saturation.value = dangerLevel * 100f;
        }
        // Diğer efektleri de bu rahatsız edici havayı desteklemesi için koruyoruz:
        if (chromatic != null)
        {
            chromatic.intensity.value = dangerLevel * 1f;
        }
        if (filmGrain != null)
        {
            filmGrain.intensity.value = dangerLevel * 1f;
        }
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = dangerLevel * -0.4f;
        }
    }
}