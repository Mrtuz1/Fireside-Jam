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

    private void Start()
    {
        GenerateRandomCustomer();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            GenerateRandomCustomer();
    }

    // -------------------------------------------------------

    /// <summary>
    /// Her parça için rastgele bir sprite seçer ve ilgili SpriteRenderer'a atar.
    /// Tekrar çağrıldığında aynı transform'lardaki sprite değişir, konum değişmez.
    /// </summary>
    [ContextMenu("Generate Random Customer")]
    public void GenerateRandomCustomer()
    {
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
}
