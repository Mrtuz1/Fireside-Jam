using UnityEngine;

public class ContainerInstance : MonoBehaviour
{
    public IngredientType containerType;
    
    [Tooltip("Color to change to when hovered")]
    public Color hoverColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    
    private SpriteRenderer[] allRenderers;
    private Color[] originalColors;

    private void Awake()
    {
        // Kendisi ve altındaki tüm SpriteRenderer'ları bul
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[allRenderers.Length];
        
        for (int i = 0; i < allRenderers.Length; i++)
        {
            originalColors[i] = allRenderers[i].color;
        }
    }

    private void SetHoverColor()
    {
        if (allRenderers == null) return;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                allRenderers[i].color = hoverColor;
            }
        }
    }

    private void RevertColor()
    {
        if (allRenderers == null) return;
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                allRenderers[i].color = originalColors[i];
            }
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
            SetHoverColor();
        }
    }

    private void OnMouseExit()
    {
        RevertColor();
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
                SetHoverColor();
            }
        }
    }
}
