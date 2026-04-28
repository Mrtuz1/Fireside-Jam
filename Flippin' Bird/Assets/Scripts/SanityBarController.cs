using UnityEngine;
using UnityEngine.UI;

public class SanityBarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage; // Containing sprite'ının Image bileşeni
    
    [Header("Settings")]
    [SerializeField] private float updateSpeed = 5f; // Barın yumuşak hareket etmesi için hız

    private float targetFillAmount;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            // Başlangıç değerini ayarla
            UpdateBarInstant(GameManager.Instance.sanity);
            
            // GameManager'daki event'e abone ol
            GameManager.Instance.OnSanityChanged += HandleSanityChanged;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSanityChanged -= HandleSanityChanged;
        }
    }

    private void Update()
    {
        // Image.fillAmount değerini hedef değere doğru yumuşak bir şekilde yaklaştır
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Lerp(fillImage.fillAmount, targetFillAmount, Time.deltaTime * updateSpeed);
            
            // Renk geçişi: Beyaz (1,1,1) -> Koyu Kırmızı (0.5,0,0)
            // fillAmount 1 iken beyaz, 0'a yaklaştıkça kırmızı olur.
            Color targetColor = Color.Lerp(new Color(0.5f, 0f, 0f, 1f), Color.white, fillImage.fillAmount);
            fillImage.color = targetColor;
        }
    }

    private void HandleSanityChanged(float newSanity)
    {
        // Sanity 0-100 arası olduğu için 100'e bölerek 0-1 arasına çekiyoruz
        targetFillAmount = newSanity / 100f;
    }

    private void UpdateBarInstant(float currentSanity)
    {
        targetFillAmount = currentSanity / 100f;
        if (fillImage != null)
        {
            fillImage.fillAmount = targetFillAmount;
        }
    }

    // --- TEST BUTONLARI (Inspector'da script'e sağ tıklayarak kullanabilirsin) ---
    [ContextMenu("Test - Decrease Sanity")]
    public void TestDecrease() => GameManager.Instance.ModifySanity(-10);

    [ContextMenu("Test - Increase Sanity")]
    public void TestIncrease() => GameManager.Instance.ModifySanity(10);
}
