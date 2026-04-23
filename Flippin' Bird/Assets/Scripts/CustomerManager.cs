using UnityEngine;

/// <summary>
/// Müşteri karakterini modüler parçalardan oluşturur.
/// Her çağrıda aynı konumdaki transform'lara rastgele sprite atar.
/// 
/// GameObject hiyerarşisi beklentisi:
///   CustomerRoot
///     ├── Hair          (SpriteRenderer)
///     ├── Face          (SpriteRenderer)
///     ├── Eye           (SpriteRenderer)
///     ├── Nose          (SpriteRenderer)
///     ├── Mouth         (SpriteRenderer)
///     └── Cloth         (SpriteRenderer)
/// 
/// Inspector'dan spriteSheet alanına FlippinBird-Sheet texture'ını sürükleyin.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }
    public bool isOrderCompleted = false;

    [Header("Sprite Sheet")]
    [Tooltip("FlippinBird-Sheet texture asset'ini buraya sürükleyin.")]
    public Texture2D spriteSheet;

    [Header("Character Parts (Child Transforms)")]
    public SpriteRenderer hairRenderer;
    public SpriteRenderer faceRenderer;
    public SpriteRenderer eyeRenderer;
    public SpriteRenderer noseRenderer;
    public SpriteRenderer mouthRenderer;
    public SpriteRenderer clothRenderer;

    // ---- Sprite grupları ----
    // Face_1, Face_2, Face_3
    private static readonly string[] FaceNames  = { "Face_1",  "Face_2",  "Face_3"  };
    // Eye_1, Eye_2, Eye_3
    private static readonly string[] EyeNames   = { "Eye_1",   "Eye_2",   "Eye_3"   };
    // Nose_1, Nose_2, Nose_3
    private static readonly string[] NoseNames  = { "Nose_1",  "Nose_2",  "Nose_3"  };
    // Mouth_1, Mouth_2, Mouth_3
    private static readonly string[] MouthNames = { "Mouth_1", "Mouth_2", "Mouth_3" };
    // Hair1_1 … Hair3_3  (9 adet)
    private static readonly string[] HairNames  =
    {
        "Hair1_1", "Hair1_2", "Hair1_3",
        "Hair2_1", "Hair2_2", "Hair2_3",
        "Hair3_1", "Hair3_2", "Hair3_3"
    };
    // Cloth1_1 … Cloth3_3  (9 adet)
    private static readonly string[] ClothNames =
    {
        "Cloth1_1", "Cloth1_2", "Cloth1_3",
        "Cloth2_1", "Cloth2_2", "Cloth2_3",
        "Cloth3_1", "Cloth3_2", "Cloth3_3"
    };

    // Tüm sprite'ların cache'lendiği dizi (ilk çağrıda doldurulur)
    private Sprite[] _allSprites;

    // Her slot için bir önceki seçimi hatırla (aynısının tekrar seçilmesini engeller)
    private string _prevHair;
    private string _prevFace;
    private string _prevEye;
    private string _prevNose;
    private string _prevMouth;
    private string _prevCloth;

    // -------------------------------------------------------
    [Header("Movement Settings")]
    public Transform startPos;
    public Transform exitPos;
    public float leaveDuration = 3f; // Müşterinin çıkışa yürüme süresi (daha yavaş)
    private bool isLeaving = false;

    [Header("Emojis")]
    [Tooltip("0: En iyi (81-100), 1: (61-80), 2: (41-60), 3: (21-40), 4: En kötü (0-20)")]
    public Sprite[] emojiSprites;
    public SpriteRenderer emojiRenderer;

    [Header("Patience System")]
    public float maxWaitTime = 20f;
    public SpriteRenderer patienceRenderer;
    [Tooltip("4 adet bekleme durumu sprite'ı. 0: En mutlu, 3: Sinirli")]
    public Sprite[] patienceSprites;
    private float currentWaitTime = 0f;
    private bool isWaiting = false;

    // -------------------------------------------------------

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SpawnNewCustomer();
    }

    private void Update()
    {
        if (isWaiting && !isOrderCompleted)
        {
            currentWaitTime += Time.deltaTime;
            UpdatePatienceVisuals();

            if (currentWaitTime >= maxWaitTime)
            {
                // Süre bitti, müşteri sinirlenip gidiyor
                isWaiting = false;
                CustomerFailed();
            }
        }
    }

    public void SpawnNewCustomer()
    {
        GenerateRandomCustomer();
        // Gün sayısını GameManager'dan alıyoruz
        int currentDay = GameManager.Instance != null ? GameManager.Instance.currentDay : 1;
        GenerateOrder(currentDay);

        // Bekleme süresini sıfırla ve başlat
        currentWaitTime = 0f;
        isWaiting = true;
        UpdatePatienceVisuals();
    }

    private void UpdatePatienceVisuals()
    {
        if (patienceRenderer == null || patienceSprites == null || patienceSprites.Length == 0) return;

        float ratio = currentWaitTime / maxWaitTime;
        int index = 0;

        if (ratio < 0.25f) index = 0;
        else if (ratio < 0.5f) index = 1;
        else if (ratio < 0.75f) index = 2;
        else index = 3;

        index = Mathf.Clamp(index, 0, patienceSprites.Length - 1);
        patienceRenderer.sprite = patienceSprites[index];
    }

    private void CustomerFailed()
    {
        isOrderCompleted = true; // Zile basılmasını engelle
        
        // 5$ ceza
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveMoney(5f);
        }

        // Tabağı temizle
        if (PlateManager.Instance != null)
        {
            PlateManager.Instance.ClearPlate();
        }

        // Müşteri %0 doğrulukla (en mutsuz) gider
        Leave(0f);
    }

    // -------------------------------------------------------

    /// <summary>
    /// Her parça için rastgele bir sprite seçer ve ilgili SpriteRenderer'a atar.
    /// Tekrar çağrıldığında aynı transform'lardaki sprite değişir, konum değişmez.
    /// </summary>
    [ContextMenu("Generate Random Customer")]
    public void GenerateRandomCustomer()
    {
        StopAllCoroutines();
        isLeaving = false;
        
        if (startPos != null) transform.position = startPos.position;
        SetCharacterColor(new Color(0, 0, 0, 0)); // Siyah ve şeffaf başla
        
        if (emojiRenderer != null) emojiRenderer.gameObject.SetActive(false);
        if (patienceRenderer != null) patienceRenderer.gameObject.SetActive(true);

        LoadSpritesIfNeeded();

        if (_allSprites == null || _allSprites.Length == 0)
        {
            Debug.LogError("[CustomerManager] Sprite'lar yüklenemedi! " +
                           "'spriteSheet' alanına FlippinBird-Sheet'i atadığınızdan emin olun.");
            return;
        }

        AssignSprite(hairRenderer,  HairNames,  ref _prevHair);
        AssignSprite(faceRenderer,  FaceNames,  ref _prevFace);
        AssignSprite(eyeRenderer,   EyeNames,   ref _prevEye);
        AssignSprite(noseRenderer,  NoseNames,  ref _prevNose);
        AssignSprite(mouthRenderer, MouthNames, ref _prevMouth);
        AssignSprite(clothRenderer, ClothNames, ref _prevCloth);

        StartCoroutine(EnterRoutine());
    }

    private System.Collections.IEnumerator EnterRoutine()
    {
        float duration = 0.4f; // 0.4 saniyede hızlıca belirsin
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            SetCharacterColor(Color.Lerp(new Color(0, 0, 0, 0), Color.white, t));
            yield return null;
        }
        
        SetCharacterColor(Color.white);
    }

    // -------------------------------------------------------
    // Yardımcı metodlar
    // -------------------------------------------------------

    /// <summary>
    /// Verilen isim listesinden rastgele birini seçip renderer'a atar.
    /// Havuzda birden fazla seçenek varsa bir öncekiyle aynı sprite seçilmez.
    /// </summary>
    private void AssignSprite(SpriteRenderer renderer, string[] namePool, ref string prevName)
    {
        if (renderer == null) return;

        // Havuzda birden fazla seçenek varsa öncekini dışla
        string chosenName;
        if (namePool.Length > 1)
        {
            do { chosenName = namePool[Random.Range(0, namePool.Length)]; }
            while (chosenName == prevName);
        }
        else
        {
            chosenName = namePool[0];
        }

        Sprite found = FindSprite(chosenName);
        if (found != null)
        {
            renderer.sprite = found;
            prevName = chosenName;   // Seçimi kaydet
        }
        else
        {
            Debug.LogWarning($"[CustomerManager] '{chosenName}' sprite'ı bulunamadı.");
        }
    }

    public void Leave(float accuracyPercent)
    {
        Debug.Log($"[CustomerManager] Müşteri ayrılıyor. Başarı: %{accuracyPercent:F1}");
        
        // Ayrılırken sipariş baloncuğunu temizle
        foreach (var part in _activeBubbleParts)
        {
            if (part != null) Destroy(part);
        }
        _activeBubbleParts.Clear();

        isWaiting = false;
        if (patienceRenderer != null) patienceRenderer.gameObject.SetActive(false);

        // Emoji gösterimi
        if (emojiRenderer != null && emojiSprites != null && emojiSprites.Length >= 5)
        {
            int emojiIndex = 0;
            if (accuracyPercent <= 20f) emojiIndex = 4;
            else if (accuracyPercent <= 40f) emojiIndex = 3;
            else if (accuracyPercent <= 60f) emojiIndex = 2;
            else if (accuracyPercent <= 80f) emojiIndex = 1;
            else emojiIndex = 0; // 81 - 100

            emojiRenderer.sprite = emojiSprites[emojiIndex];
            emojiRenderer.gameObject.SetActive(true);
        }

        StartCoroutine(LeaveRoutine());
    }

    private System.Collections.IEnumerator LeaveRoutine()
    {
        isLeaving = true;
        float elapsed = 0f;
        
        Vector3 startLoc = transform.position;
        Vector3 endLoc = exitPos != null ? exitPos.position : startLoc + new Vector3(5f, 0f, 0f); // Default sağa
        
        while (elapsed < leaveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / leaveDuration;
            
            // Konumu ilerlet
            transform.position = Vector3.Lerp(startLoc, endLoc, t);
            
            // Renkleri siyaha (rgb=0) ve şeffafa (a=0) doğru kaydır
            Color currentColor = Color.Lerp(Color.white, new Color(0, 0, 0, 0), t);
            SetCharacterColor(currentColor);
            
            yield return null;
        }
        
        transform.position = endLoc;
        SetCharacterColor(new Color(0, 0, 0, 0));
        isLeaving = false;

        // Karakter tamamen yok olunca emojiyi de gizle
        if (emojiRenderer != null) emojiRenderer.gameObject.SetActive(false);

        // 2 saniye bekle ve yeni müşteri getir
        yield return new WaitForSeconds(2f);
        SpawnNewCustomer();
    }

    private void SetCharacterColor(Color c)
    {
        if (hairRenderer != null) hairRenderer.color = c;
        if (faceRenderer != null) faceRenderer.color = c;
        if (eyeRenderer != null) eyeRenderer.color = c;
        if (noseRenderer != null) noseRenderer.color = c;
        if (mouthRenderer != null) mouthRenderer.color = c;
        if (clothRenderer != null) clothRenderer.color = c;
        // Emojinin rengi etkilenmesin diye buradan kaldırıldı
    }

    /// <summary>
    /// Cache'lenmiş dizide isimle sprite arar.
    /// </summary>
    private Sprite FindSprite(string spriteName)
    {
        if (_allSprites == null) return null;

        foreach (Sprite s in _allSprites)
            if (s != null && s.name == spriteName)
                return s;

        return null;
    }

    /// <summary>
    /// Sprite'ları ilk çağrıda Texture2D'den yükler ve önbelleğe alır.
    /// </summary>
    private void LoadSpritesIfNeeded()
    {
        if (_allSprites != null && _allSprites.Length > 0) return;

#if UNITY_EDITOR
        // Editor modunda AssetDatabase kullanarak sub-sprite'ları al
        string path = UnityEditor.AssetDatabase.GetAssetPath(spriteSheet);
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);

        var list = new System.Collections.Generic.List<Sprite>();
        foreach (Object obj in assets)
            if (obj is Sprite sp)
                list.Add(sp);

        _allSprites = list.ToArray();
