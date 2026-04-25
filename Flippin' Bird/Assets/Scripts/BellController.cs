using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class BellController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    public float blinkSpeed = 5f;
    
    private bool isBlinking = false;
    private bool canBePressed = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (PlateManager.Instance == null || CustomerManager.Instance == null) return;

        // Sipariş henüz tamamlanmamışsa kontrol et
        if (!CustomerManager.Instance.isOrderCompleted)
        {
            var stack = PlateManager.Instance.GetIngredientStack();
            
            // Eğer tabakta en az 1 malzeme varsa ve en üstteki TopBun ise
            if (stack.Count > 0 && stack.Last().type == IngredientType.TopBun)
            {
                canBePressed = true;
                isBlinking = true;
            }
            else
            {
                canBePressed = false;
                isBlinking = false;
                SetAlpha(1f); // Yanıp sönmeyi durdur ve görünür yap
            }
        }
        else
        {
            // Sipariş tamamlandıktan sonra zil etkileşimsiz kalır
            canBePressed = false;
            isBlinking = false;
            SetAlpha(1f);
        }

        // Yanıp sönme efekti
        if (isBlinking)
        {
            float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f; // 0 ile 1 arası gidip gelir
            SetAlpha(alpha);
        }
    }

    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }

    private void OnMouseDown()
    {
        if (GameManager.Instance != null && !GameManager.Instance.isDayActive) return;
        if (canBePressed)
        {
            // Siparişi tamamla
            CustomerManager.Instance.isOrderCompleted = true;
            isBlinking = false;
            SetAlpha(1f);

            CalculateOrderAccuracy();
            
            // Tabaktaki burgeri sil
            if (PlateManager.Instance != null)
            {
                PlateManager.Instance.ClearPlate();
            }
        }
    }

    private void CalculateOrderAccuracy()
    {
        var stack = PlateManager.Instance.GetIngredientStack();
        var requestedOrder = CustomerManager.Instance.currentActiveOrder;

        // Tabaktaki (ekmekler hariç) malzemeleri say
        Dictionary<IngredientType, int> providedCounts = new Dictionary<IngredientType, int>();
        foreach (var item in stack)
        {
            if (item.type != IngredientType.TopBun && item.type != IngredientType.BottomBun)
            {
                if (providedCounts.ContainsKey(item.type))
                    providedCounts[item.type]++;
                else
                    providedCounts[item.type] = 1;
            }
        }

        // Doğruluk oranını hesapla
        int totalRequested = 0;
        int totalCorrect = 0;

        foreach (var requestedItem in requestedOrder)
        {
            totalRequested += requestedItem.count;

            int provided = 0;
            if (providedCounts.TryGetValue(requestedItem.ingredient, out provided))
            {
                // Fazladan konulan malzeme ekstra doğru sayılmaz, sadece istenen kadarını doğru kabul et
                totalCorrect += Mathf.Min(provided, requestedItem.count);
            }
        }

        // Çiğ veya yanık köfte cezası
        int penalty = 0;
        foreach (var item in stack)
        {
            if (item.type == IngredientType.RawPatty || item.type == IngredientType.BurntPatty)
            {
                penalty++;
            }
        }
        totalCorrect = Mathf.Max(0, totalCorrect - penalty);

        float accuracyPercent = 0f;
        if (totalRequested > 0)
        {
            accuracyPercent = ((float)totalCorrect / totalRequested) * 100f;
        }

        // --- EKONOMİ HESABI ---
        float totalCostOfRequested = 0f;
        // Müşterinin TAM OLARAK istediği siparişin masrafı (Ekmekler dahil)
        foreach (var req in requestedOrder)
        {
            totalCostOfRequested += GameManager.Instance.GetIngredientCost(req.ingredient) * req.count;
        }
        totalCostOfRequested += GameManager.Instance.GetIngredientCost(IngredientType.BottomBun);
        totalCostOfRequested += GameManager.Instance.GetIngredientCost(IngredientType.TopBun);

        // Kâr marjı: GameManager'dan alınır
        float multiplier = GameManager.Instance != null ? GameManager.Instance.profitMultiplier : 2f;
        float baseSellPrice = totalCostOfRequested * multiplier;
        
        // Kazanç, doğruluk oranıyla çarpılır ve en yakın 10 kuruşa yuvarlanır (örn: 5.54 -> 5.50, 3.67 -> 3.70)
        float rawEarnings = baseSellPrice * (accuracyPercent / 100f);
        float finalEarnings = Mathf.Round(rawEarnings * 10f) / 10f;

        if (finalEarnings > 0f)
        {
            GameManager.Instance.AddMoney(finalEarnings);
        }

        // İstatistikleri güncelle
        if (GameManager.Instance != null)
        {
            GameManager.Instance.totalCustomersServed++;
        }

        // Log çıkart
        Debug.Log("==== SİPARİŞ TAMAMLANDI ====");
        Debug.Log("İstenenler:");
        foreach (var item in requestedOrder)
        {
            Debug.Log($"- {item.ingredient}: {item.count} adet");
        }

        Debug.Log("Verilenler (Ekmekler Hariç):");
        foreach (var kvp in providedCounts)
        {
            Debug.Log($"- {kvp.Key}: {kvp.Value} adet");
        }

        Debug.Log($"DOĞRULUK ORANI: %{accuracyPercent:F1} ({totalCorrect} / {totalRequested})");
        Debug.Log($"[Hesap] Toplam Maliyet (Ekmekler Dahil): {totalCostOfRequested:F2}$ -> Hedef Satış: {baseSellPrice:F2}$");
        Debug.Log($"KAZANÇ: +{finalEarnings:F2}$");
        Debug.Log("============================");

        // Müşteriye gitmesini ve animasyonu başlatmasını söyle
        CustomerManager.Instance.Leave(accuracyPercent);
    }
}
