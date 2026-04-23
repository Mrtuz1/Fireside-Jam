using UnityEngine;
using System.Collections.Generic;

public class PlateManager : MonoBehaviour
{
    public static PlateManager Instance { get; private set; }

    [Header("Stack Visuals")]
    [Tooltip("The transform representing the base of the burger stack")]
    public Transform stackStartTransform;
    [Tooltip("Y offset for each ingredient placed on top")]
    public float yOffset = 0.3f;
    [Tooltip("How much the collider grows in height per ingredient")]
    public float colliderGrowthPerItem = 0.5f;
    [Tooltip("Maximum number of ingredients on the plate")]
    public int maxStackSize = 30;

    private List<IngredientInstance> ingredientStack = new List<IngredientInstance>();
    
    private CapsuleCollider2D capsuleCol;
    private float originalColliderSizeY;
    private float originalColliderOffsetY;

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

        capsuleCol = GetComponent<CapsuleCollider2D>();
        if (capsuleCol != null)
        {
            originalColliderSizeY = capsuleCol.size.y;
            originalColliderOffsetY = capsuleCol.offset.y;
        }
    }

    private void UpdateCollider()
    {
        if (capsuleCol == null) return;
        
        float addedHeight = ingredientStack.Count > 0 ? (ingredientStack.Count * colliderGrowthPerItem) : 0f;
        
        capsuleCol.size = new Vector2(capsuleCol.size.x, originalColliderSizeY + addedHeight);
        capsuleCol.offset = new Vector2(capsuleCol.offset.x, originalColliderOffsetY + (addedHeight / 2f));
    }

    public bool IsStackEmpty()
    {
        return ingredientStack.Count == 0;
    }

    public List<IngredientInstance> GetIngredientStack()
    {
        return ingredientStack;
    }

    public void ClearPlate()
    {
        foreach (var ingredient in ingredientStack)
        {
            if (ingredient != null)
            {
                Destroy(ingredient.gameObject);
            }
        }
        ingredientStack.Clear();
        UpdateCollider();
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isDayActive) return;
        if (PlayerHand.Instance == null) return;

        IngredientInstance heldObj = PlayerHand.Instance.heldIngredient;

        // Ekranda tıklamayı algılaması için bu scriptin olduğu objede (veya child'ında) bir Collider2D olmalı.

        if (heldObj != null)
        {
            // Elimiz doluysa ve stack boşsa, sadece BottomBun konulabilir
            if (ingredientStack.Count == 0)
            {
                if (heldObj.type != IngredientType.BottomBun)
                {
                    Debug.Log("The first ingredient on the plate must be a Bottom Bun!");
                    return;
                }
            }
            else
            {
                // Eğer stack boş değilse, en üstteki malzemenin TopBun olup olmadığını kontrol et
                IngredientInstance topIngredient = ingredientStack[ingredientStack.Count - 1];
                if (topIngredient.type == IngredientType.TopBun)
                {
                    Debug.Log("The burger is closed with a Top Bun! No more ingredients can be added.");
                    return;
                }
                
                if (ingredientStack.Count >= maxStackSize && heldObj.type != IngredientType.TopBun)
                {
                    Debug.Log("Maximum stack size reached! Only a Top Bun can be added now.");
                    return;
                }
            }

            // Ingredient'ı tabağa ekle
            ingredientStack.Add(heldObj);

            // Elimizden çıkar
            PlayerHand.Instance.heldIngredient = null;
            heldObj.isHolding = false;

            // Pozisyonunu ayarla
            Vector3 newPos = stackStartTransform != null ? stackStartTransform.position : transform.position;
            newPos.y += (ingredientStack.Count - 1) * yOffset;
            
            heldObj.transform.position = newPos;
            if (stackStartTransform != null)
            {
                heldObj.transform.SetParent(stackStartTransform);
            }
            
            // Görsel olarak üst üste doğru binmesi için sorting layer'ı arttır
            SpriteRenderer sr = heldObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10 + ingredientStack.Count;
            }
            
            UpdateCollider();
        }
        else if (heldObj == null && ingredientStack.Count > 0)
        {
            // Elimiz boşsa, en üstteki malzemeyi al
            IngredientInstance topObj = ingredientStack[ingredientStack.Count - 1];
            ingredientStack.RemoveAt(ingredientStack.Count - 1);

            PlayerHand.Instance.heldIngredient = topObj;
            topObj.isHolding = true;
            topObj.transform.SetParent(null);
            
            // Mouse'a geçtiğinde en üstte görünmesi için order in layer'ı iyice yükseltebiliriz
            SpriteRenderer sr = topObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 50; 
            }
            
            UpdateCollider();
        }
    }
}
