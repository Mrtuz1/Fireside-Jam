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

    [Header("Day Cycle Settings")]
    public float dayDuration = 120f; // 2 dakika
    public float baseDailyRent = 25f; // Başlangıç kirası
    public float rentMultiplier = 1.3f; // Her gün kiranın katlanma oranı
    private float currentRent;
    public bool isDayActive = false;
    private float dayTimer;

    [Header("Day Cycle UI")]
    public TMP_Text timerText;
    public GameObject endOfDayCanvas;
    public GameObject gameOverCanvas;
    public TMP_Text endOfDaySummaryText;

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
        InitializeGameVariables();
    }

    private void Update()
    {
        if (isDayActive)
        {
            dayTimer -= Time.deltaTime;
            UpdateTimerUI();

            if (dayTimer <= 0f)
            {
                EndDay();
            }
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(dayTimer / 60f);
            int seconds = Mathf.FloorToInt(dayTimer % 60f);
            timerText.text = $"{minutes}:{seconds:00}";
        }
    }

    private void InitializeGameVariables()
    {
        currentDay = 1;
        money = 20f;
        sanity = 100;
        currentRent = baseDailyRent;

        dayTimer = dayDuration;
        isDayActive = true;
        Time.timeScale = 1f;

        if (endOfDayCanvas != null) endOfDayCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        if (moneyText != null) moneyText.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);

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
        currentRent *= rentMultiplier; // Kirayı katla
        
        OnDayChanged?.Invoke(currentDay);
        Debug.Log($"[GameManager] Yeni güne geçildi. Gün: {currentDay}, Yeni Kira: {currentRent:F2}$");
    }

    private void EndDay()
    {
        isDayActive = false;
        Time.timeScale = 0f; // Oyunu durdur, ocaktakiler yanmasın

        // HUD'ı gizle
        if (moneyText != null) moneyText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);

        // Eldeki malzemeyi sil
        if (PlayerHand.Instance != null && PlayerHand.Instance.heldIngredient != null)
        {
            Destroy(PlayerHand.Instance.heldIngredient.gameObject);
            PlayerHand.Instance.heldIngredient = null;
        }

        // Müşteriyi gönder
        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.EndDayForceLeave();
        }

        // Kirayı kes
        money -= currentRent;
        OnMoneyChanged?.Invoke(money);

        if (money < 0f)
        {
            // Game Over
            if (gameOverCanvas != null) gameOverCanvas.SetActive(true);
        }
        else
        {
            // Next Day Screen
            if (endOfDayCanvas != null)
            {
                endOfDayCanvas.SetActive(true);
                if (endOfDaySummaryText != null)
                {
                    float nextRent = currentRent * rentMultiplier;
                    endOfDaySummaryText.text = 
                        $"DAY {currentDay} COMPLETED!\n\n" +
                        $"Rent Paid: -${currentRent:F2}\n" +
                        $"Remaining Money: ${money:F2}\n\n" +
                        $"WARNING: Tomorrow's rent will be ${nextRent:F2}!\n" +
                        $"Work faster or risk losing it all!";
                }
            }
        }
    }

    public void StartNextDay()
    {
        if (endOfDayCanvas != null) endOfDayCanvas.SetActive(false);
        if (gameOverCanvas != null) gameOverCanvas.SetActive(false);

        NextDay(); // currentDay++
        
        dayTimer = dayDuration;
        isDayActive = true;
        Time.timeScale = 1f;

        if (moneyText != null) moneyText.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);

        if (CustomerManager.Instance != null)
        {
            CustomerManager.Instance.SpawnNewCustomer();
        }
    }

    public void RestartGame()
    {
        // Sahnemizi baştan yükler, böylece her şey (ızgara, çöpler vb) sıfırlanır
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
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
