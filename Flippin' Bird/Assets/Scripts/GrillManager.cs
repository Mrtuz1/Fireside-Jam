using UnityEngine;
using System.Collections.Generic;

public class GrillManager : MonoBehaviour
{
    public static GrillManager Instance { get; private set; }

    [Tooltip("List of Grill Slots managed by this manager")]
    [SerializeField] private List<GrillSlot> grillSlots = new List<GrillSlot>();

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

        // Auto-populate if empty
        if (grillSlots == null || grillSlots.Count == 0)
        {
            grillSlots = new List<GrillSlot>(GetComponentsInChildren<GrillSlot>());
            Debug.Log("GrillManager: Auto-populated " + grillSlots.Count + " slots.");
        }
    }

    private void Start()
    {
        if (PlayerHand.Instance != null)
        {
            PlayerHand.Instance.OnHeldIngredientChanged += CheckHeldIngredient;
            // Initialize once
            CheckHeldIngredient();
        }
    }

    private void OnDestroy()
    {
        if (PlayerHand.Instance != null)
        {
            PlayerHand.Instance.OnHeldIngredientChanged -= CheckHeldIngredient;
        }
    }

    private void CheckHeldIngredient()
    {
        bool holdingPatty = false;

        if (PlayerHand.Instance != null && PlayerHand.Instance.heldIngredient != null)
        {
            IngredientType type = PlayerHand.Instance.heldIngredient.type;
            if (type == IngredientType.RawPatty || type == IngredientType.CookedPatty || type == IngredientType.BurntPatty)
            {
                holdingPatty = true;
            }
        }

        // Notify slots about the state
        foreach (var slot in grillSlots)
        {
            if (slot != null)
            {
                slot.UpdateBlinkState(holdingPatty);
            }
        }
    }
}
