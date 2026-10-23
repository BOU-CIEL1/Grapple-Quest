using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Restaure la position avant l'initialisation du grappin et de la camera.
[DefaultExecutionOrder(-20)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [SerializeField, Min(0f)] private float dureeInvincibilite = 2f;
    [SerializeField, Min(0.01f)] private float intervalleClignotement = 0.1f;
    [SerializeField, Min(0f)] private float pauseMort = 1f;
    [SerializeField, Min(0.1f)] private float vitesseChuteMort = 6f;
    public int PvMax => 3;
    public int PvActuels { get; private set; } = 3;
    public bool EstMort => PvActuels == 0;
    public bool EstInvincible => EstMort || Time.time < finInvincibilite;
    private float finInvincibilite;
    private Rigidbody2D corps;
    private SpriteRenderer[] sprites;
    private Animator animateur;
    private bool pauseEnCours;
    private float tempsAvantMort;
    private Coroutine clignotement;
    private Vector3? positionCheckpoint;
    private static Vector3? checkpointAuRechargement;
    private static string sceneAuRechargement;

    public AudioClip checkpointSound;

    [SerializeField] private GameObject gameOverPanel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ReinitialiserCheckpoint()
    {
        checkpointAuRechargement = null;
        sceneAuRechargement = null;
    }

    private void Awake()
    {
        corps = GetComponent<Rigidbody2D>();
        if (checkpointAuRechargement.HasValue && sceneAuRechargement == gameObject.scene.path)
        {
            positionCheckpoint = checkpointAuRechargement;
            transform.position = positionCheckpoint.Value;
            corps.position = positionCheckpoint.Value;
        }
        // Seul le bouton de reapparition transmet un checkpoint au prochain chargement.
        ReinitialiserCheckpoint();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        animateur = GetComponentInChildren<Animator>();
        foreach (BossCollision boss in Object.FindObjectsByType<BossCollision>(FindObjectsSortMode.None))
            boss.IgnorerJoueur(this);
    }

    private void OnCollisionEnter2D(Collision2D collision) => ToucherEnnemi(collision.collider);
    private void OnCollisionStay2D(Collision2D collision) => ToucherEnnemi(collision.collider);
    private void OnTriggerEnter2D(Collider2D collision) => ToucherEnnemi(collision);
    private void OnTriggerStay2D(Collider2D collision) => ToucherEnnemi(collision);

    private void ToucherEnnemi(Collider2D collision)
    {
        if (!(collision is BoxCollider2D) || EstInvincible) return;
        Boss boss = collision.GetComponentInParent<Boss>();
        if (boss != null)
        {
            if (!boss.EstMort) PrendreDegats();
            return;
        }
        if (collision.GetComponentInParent<Ennemy>() != null
            || collision.CompareTag("Enemy") || collision.transform.root.CompareTag("Enemy"))
            PrendreDegats();
    }

    // Chaque coup retire 1 PV.
    public void PrendreDegats()
    {
        if (EstInvincible) return;
        PvActuels--;
        if (EstMort)
        {
            DemarrerMort();
            return;
        }
        finInvincibilite = Time.time + dureeInvincibilite;
        clignotement = StartCoroutine(Clignoter());
    }

    // Les zones mortelles ignorent les PV restants et l'invincibilite.
    public void Tuer()
    {
        if (EstMort) return;
        PvActuels = 0;
        DemarrerMort();
    }

    private void DemarrerMort()
    {
        if (clignotement != null) StopCoroutine(clignotement);
        clignotement = null;
        AfficherSprites(true);
        StartCoroutine(Mourir());
    }

    private IEnumerator Clignoter()
    {
        bool visible = true;
        while (Time.time < finInvincibilite)
        {
            visible = !visible;
            AfficherSprites(visible);
            yield return new WaitForSeconds(intervalleClignotement);
        }
        AfficherSprites(true);
        clignotement = null;
    }

    private void AfficherSprites(bool visible)
    {
        foreach (SpriteRenderer sprite in sprites)
            if (sprite != null) sprite.enabled = visible;
    }

    private IEnumerator Mourir()
    {
        Grapple grappin = GetComponent<Grapple>();
        if (grappin != null) grappin.enabled = false;
        Mouvements mouvements = GetComponent<Mouvements>();
        if (mouvements != null) mouvements.enabled = false;
        foreach (CameraSuivi suivi in Object.FindObjectsByType<CameraSuivi>(FindObjectsSortMode.None))
            suivi.ArreterSuivi(transform);
        corps.linearVelocity = Vector2.zero;
        corps.simulated = false;
        foreach (Collider2D collision in GetComponentsInChildren<Collider2D>()) collision.enabled = false;
        if (animateur != null && animateur.HasState(0, Animator.StringToHash("Base Layer.Player_Death")))
        {
            animateur.SetBool("IsDead", true);
            animateur.Play("Base Layer.Player_Death", 0, 0f);
            animateur.Update(0f);
        }
        tempsAvantMort = Time.timeScale;
        pauseEnCours = true;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(pauseMort);
        RestaurerTemps();
        if (animateur != null) animateur.enabled = false;
        Camera vue = Camera.main;
        float marge = 1f;
        foreach (SpriteRenderer sprite in sprites)
            if (sprite != null) marge = Mathf.Max(marge, sprite.bounds.extents.y);
        float vitesse = vitesseChuteMort;
        float distance = 0f;
        while (vue != null ? vue.WorldToViewportPoint(transform.position + Vector3.up * marge).y >= 0f : distance < 30f)
        {
            vitesse += 9.81f * Time.unscaledDeltaTime;
            float pas = vitesse * Time.unscaledDeltaTime;
            transform.position += Vector3.down * pas;
            distance += pas;
            yield return null;
        }
        AfficherSprites(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public bool EstCheckpointActif(Vector3 position) =>
        positionCheckpoint.HasValue && positionCheckpoint.Value == position;

    public bool ActiverCheckpoint(Vector3 position)
    {
        if (EstMort || EstCheckpointActif(position)) return false;
        positionCheckpoint = position;
        AudioManager.Instance.PlayClipAt(checkpointSound, transform.position);
        return true;
    }

    // Le bouton existant recharge le niveau avec les PV et les controles reinitialises.
    public void RecommencerJeu()
    {
        checkpointAuRechargement = positionCheckpoint;
        sceneAuRechargement = gameObject.scene.path;
        pauseEnCours = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void RestaurerTemps()
    {
        if (!pauseEnCours) return;
        Time.timeScale = tempsAvantMort;
        pauseEnCours = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        RestaurerTemps();
        if (sprites != null) AfficherSprites(true);
        finInvincibilite = 0f;
        clignotement = null;
    }
}
