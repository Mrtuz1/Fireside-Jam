using UnityEngine;

public class IngredientInstance : MonoBehaviour
{
    public IngredientType type;
    public bool isHolding = false;
    
    [Range(1, 8)]
    public int cookingStatus = 1;

    [System.Serializable]
    public struct IngredientSprite
    {
        public IngredientType type;
        public Sprite sprite;
    }

    [Tooltip("Assign sprites for each ingredient type in the inspector")]
    public IngredientSprite[] sprites;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (isHolding)
        {
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = 0f; // Ensure it stays on the 2D plane
            transform.position = mousePosition;
        }
    }

    // Constructor-like initialization
    public void Initialize(IngredientType newType)
    {
        type = newType;
        
        if (type == IngredientType.RawPatty)
        {
            cookingStatus = 1;
        }
        
        UpdateSprite();
    }

    // Updates the SpriteRenderer based on current type
    public void UpdateSprite()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        foreach (var item in sprites)
        {
            if (item.type == type)
            {
                spriteRenderer.sprite = item.sprite;
                return;
            }
        }
        
        Debug.LogWarning($"Sprite for IngredientType {type} is not assigned in the inspector!");
    }
}
