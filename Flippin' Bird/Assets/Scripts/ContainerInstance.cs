using UnityEngine;

public class ContainerInstance : MonoBehaviour
{
    public IngredientType containerType;
    
    [Tooltip("Color to change to when hovered")]
    public Color hoverColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void OnMouseEnter()
    {
        if (PlayerHand.Instance == null) return;

        // Dim color if mouse is empty OR if holding an ingredient of the same type
        if (PlayerHand.Instance.heldIngredient == null || PlayerHand.Instance.heldIngredient.type == containerType)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }
        }
    }

    private void OnMouseExit()
    {
        // Revert to original color when mouse exits
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseDown()
    {
        if (PlayerHand.Instance == null) return;

        if (PlayerHand.Instance.heldIngredient == null)
        {
            // Mouse is empty: create an ingredient and hold it
            if (PlayerHand.Instance.ingredientPrefab != null)
            {
                IngredientInstance newIngredient = Instantiate(PlayerHand.Instance.ingredientPrefab);
                newIngredient.Initialize(containerType);
                newIngredient.isHolding = true;
                
                PlayerHand.Instance.heldIngredient = newIngredient;
            }
            else
            {
                Debug.LogError("IngredientPrefab is not assigned in PlayerHand script!");
            }
        }
        else if (PlayerHand.Instance.heldIngredient.type == containerType)
        {
            // Mouse is holding an ingredient of the same type: destroy the held ingredient
            Destroy(PlayerHand.Instance.heldIngredient.gameObject);
            PlayerHand.Instance.heldIngredient = null;
            
            // Ensure hover color stays since mouse is still hovering and now empty
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }
        }
        // If holding a different type of ingredient, do nothing
    }
}
