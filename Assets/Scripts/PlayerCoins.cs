using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerCoins : MonoBehaviour
{
    // Compteur du joueur, visible dans l'Inspecteur pendant la partie.
    [SerializeField, Min(0)] private int coins = 0;
    // Permet aux autres scripts de lire le total sans le modifier directement.
    public int Coins => coins;

    private GameObject hud;
    private Text counter;
    private Player player;
    private readonly HeartGraphic[] hearts = new HeartGraphic[3];

    private void Awake()
    {
        player = GetComponent<Player>();
        // Crée le HUD par-dessus l'affichage du jeu.
        hud = new GameObject("Coins HUD", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // Adapte la taille du HUD à la résolution de l'écran.
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Crée le texte du compteur et règle son apparence.
        GameObject label = new GameObject("Coins", typeof(RectTransform), typeof(Text), typeof(Shadow));
        label.transform.SetParent(hud.transform, false);
        counter = label.GetComponent<Text>();
        counter.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counter.fontSize = 36;
        counter.fontStyle = FontStyle.Bold;
        counter.color = new Color(1f, 0.85f, 0.2f);
        counter.alignment = TextAnchor.UpperLeft;
        counter.raycastTarget = false;

        // Fixe le compteur en haut à gauche avec une petite marge.
        RectTransform rect = counter.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -80f);
        rect.sizeDelta = new Vector2(420f, 60f);

        // Ajoute une ombre pour rendre le texte plus lisible.
        label.GetComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
        RefreshHud();
    }

    private void Start()
    {
        player = GetComponent<Player>();
        for (int i = 0; i < hearts.Length; i++)
        {
            GameObject heart = new GameObject("Heart " + (i + 1), typeof(RectTransform), typeof(HeartGraphic));
            heart.transform.SetParent(hud.transform, false);
            hearts[i] = heart.GetComponent<HeartGraphic>();
            hearts[i].raycastTarget = false;
            RectTransform rect = hearts[i].rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f + i * 52f, -24f);
            rect.sizeDelta = new Vector2(40f, 35f);
        }
    }

    private void Update()
    {
        for (int i = 0; i < hearts.Length; i++)
            if (hearts[i] != null)
                hearts[i].color = player != null && i < player.PvActuels
                    ? new Color(1f, 0.15f, 0.25f) : new Color(0.25f, 0.25f, 0.25f);
    }

    // Ajoute une pièce et actualise aussitôt le HUD.
    public void AddCoin()
    {
        coins++;
        RefreshHud();
    }

    // Affiche la valeur actuelle du compteur.
    private void RefreshHud()
    {
        counter.text = $"Coins : {coins}";
    }

    // Supprime aussi le HUD lorsque le joueur est détruit.
    private void OnDestroy()
    {
        if (hud != null)
            Destroy(hud);
    }
}
