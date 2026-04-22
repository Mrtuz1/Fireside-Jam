using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    public static PlayerHand Instance { get; private set; }

    [Tooltip("The prefab that has the IngredientInstance script attached")]
    public IngredientInstance ingredientPrefab;
    
    [SerializeField, Tooltip("Currently held ingredient by the mouse")]
    private IngredientInstance _heldIngredient;
    
    public IngredientInstance heldIngredient
    {
        get => _heldIngredient;
        set
        {
            _heldIngredient = value;
            
            // Eğer elimize yeni bir malzeme aldıysak, ekrandaki her şeyin üstünde görünsün
            if (_heldIngredient != null)
            {
                SpriteRenderer sr = _heldIngredient.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 100;
                }
            }
            
            OnHeldIngredientChanged?.Invoke();
        }
    }

    public event System.Action OnHeldIngredientChanged;

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
    }
}
