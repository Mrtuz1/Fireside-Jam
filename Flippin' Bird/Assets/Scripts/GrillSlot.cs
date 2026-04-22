using UnityEngine;

public class GrillSlot : MonoBehaviour
{
    [Tooltip("The patty currently on this grill slot")]
    public IngredientInstance onGrillObject;

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer slotSpriteRenderer;
    [SerializeField] private SpriteRenderer progressSpriteRenderer;
    
    [Header("Progress Sprites")]
    [Tooltip("Size should be 8. Indexes 0-3 for cook 1-4, Indexes 4-7 for burn 1-4.")]
    [SerializeField] private Sprite[] progressSprites;

    [Header("Settings")]
    public float timePerCookingStage = 2f; // Time in seconds before cookingStatus increments
    public float blinkSpeed = 0.5f; // Controls how fast the slot blinks
    private float cookTimer = 0f;

    private Color originalColor;
    private bool isBlinking = false;

    private void Awake()
    {
        if (slotSpriteRenderer == null) slotSpriteRenderer = GetComponent<SpriteRenderer>();
        if (slotSpriteRenderer != null) 
        {
            originalColor = slotSpriteRenderer.color;
            slotSpriteRenderer.enabled = false; // Hide by default
        }

        if (progressSpriteRenderer != null)
        {
            progressSpriteRenderer.sprite = null;
            progressSpriteRenderer.gameObject.SetActive(false); // Hidden by default
        }
    }

    private bool shouldBlink = false;

    private void Update()
    {
        HandleCooking();
        HandleBlinking();
    }

    public void UpdateBlinkState(bool holdingPatty)
    {
        shouldBlink = holdingPatty && onGrillObject == null;
        
        if (shouldBlink)
        {
            if (slotSpriteRenderer != null)
            {
                slotSpriteRenderer.enabled = true; // Show when blinking
            }
        }
        else if (isBlinking)
        {
            // Stop blinking and reset/hide
            if (slotSpriteRenderer != null)
            {
                slotSpriteRenderer.color = originalColor;
                slotSpriteRenderer.enabled = false;
            }
            isBlinking = false;
        }
    }

    private void HandleBlinking()
    {
        if (shouldBlink)
        {
            if (slotSpriteRenderer != null)
            {
                Color blinkColor = originalColor;
                // Alpha pulses between 0.0 and 0.4 based on blinkSpeed
                blinkColor.a = Mathf.PingPong(Time.time * blinkSpeed, 0.4f); 
                slotSpriteRenderer.color = blinkColor;
            }
            isBlinking = true;
        }
    }

    private void HandleCooking()
    {
        if (onGrillObject != null)
        {
            // Only cook if status is less than 9 (9 = completely burnt)
            if (onGrillObject.cookingStatus < 9)
            {
                cookTimer += Time.deltaTime;
                if (cookTimer >= timePerCookingStage)
                {
                    cookTimer = 0f;
                    onGrillObject.cookingStatus++;

                    // Type transitions based on status
                    if (onGrillObject.cookingStatus == 5)
                    {
                        onGrillObject.type = IngredientType.CookedPatty;
                        onGrillObject.UpdateSprite(); // Update main patty visual
                    }
                    else if (onGrillObject.cookingStatus == 9)
                    {
                        onGrillObject.type = IngredientType.BurntPatty;
                        onGrillObject.UpdateSprite(); // Update main patty visual
                    }

                    UpdateProgressVisuals();
                }
            }
        }
        else
        {
            cookTimer = 0f;
        }
    }

    private void UpdateProgressVisuals()
    {
        if (progressSpriteRenderer != null)
        {
            // Status is between 1 and 8
            if (onGrillObject != null && onGrillObject.cookingStatus <= 8 && progressSprites != null && progressSprites.Length >= 8)
            {
                progressSpriteRenderer.gameObject.SetActive(true);
                int spriteIndex = onGrillObject.cookingStatus - 1; // 1 -> 0, 8 -> 7
                progressSpriteRenderer.sprite = progressSprites[spriteIndex];
            }
            else
            {
                // If it reached 9 (burnt) or is empty, hide the progress bar
                progressSpriteRenderer.sprite = null;
                progressSpriteRenderer.gameObject.SetActive(false);
            }
        }
    }

    private void OnMouseDown()
    {
        if (PlayerHand.Instance == null) return;

        bool holdingPatty = false;
        if (PlayerHand.Instance.heldIngredient != null)
        {
            IngredientType type = PlayerHand.Instance.heldIngredient.type;
            if (type == IngredientType.RawPatty || type == IngredientType.CookedPatty || type == IngredientType.BurntPatty)
            {
                holdingPatty = true;
            }
        }

        // Dropping Patty on the Grill
        if (holdingPatty && onGrillObject == null)
        {
            IngredientInstance patty = PlayerHand.Instance.heldIngredient;
            
            onGrillObject = patty; // Assign to this slot
            
            // Remove from hand
            PlayerHand.Instance.heldIngredient = null;
            patty.isHolding = false;
            
            // Set transform to slot's transform and make it a child
            patty.transform.position = transform.position;
            patty.transform.SetParent(transform);
            
            // Update visual state and reset timer
            cookTimer = 0f;
            UpdateProgressVisuals();
        }
        // Picking Patty up from the Grill
        else if (PlayerHand.Instance.heldIngredient == null && onGrillObject != null)
        {
            IngredientInstance patty = onGrillObject;
            
            onGrillObject = null; // Remove from slot
            
            // Add to hand
            PlayerHand.Instance.heldIngredient = patty;
            patty.isHolding = true;
            patty.transform.SetParent(null); // Remove from child
            
            // Hide progress UI
            if (progressSpriteRenderer != null)
            {
                progressSpriteRenderer.sprite = null;
                progressSpriteRenderer.gameObject.SetActive(false);
            }
        }
    }
}
