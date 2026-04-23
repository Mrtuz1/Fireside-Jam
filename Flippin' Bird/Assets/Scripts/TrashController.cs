using UnityEngine;

public class TrashController : MonoBehaviour
{
    [Tooltip("Color to change to when hovered (optional)")]
    public Color hoverColor = new Color(0.8f, 0.5f, 0.5f, 1f);
    
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
        if (PlayerHand.Instance != null && PlayerHand.Instance.heldIngredient != null)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hoverColor;
            }
        }
    }

    private void OnMouseExit()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isDayActive) return;
        if (PlayerHand.Instance != null && PlayerHand.Instance.heldIngredient != null)
        {
            // Elimizdekini yok et
            Destroy(PlayerHand.Instance.heldIngredient.gameObject);
            PlayerHand.Instance.heldIngredient = null;
            
            // Mouse üzerindeyken renk eski haline dönsün
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }
}
