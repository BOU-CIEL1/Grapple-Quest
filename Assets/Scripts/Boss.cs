using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class Boss : MonoBehaviour
{
    [Header("Statistiques")]
    [SerializeField, Min(1)] private int pvMax = 100;
    [SerializeField, Min(0)] private int attaque = 5;

    [Header("Animations")]
    [SerializeField] private Animator animateur;
    [SerializeField, Min(0.1f)] private float delaiSortMin = 3f;
    [SerializeField, Min(0.1f)] private float delaiSortMax = 6f;

    [Header("Arene et deplacement")]
    [SerializeField] private Transform joueur;
    [SerializeField] private float limiteGauche = -8f;
    [SerializeField] private float limiteDroite = 16f;
    [SerializeField, Min(0f)] private float vitesseDeplacement = 2f;
    [SerializeField, Min(0.1f)] private float distanceArret = 1.5f;

    [Header("Teleportation")]
    [SerializeField, Range(0f, 1f)] private float chanceTeleportation = 0.25f;
    [SerializeField, Min(0.1f)] private float delaiTeleportation = 10f;
    // Maw : Teleport commence a Frame_114. La fin de Frame_141 est a 3.12 s.
    [SerializeField, Min(0f), Tooltip("Temps dans le clip Teleport a la fin de Frame_141.")]
    private float instantTeleportation = 3.12f;

    [Header("Portee des attaques")]
    [SerializeField, Min(0f)] private float porteeHandAttack = 2f;
    [SerializeField, Min(0f)] private float porteeBoomerang = 3f;
    [SerializeField, Min(0f)] private float porteeExplosion = 5f;
    [SerializeField, Min(0f)] private float porteeSummon = 8f;

    private int pvActuels;
    private GameObject hud;
    private RectTransform remplissage;
    private Text textePv;
    private Coroutine sorts;
    private Rigidbody2D corps;
    private bool poursuivre;
    private float prochaineTeleportation;
    private readonly int[] attaquesDisponibles = new int[4];
    private static readonly int Idle = Animator.StringToHash("Base Layer.Idle");
    private static readonly int CastSpell = Animator.StringToHash("Base Layer.CastSpell");
    private static readonly int Floating = Animator.StringToHash("Base Layer.Floating");
    private static readonly int Teleport = Animator.StringToHash("Base Layer.Teleport");
    private static readonly int FinishSpell = Animator.StringToHash("Base Layer.FinishSpell");
    private static readonly int HandAttack = Animator.StringToHash("Base Layer.HandAttack");
    private static readonly int Boomerang = Animator.StringToHash("Base Layer.Boomerang");
    private static readonly int Explosion = Animator.StringToHash("Base Layer.Explosion");
    private static readonly int Summon = Animator.StringToHash("Base Layer.Summon");

    public int PvMax => pvMax;
    public int PvActuels => pvActuels;
    public int Attaque => attaque;
    public bool EstMort => pvActuels <= 0;

    private void Awake()
    {
        pvActuels = pvMax;
        corps = GetComponent<Rigidbody2D>();
        if (joueur == null)
        {
            Mouvements personnage = Object.FindFirstObjectByType<Mouvements>();
            if (personnage != null)
                joueur = personnage.transform;
        }
        if (animateur == null)
            animateur = GetComponentInChildren<Animator>();
        CreerHud();
        ActualiserHud();
    }

    private void OnEnable()
    {
        if (hud != null)
            hud.SetActive(true);
        if (!EstMort)
            sorts = StartCoroutine(LancerSorts());
    }

    private IEnumerator LancerSorts()
    {
        if (animateur == null || animateur.runtimeAnimatorController == null)
        {
            Debug.LogWarning("Boss : un Animator est necessaire.", this);
            yield break;
        }

        int[] etats = { Idle, CastSpell, Floating, Teleport, FinishSpell,
            HandAttack, Boomerang, Explosion, Summon };
        foreach (int etat in etats)
        {
            if (!animateur.HasState(0, etat))
            {
                Debug.LogWarning("Boss : une animation requise manque dans le controller Maw.", this);
                yield break;
            }
        }

        animateur.Play(Idle, 0, 0f);
        prochaineTeleportation = Time.time + delaiTeleportation;
        while (!EstMort)
        {
            poursuivre = true;
            float prochaineAction = Time.time + Random.Range(delaiSortMin, delaiSortMax);
            while (joueur == null || Time.time < prochaineAction)
                yield return null;

            // Continue a approcher tant qu'aucun pattern n'est a portee.
            int pattern;
            while ((pattern = ChoisirAttaque()) == 0)
            {
                if (PeutSeTeleporter())
                    break;
                yield return null;
            }

            poursuivre = false;
            ArreterDeplacement();
            OrienterVersJoueur();
            if (PeutSeTeleporter() && (pattern == 0 || Random.value < chanceTeleportation))
            {
                yield return Teleporter();
            }
            else
            {
                yield return JouerUneFois(CastSpell);
                yield return JouerUneFois(pattern);
                yield return JouerUneFois(FinishSpell);
            }

            animateur.Play(Idle, 0, 0f);
        }
    }

    private int ChoisirAttaque()
    {
        if (joueur == null)
            return 0;
        float distance = Vector2.Distance(corps.position, joueur.position);
        int nombre = 0;
        if (distance <= porteeHandAttack) attaquesDisponibles[nombre++] = HandAttack;
        if (distance <= porteeBoomerang) attaquesDisponibles[nombre++] = Boomerang;
        if (distance <= porteeExplosion) attaquesDisponibles[nombre++] = Explosion;
        if (distance <= porteeSummon) attaquesDisponibles[nombre++] = Summon;
        return nombre > 0 ? attaquesDisponibles[Random.Range(0, nombre)] : 0;
    }

    private IEnumerator JouerUneFois(int etat)
    {
        animateur.Play(etat, 0, 0f);
        yield return null;
        while (animateur.GetCurrentAnimatorStateInfo(0).fullPathHash == etat
            && animateur.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
    }

    private bool PeutSeTeleporter()
    {
        return joueur != null && Time.time >= prochaineTeleportation
            && Mathf.Abs(joueur.position.x - corps.position.x) > distanceArret;
    }

    private IEnumerator Teleporter()
    {
        animateur.Play(Teleport, 0, 0f);
        yield return null;
        AnimatorStateInfo etat = animateur.GetCurrentAnimatorStateInfo(0);
        // Utilise le temps de l'Animator : la vitesse du clip et les pauses
        // sont prises en compte sans couper ni relancer l'animation.
        float seuil = Mathf.Min(instantTeleportation / etat.length, 0.99f);
        while (animateur.GetCurrentAnimatorStateInfo(0).normalizedTime < seuil)
            yield return null;

        if (joueur != null)
        {
            float cote = Random.value < 0.5f ? -1f : 1f;
            Vector2 destination = corps.position;
            destination.x = Mathf.Clamp(joueur.position.x + cote * distanceArret,
                limiteGauche, limiteDroite);
            corps.position = destination;
            OrienterVersJoueur();
        }
        prochaineTeleportation = Time.time + delaiTeleportation;
        while (animateur.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            yield return null;
    }

    private void FixedUpdate()
    {
        Vector2 position = corps.position;
        position.x = Mathf.Clamp(position.x, limiteGauche, limiteDroite);
        if (position != corps.position)
            corps.position = position;

        Vector2 vitesse = corps.linearVelocity;
        vitesse.x = 0f;
        if (poursuivre && !EstMort && joueur != null)
        {
            float ecart = joueur.position.x - position.x;
            float trajet = Mathf.Max(0f, Mathf.Abs(ecart) - distanceArret);
            float cible = Mathf.Clamp(position.x + Mathf.Sign(ecart)
                * Mathf.Min(vitesseDeplacement * Time.fixedDeltaTime, trajet),
                limiteGauche, limiteDroite);
            vitesse.x = (cible - position.x) / Time.fixedDeltaTime;
            OrienterVersJoueur();
            int animation = Mathf.Abs(vitesse.x) > 0.01f ? Floating : Idle;
            if (animateur != null && animateur.HasState(0, animation)
                && animateur.GetCurrentAnimatorStateInfo(0).fullPathHash != animation)
                animateur.Play(animation, 0, 0f);
        }
        corps.linearVelocity = vitesse;
    }

    private void OrienterVersJoueur()
    {
        if (joueur == null || Mathf.Abs(joueur.position.x - corps.position.x) < 0.01f)
            return;
        // Le sprite Maw regarde a droite dans son orientation d'origine.
        transform.rotation = Quaternion.Euler(0f, joueur.position.x < corps.position.x ? 180f : 0f, 0f);
    }

    private void ArreterDeplacement()
    {
        if (corps != null)
            corps.linearVelocity = new Vector2(0f, corps.linearVelocity.y);
    }

    // A appeler depuis une arme ou un projectile lorsqu'il touche le boss.
    public void PrendreDegats(int degats)
    {
        if (degats <= 0 || EstMort)
            return;

        pvActuels = Mathf.Max(0, pvActuels - degats);
        ActualiserHud();
        if (EstMort)
        {
            ArreterSorts();
            if (animateur != null && animateur.HasState(0, Idle))
                animateur.Play(Idle, 0, 0f);
        }
    }

    private void CreerHud()
    {
        hud = new GameObject("HUD Roi demon", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panneau = CreerRect("Barre du boss", hud.transform);
        panneau.anchorMin = new Vector2(0.25f, 0f);
        panneau.anchorMax = new Vector2(0.75f, 0f);
        panneau.pivot = new Vector2(0.5f, 0f);
        panneau.anchoredPosition = new Vector2(0f, 40f);
        panneau.sizeDelta = new Vector2(0f, 80f);

        Font police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text titre = CreerTexte("Titre", panneau, police, 30);
        titre.text = "Roi d\u00e9mon";
        titre.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        titre.rectTransform.anchorMax = Vector2.one;

        RectTransform fond = CreerRect("Fond", panneau);
        fond.anchorMax = new Vector2(1f, 0f);
        fond.sizeDelta = new Vector2(0f, 32f);
        fond.pivot = new Vector2(0.5f, 0f);
        Image imageFond = fond.gameObject.AddComponent<Image>();
        imageFond.color = new Color(0.08f, 0.04f, 0.05f, 0.95f);
        imageFond.raycastTarget = false;

        RectTransform interieur = CreerRect("Interieur", fond);
        interieur.offsetMin = new Vector2(3f, 3f);
        interieur.offsetMax = new Vector2(-3f, -3f);
        remplissage = CreerRect("PV", interieur);
        Image imagePv = remplissage.gameObject.AddComponent<Image>();
        imagePv.color = new Color(0.8f, 0.08f, 0.15f);
        imagePv.raycastTarget = false;
        textePv = CreerTexte("Valeur PV", fond, police, 22);
    }

    private static RectTransform CreerRect(string nom, Transform parent)
    {
        RectTransform rect = new GameObject(nom, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Text CreerTexte(string nom, Transform parent, Font police, int taille)
    {
        Text texte = CreerRect(nom, parent).gameObject.AddComponent<Text>();
        texte.font = police;
        texte.fontSize = taille;
        texte.color = Color.white;
        texte.alignment = TextAnchor.MiddleCenter;
        texte.raycastTarget = false;
        return texte;
    }

    private void ActualiserHud()
    {
        remplissage.anchorMax = new Vector2((float)pvActuels / pvMax, 1f);
        textePv.text = $"{pvActuels} / {pvMax}";
    }

    private void ArreterSorts()
    {
        poursuivre = false;
        ArreterDeplacement();
        if (sorts != null)
            StopCoroutine(sorts);
        sorts = null;
    }

    private void OnDisable()
    {
        ArreterSorts();
        if (hud != null)
            hud.SetActive(false);
    }

    private void OnDestroy()
    {
        if (hud != null)
            Destroy(hud);
    }

    private void OnValidate()
    {
        pvMax = Mathf.Max(1, pvMax);
        attaque = Mathf.Max(0, attaque);
        delaiSortMin = Mathf.Max(0.1f, delaiSortMin);
        delaiSortMax = Mathf.Max(delaiSortMin, delaiSortMax);
        limiteDroite = Mathf.Max(limiteGauche, limiteDroite);
        distanceArret = Mathf.Max(0.1f, distanceArret);
    }
}
