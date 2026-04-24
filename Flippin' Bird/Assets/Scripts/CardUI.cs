using UnityEngine;
using UnityEngine.UI; // UI elementlerine erişmek için bu şart!

public class CardUI : MonoBehaviour
{
    private Image cardImage;

    void Awake()
    {
        // Objenin üzerindeki Image bileşenini bul ve hafızaya al
        cardImage = GetComponent<Image>();
    }

    // Bu fonksiyonu Manager çağıracak ve içine "Hangi kart?" bilgisini atacak
    public void SetCard(CardData data)
    {
        // ScriptableObject içindeki görseli, ekrandaki Image'a aktar
        cardImage.sprite = data.cardSprite;
    }
}