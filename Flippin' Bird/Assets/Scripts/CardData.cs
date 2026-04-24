using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Blackjack/Card")]
public class CardData : ScriptableObject
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public Suit cardSuit;
    public string cardName;
    public int value; // 2-11 arası
    public Sprite cardSprite; // Kartın görseli
}