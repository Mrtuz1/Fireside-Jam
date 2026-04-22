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

    private bool IsBunType(IngredientType t)
    {
        return t == IngredientType.BottomBun || t == IngredientType.TopBun;
    }

    private void OnMouseEnter()
    {
        if (PlayerHand.Instance == null) return;

        bool isMatch = false;
        if (PlayerHand.Instance.heldIngredient != null)
        {
            IngredientType handType = PlayerHand.Instance.heldIngredient.type;
            if (handType == containerType) isMatch = true;
            else if (IsBunType(containerType) && IsBunType(handType)) isMatch = true;
        }

        // Dim color if mouse is empty OR if holding an ingredient of the same type (or any bun if this is a bun container)
        if (PlayerHand.Instance.heldIngredient == null || isMatch)
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
                IngredientType typeToSpawn = containerType;
                
                // Dynamic bun logic
                if (IsBunType(containerType) && PlateManager.Instance != null)
                {
                    if (PlateManager.Instance.IsStackEmpty())
                    {
                        typeToSpawn = IngredientType.BottomBun;
                    }
                    else
                    {
                        typeToSpawn = IngredientType.TopBun;
                    }
                }

                IngredientInstance newIngredient = Instantiate(PlayerHand.Instance.ingredientPrefab);
                newIngredient.Initialize(typeToSpawn);
                newIngredient.isHolding = true;
                
                PlayerHand.Instance.heldIngredient = newIngredient;
            }
            else
            {
                Debug.LogError("IngredientPrefab is not assigned in PlayerHand script!");
            }
        }
        else 
        {
            // Check if holding an ingredient that matches this container
            bool isMatch = false;
            IngredientType handType = PlayerHand.Instance.heldIngredient.type;
            
            if (handType == containerType) isMatch = true;
            else if (IsBunType(containerType) && IsBunType(handType)) isMatch = true;

            if (isMatch)
            {
                // Mouse is holding a matching ingredient: destroy the held ingredient
                Destroy(PlayerHand.Instance.heldIngredient.gameObject);
                PlayerHand.Instance.heldIngredient = null;
                
                // Ensure hover color stays since mouse is still hovering and now empty
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = hoverColor;
                }
            }
        }
    }
}
