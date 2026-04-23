using UnityEngine;
using System;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Core State")]
    public int currentDay = 1;
    public float money = 20f;
    [Range(0, 100)] public int sanity = 100;

    [Header("Economy Settings")]
    [Tooltip("Burgerin maliyetinin kaç katına satılacağı (Örn: 2 ise 1$ maliyetli burger 2$'a satılır, net kâr 1$ olur)")]
    public float profitMultiplier = 2f;

    // Diğer scriptlerin abone olabileceği Eventler (Olaylar)
    public event Action<int> OnDayChanged;
    public event Action<float> OnMoneyChanged;
    public event Action<int> OnSanityChanged;

    [Header("UI References")]
    [SerializeField] private TMP_Text moneyText;
    [Tooltip("Para artış/azalışında çıkacak kayan yazı prefab'ı")]
    [SerializeField] private GameObject floatingTextPrefab;
    [Tooltip("Kayan yazının nerede çıkacağı. Boş bırakılırsa ana paranın bulunduğu yerden çıkar.")]
    [SerializeField] private Transform floatingTextSpawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        OnMoneyChanged += UpdateMoneyUI;
    }

    private void OnDestroy()
    {
        OnMoneyChanged -= UpdateMoneyUI;
    }

    private void Start()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        currentDay = 1;
        money = 20f;
        sanity = 100;

        OnDayChanged?.Invoke(currentDay);
        OnMoneyChanged?.Invoke(money);
        OnSanityChanged?.Invoke(sanity);
    }

    // ==========================================
    // PARA (ECONOMY) YÖNETİMİ
    // ==========================================

    public float GetIngredientCost(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.BottomBun:
            case IngredientType.TopBun:
                return 0.50f;
            case IngredientType.RawPatty:
            case IngredientType.CookedPatty:
            case IngredientType.BurntPatty:
                return 1.50f;
            case IngredientType.Cheese:
                return 0.75f;
            case IngredientType.Lettuce:
                return 0.30f;
            case IngredientType.Tomato:
                return 0.40f;
            default:
                return 0f;
        }
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0f) return;
        money += amount;
        
        OnMoneyChanged?.Invoke(money);
        Debug.Log($"[GameManager] +{amount:F2}$ Kazanıldı. Güncel Kasa: {money:F2}$");

        SpawnFloatingText($"+${amount:F2}", Color.green);
    }

    public void RemoveMoney(float amount)
    {
        if (amount <= 0f) return;
        money -= amount;
        
        OnMoneyChanged?.Invoke(money);
        Debug.Log($"[GameManager] -{amount:F2}$ Harcandı. Güncel Kasa: {money:F2}$");

        SpawnFloatingText($"-${amount:F2}", Color.red);
    }

    private void SpawnFloatingText(string text, Color color)
    {
        if (floatingTextPrefab != null && moneyText != null)
        {
            Transform parentTransform = floatingTextSpawnPoint != null ? floatingTextSpawnPoint : moneyText.transform.parent;
            
            // Paranın pozisyonunda oluşturup aynı UI hiyerarşisine ekle
            GameObject floatingObj = Instantiate(floatingTextPrefab, moneyText.transform.position, Quaternion.identity, parentTransform);
            
            FloatingText ft = floatingObj.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.Setup(text, color);
            }
        }
    }

    private void UpdateMoneyUI(float currentMoney)
    {
        if (moneyText != null)
        {
            moneyText.text = $"${currentMoney:F2}";
        }
    }

    // ==========================================
    // GÜN (DAY) YÖNETİMİ
    // ==========================================

    public void NextDay()
    {
        currentDay++;
        
        OnDayChanged?.Invoke(currentDay);
        Debug.Log($"[GameManager] Yeni güne geçildi. Gün: {currentDay}");
    }

    // ==========================================
    // AKIL SAĞLIĞI (SANITY) YÖNETİMİ
    // ==========================================

    public void ModifySanity(int amount)
    {
        sanity = Mathf.Clamp(sanity + amount, 0, 100);
        
        OnSanityChanged?.Invoke(sanity);
        Debug.Log($"[GameManager] Akıl sağlığı değişti ({amount}). Güncel: %{sanity}");
    }
}
