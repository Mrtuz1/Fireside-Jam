using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BlackjackManager : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  UI References
    // ─────────────────────────────────────────
    [Header("UI References")]
    public GameObject cardPrefab;
    public Transform playerArea;
    public Transform dealerArea;

    public Button hitButton;
    public Button standButton;

    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI dealerScoreText;

    // Panel içinde RestartButton + SummaryText bulunur; oyun bitince açılır
    [SerializeField] private GameObject summaryPanel;
    public TextMeshProUGUI summaryText;   // summaryPanel içindeki text — Panel'in child'ı

    [Header("Bet System UI")]
    public GameObject computerPanel;
    public GameObject betPanel;
    public TextMeshProUGUI currentBetText;
    private float currentBet = 0f;

    [Header("Game Data")]
    public List<CardData> allCards;
    public Sprite cardBackSprite;

    // ─────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────
    private Stack<CardData> deck = new Stack<CardData>();

    // El listeleri: int değerler saklanır (As ilk çekildiğinde 11)
    private List<int> playerValues = new List<int>();
    private List<int> dealerValues = new List<int>();

    // Kart verisi (görsel için hâlâ lazım)
    private List<CardData> playerHand = new List<CardData>();
    private List<CardData> dealerHand = new List<CardData>();

    private GameObject dealerHiddenCardObj;
    private bool gameOver = false;

    // ─────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────
    void Start()
    {
        summaryPanel.SetActive(false);
        if (computerPanel != null) computerPanel.SetActive(false);
        if (betPanel != null) betPanel.SetActive(true);
        currentBet = 0;
        UpdateBetUI();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayEnded += HandleDayEnd;
            GameManager.Instance.OnDayChanged += HandleDayStart;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDayEnded -= HandleDayEnd;
            GameManager.Instance.OnDayChanged -= HandleDayStart;
        }
    }

    private void HandleDayEnd()
    {
        // Oyun devam ediyorsa (oyun ekranı açık ve bitmemişse) parayı iade et
        if (!gameOver && computerPanel != null && computerPanel.activeSelf)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMoney(currentBet);
            }
        }

        // Tüm panelleri kapat
        summaryPanel.SetActive(false);
        if (computerPanel != null) computerPanel.SetActive(false);
        if (betPanel != null) betPanel.SetActive(false);
    }

    private void HandleDayStart(int newDay)
    {
        // Yeni gün başladığında sistemi baştan başlat (bet ekranı açılsın vb.)
        Restart();
    }

    // ─────────────────────────────────────────
    //  Game Setup
    // ─────────────────────────────────────────
    public void PrepareGame()
    {
        summaryPanel.SetActive(false);

        // Eski kartları temizle
        foreach (Transform t in playerArea) Destroy(t.gameObject);
        foreach (Transform t in dealerArea)  Destroy(t.gameObject);

        playerValues.Clear();
        dealerValues.Clear();
        playerHand.Clear();
        dealerHand.Clear();
        dealerHiddenCardObj = null;
        gameOver = false;


        // Desteyi karıştır
        var shuffled = new List<CardData>(allCards);
        for (int i = 0; i < shuffled.Count; i++)
        {
            int rnd = Random.Range(0, shuffled.Count);
            var temp = shuffled[i];
            shuffled[i] = shuffled[rnd];
            shuffled[rnd] = temp;
        }
        deck = new Stack<CardData>(shuffled);

        StartCoroutine(InitialDeal());
    }

    // ─────────────────────────────────────────
    //  Initial Deal
    // ─────────────────────────────────────────
    IEnumerator InitialDeal()
    {
        SetPlayerButtons(false);

        // Oyuncuya 2 açık kart
        DrawForPlayer(); yield return new WaitForSeconds(0.5f);
        DrawForPlayer(); yield return new WaitForSeconds(0.5f);

        // Kasaya 1 açık kart
        DrawForDealer(false); yield return new WaitForSeconds(0.5f);

        // Kasaya 1 kapalı kart
        DrawForDealer(true);

        UpdateScoreUI();

        // İlk dağıtımda 21 kontrolü (natural blackjack)
        if (SumValues(playerValues) == 21)
        {
            yield return new WaitForSeconds(0.4f);
            EndGame(true);
            yield break;
        }

        SetPlayerButtons(true);
    }

    // ─────────────────────────────────────────
    //  Draw Helpers
    // ─────────────────────────────────────────
    void DrawForPlayer()
    {
        CardData card = deck.Pop();
        playerHand.Add(card);
        playerValues.Add(card.value); // As'ın value'su CardData'da 11 olmalı
        CreateCardUI(card, playerArea, false);
    }

    void DrawForDealer(bool isHidden)
    {
        CardData card = deck.Pop();
        dealerHand.Add(card);
        dealerValues.Add(card.value);
        GameObject obj = CreateCardUI(card, dealerArea, isHidden);
        if (isHidden) dealerHiddenCardObj = obj;
    }

    // ─────────────────────────────────────────
    //  HIT (Button callback)
    // ─────────────────────────────────────────
    public void Hit()
    {
        if (gameOver) return;
        StartCoroutine(HitRoutine());
    }

    IEnumerator HitRoutine()
    {
        SetPlayerButtons(false);

        DrawForPlayer();
        UpdateScoreUI();
        yield return new WaitForSeconds(0.3f);

        // Bust kontrol döngüsü
        yield return StartCoroutine(CheckPlayerBust());

        // Hâlâ oyundaysak butonları aç
        if (!gameOver) SetPlayerButtons(true);
    }

    // ─────────────────────────────────────────
    //  Bust Check (Oyuncu)
    // ─────────────────────────────────────────
    IEnumerator CheckPlayerBust()
    {
        int score = SumValues(playerValues);

        if (score == 21)
        {
            // Blackjack! Direkt kazan
            yield return new WaitForSeconds(0.4f);
            EndGame(true);
            yield break;
        }

        if (score > 21)
        {
            // 11'i 1'e çevirme döngüsü
            bool converted = false;
            for (int i = 0; i < playerValues.Count; i++)
            {
                if (playerValues[i] == 11)
                {
                    playerValues[i] = 1;
                    converted = true;
                    UpdateScoreUI();
                    yield return new WaitForSeconds(0.3f);

                    int newScore = SumValues(playerValues);

                    if (newScore == 21)
                    {
                        EndGame(true);
                        yield break;
                    }

                    if (newScore <= 21)
                    {
                        // Oyuna devam
                        yield break;
                    }

                    // Hâlâ > 21: döngü devam eder, bir sonraki 11'e bak
                }
            }

            if (!converted)
            {
                // 11 yoktu, kesinlikle bust
                EndGame(false);
            }
            else
            {
                // 11'leri tükettik ama hâlâ > 21
                int finalScore = SumValues(playerValues);
                if (finalScore > 21) EndGame(false);
            }
        }
    }

    // ─────────────────────────────────────────
    //  STAND (Button callback)
    // ─────────────────────────────────────────
    public void Stand()
    {
        if (gameOver) return;
        SetPlayerButtons(false);
        StartCoroutine(DealerTurn());
    }

    // ─────────────────────────────────────────
    //  Dealer Turn
    // ─────────────────────────────────────────
    IEnumerator DealerTurn()
    {
        // Kapalı kartı aç
        if (dealerHiddenCardObj != null)
            dealerHiddenCardObj.GetComponent<Image>().sprite = dealerHand[1].cardSprite;

        UpdateScoreUI(revealDealer: true);
        yield return new WaitForSeconds(1f);

        int playerScore = SumValues(playerValues);
        int dealerScore = GetDealerScore();

        // Kasa kart çekme: 17'nin altındayken veya oyuncudan küçükken çek
        while (dealerScore < 17 || dealerScore < playerScore)
        {
            DrawForDealer(false);
            dealerScore = GetDealerScore();
            UpdateScoreUI(revealDealer: true);
            yield return new WaitForSeconds(0.8f);
        }

        // Sonuç
        if (dealerScore > 21)
            EndGame(true);  // Kasa patladı → oyuncu kazandı
        else if (dealerScore > playerScore)
            EndGame(false); // Kasa daha yüksek
        else if (dealerScore == playerScore)
            EndGameDraw();
        else
            EndGame(true);  // Oyuncu daha yüksek
    }

    // ─────────────────────────────────────────
    //  Score Calculation
    // ─────────────────────────────────────────
    int SumValues(List<int> values)
    {
        int total = 0;
        foreach (int v in values) total += v;
        return total;
    }

    /// <summary>Kasanın el değerini Ace soft-hard değerlendirmesiyle hesaplar.</summary>
    int GetDealerScore()
    {
        int score = SumValues(dealerValues);
        // Kasa için de gerekirse 11→1 uygula
        List<int> temp = new List<int>(dealerValues);
        while (score > 21)
        {
            bool found = false;
            for (int i = 0; i < temp.Count; i++)
            {
                if (temp[i] == 11) { temp[i] = 1; score -= 10; found = true; break; }
            }
            if (!found) break;
        }
        return score;
    }

    // ─────────────────────────────────────────
    //  UI Updates
    // ─────────────────────────────────────────
    void UpdateScoreUI(bool revealDealer = false)
    {
        playerScoreText.text = "Player:\n " + SumValues(playerValues);

        if (revealDealer)
            dealerScoreText.text = "Dealer:\n " + GetDealerScore();
        else
            dealerScoreText.text = "Dealer:\n ?";
    }

    // ─────────────────────────────────────────
    //  Game Over
    // ─────────────────────────────────────────
    void EndGame(bool playerWins)
    {
        gameOver = true;
        SetPlayerButtons(false);
        summaryPanel.SetActive(true);

        if (playerWins)
        {
            if (AudioManager.instance != null) AudioManager.instance.PlayOneShot("Register");
            float winAmount = currentBet * 2f;
            if (GameManager.Instance != null) 
            {
                GameManager.Instance.AddMoney(winAmount);
                GameManager.Instance.blackjackWins++;
                GameManager.Instance.blackjackRevenue += currentBet; // Net profit is currentBet
                GameManager.Instance.ModifySanity(10);
            }
            summaryText.text = $"YOU WON!\n+${winAmount:F2}";
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.blackjackLosses++;
                GameManager.Instance.blackjackRevenue -= currentBet;
                GameManager.Instance.ModifySanity(-5);
            }
            summaryText.text = $"YOU LOST!\n-${currentBet:F2}";
        }

        UpdateScoreUI(revealDealer: true);
    }

    void EndGameDraw()
    {
        gameOver = true;
        SetPlayerButtons(false);
        summaryPanel.SetActive(true);

        if (GameManager.Instance != null) GameManager.Instance.AddMoney(currentBet);
        summaryText.text = $"DRAW!\n+${currentBet:F2}";
        UpdateScoreUI(revealDealer: true);
    }

    // ─────────────────────────────────────────
    //  Button State
    // ─────────────────────────────────────────
    void SetPlayerButtons(bool active)
    {
        hitButton.interactable = active;
        standButton.interactable = active;
    }

    // ─────────────────────────────────────────
    //  Card UI Creation
    // ─────────────────────────────────────────
    GameObject CreateCardUI(CardData data, Transform area, bool isHidden)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayOneShot("CardSlide");
        GameObject obj = Instantiate(cardPrefab, area);
        obj.GetComponent<Image>().sprite = isHidden ? cardBackSprite : data.cardSprite;
        return obj;
    }

    // ─────────────────────────────────────────
    //  Restart (Button callback)
    // ─────────────────────────────────────────
    public void Restart()
    {
        summaryPanel.SetActive(false);
        if (computerPanel != null) computerPanel.SetActive(false);
        if (betPanel != null) betPanel.SetActive(true);
        
        currentBet = 0;
        UpdateBetUI();

        // Skorları temizle
        if (playerScoreText != null) playerScoreText.text = "";
        if (dealerScoreText != null) dealerScoreText.text = "";
    }

    // ─────────────────────────────────────────
    //  Bet System
    // ─────────────────────────────────────────
    public void ModifyBet(float amount)
    {
        currentBet += amount;
        if (currentBet < 0) currentBet = 0;
        
        if (GameManager.Instance != null && currentBet > GameManager.Instance.money)
        {
            currentBet = GameManager.Instance.money;
        }
        UpdateBetUI();
    }

    private void UpdateBetUI()
    {
        if (currentBetText != null)
            currentBetText.text = $"${currentBet:F2}";
    }

    public void StartBetGame()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.money <= 0)
            {
                Debug.LogWarning("Cannot bet with 0 money!");
                return;
            }

            if (currentBet > GameManager.Instance.money)
            {
                currentBet = GameManager.Instance.money;
                UpdateBetUI();
                return; // Yeterli bakiye yoksa beti maksimuma çekip bekle
            }

            if (currentBet <= 0)
            {
                Debug.LogWarning("Bet must be greater than 0!");
                return;
            }
        }

        if (betPanel != null) betPanel.SetActive(false);
        if (computerPanel != null) computerPanel.SetActive(true);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemoveMoney(currentBet);
        }

        PrepareGame();
    }
}