#else
        // Build: Resources klasöründe ise Resources.LoadAll kullanabilirsiniz.
        // Şimdilik inspector'dan atanan texture yeterlidir; build için
        // SpriteAtlas veya Addressables kullanmanızı öneririz.
        Debug.LogWarning("[CustomerManager] Build modunda sprite yükleme için " +
                         "SpriteAtlas veya Addressables kullanın.");
#endif
    }

    // -------------------------------------------------------
    // Sipariş (Order) Sistemi
    // -------------------------------------------------------
    [Header("Order System")]

    public Transform orderBubbleAnchor;
    public GameObject bubbleStartPrefab;
    public GameObject bubbleMiddlePrefab;
    public GameObject bubbleEndPrefab;
    [Header("Bubble Offsets")]
    [Tooltip("Başlangıç parçasından ilk orta parçaya olan uzaklık")]
    public float offsetStartToMiddle = 0.8f;
    [Tooltip("Orta parçaların birbirine olan uzaklığı")]
    public float offsetMiddleToMiddle = 1.4f;
    [Tooltip("Son orta parçadan bitiş parçasına olan uzaklık")]
    public float offsetMiddleToFinish = 0.4f;
    
    [System.Serializable]
    public class OrderItem
    {
        public IngredientType ingredient;
        public int count;
    }

    [System.Serializable]
    public struct OrderIngredientData
    {
        public IngredientType type;
        public Sprite icon;
    }
    public OrderIngredientData[] orderIngredientIcons;

    // Her siparişi sonradan kontrol edebilmek için tuttuğumuz liste
    [Header("Current Active Order")]
    public System.Collections.Generic.List<OrderItem> currentActiveOrder = new System.Collections.Generic.List<OrderItem>();

    // Aktif baloncuk parçalarını tutmak için
    private System.Collections.Generic.List<GameObject> _activeBubbleParts = new System.Collections.Generic.List<GameObject>();

    /// <summary>
    /// Gün sayısına göre müşterinin siparişini oluşturur ve konuşma baloncuğunu çizer.
    /// </summary>
    public void GenerateOrder(int dayNumber)
    {
        // Önceki baloncuğu temizle
        foreach (var part in _activeBubbleParts)
        {
            if (part != null) Destroy(part);
        }
        _activeBubbleParts.Clear();
        currentActiveOrder.Clear();
        isOrderCompleted = false;

        // 1. Kural: Kesinlikle burger köftesi olmalı (Maksimum 5)
        int pattyCount = Random.Range(1, Mathf.Clamp(1 + dayNumber / 2, 2, 6)); // max 5
        currentActiveOrder.Add(new OrderItem { ingredient = IngredientType.CookedPatty, count = pattyCount });

        // Tüm malzemeler 1. günden itibaren açık
        System.Collections.Generic.List<IngredientType> availableExtras = new System.Collections.Generic.List<IngredientType>
        {
            IngredientType.Cheese,
            IngredientType.Lettuce,
            IngredientType.Tomato
        };

        // Çeşitleri karıştır
        for (int i = 0; i < availableExtras.Count; i++)
        {
            int r = Random.Range(i, availableExtras.Count);
            var temp = availableExtras[i];
            availableExtras[i] = availableExtras[r];
            availableExtras[r] = temp;
        }

        // Kaç ekstra malzeme ekleneceğini belirle (0 ile mevcut ekstra çeşidi arası)
        int numExtras = Random.Range(0, availableExtras.Count + 1);

        for (int i = 0; i < numExtras; i++)
        {
            IngredientType extra = availableExtras[i];
            // 2. Kural: Her malzeme için maksimum 5
            int maxCount = 5;

            // 3. Kural: Domates ve marul köfte sayısından fazla olmamalı (aynı olabilir)
            if (extra == IngredientType.Lettuce || extra == IngredientType.Tomato)
            {
                maxCount = pattyCount;
            }

            // Güne göre zorluk artsın ama kural dışına çıkmasın
            int maxForThisDay = Mathf.Clamp(1 + (dayNumber / 2), 1, maxCount);
            int extraCount = Random.Range(1, maxForThisDay + 1);
            
            currentActiveOrder.Add(new OrderItem { ingredient = extra, count = extraCount });
        }

        // 4. Baloncuğu görsel olarak oluştur
        if (orderBubbleAnchor == null) 
        {
            Debug.LogWarning("[CustomerManager] orderBubbleAnchor atanmamış!");
            return;
        }

        float currentX = 0f;

        // Başlangıç parçası
        if (bubbleStartPrefab != null)
        {
            GameObject startPart = Instantiate(bubbleStartPrefab, orderBubbleAnchor);
            startPart.transform.localPosition = new Vector3(currentX, 0, 0);
            _activeBubbleParts.Add(startPart);
            currentX += offsetStartToMiddle;
        }

        // Orta parçalar (Her malzeme çeşidi için 1 tane)
        for (int i = 0; i < currentActiveOrder.Count; i++)
        {
            if (bubbleMiddlePrefab != null)
            {
                GameObject middlePart = Instantiate(bubbleMiddlePrefab, orderBubbleAnchor);
                middlePart.transform.localPosition = new Vector3(currentX, 0, 0);
                _activeBubbleParts.Add(middlePart);

                // Malzeme ikonunu ve sayısını ayarla
                SpriteRenderer iconRenderer = middlePart.transform.Find("Icon")?.GetComponent<SpriteRenderer>();
                TMPro.TextMeshPro countText = middlePart.transform.Find("CountText")?.GetComponent<TMPro.TextMeshPro>();

                if (iconRenderer != null)
                {
                    iconRenderer.sprite = GetIngredientIcon(currentActiveOrder[i].ingredient);
                }
                if (countText != null)
                {
                    countText.text = "x" + currentActiveOrder[i].count.ToString();
                }

                // Sonraki parça için offset'i ayarla
                if (i == currentActiveOrder.Count - 1)
                {
                    // Son orta parçadaysak bitiş parçasına olan mesafeyi ekle
                    currentX += offsetMiddleToFinish;
                }
                else
                {
                    // Başka orta parça gelecekse orta parçalar arası mesafeyi ekle
                    currentX += offsetMiddleToMiddle;
                }
            }
        }

        // Bitiş parçası
        if (bubbleEndPrefab != null)
        {
            GameObject endPart = Instantiate(bubbleEndPrefab, orderBubbleAnchor);
            endPart.transform.localPosition = new Vector3(currentX, 0, 0);
            _activeBubbleParts.Add(endPart);
        }
    }

    private Sprite GetIngredientIcon(IngredientType type)
    {
        foreach (var item in orderIngredientIcons)
        {
            if (item.type == type) return item.icon;
        }
        return null;
    }
}
