using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class FragilePlatforms : MonoBehaviour
{
    private float vitesse = 5f;
    private float chute = 5f;
    private float attente = 10f;
    private float delai_entree = 1f;

    private enum Etat { Disponible, Delai, Chute, Cachee }
    private Etat etat;
    private float tempsRestant;
    private SpriteRenderer spriteRenderer;
    private Collider2D platformCollider;
    private Rigidbody2D corps;
    private Vector2 positionInitiale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();
        corps = GetComponent<Rigidbody2D>();
        corps.bodyType = RigidbodyType2D.Kinematic;
        corps.interpolation = RigidbodyInterpolation2D.Interpolate;
        positionInitiale = corps.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (etat != Etat.Disponible || collision.gameObject.GetComponent<Player>() == null)
            return;

        // Reserve la plateforme des le contact pour ne lancer qu'une seule chute.
        etat = Etat.Delai;
        tempsRestant = delai_entree;
    }

    private void FixedUpdate()
    {
        Vector2 prochainePosition = corps.position;
        switch (etat)
        {
            case Etat.Delai:
                tempsRestant -= Time.fixedDeltaTime;
                if (tempsRestant <= 0f) etat = Etat.Chute;
                break;
            case Etat.Chute:
                Vector2 cible = positionInitiale + Vector2.down * chute;
                prochainePosition = Vector2.MoveTowards(corps.position, cible,
                    vitesse * Time.fixedDeltaTime);
                if (prochainePosition == cible)
                {
                    spriteRenderer.enabled = false;
                    platformCollider.enabled = false;
                    etat = Etat.Cachee;
                    tempsRestant = attente;
                }
                break;
            case Etat.Cachee:
                tempsRestant -= Time.fixedDeltaTime;
                if (tempsRestant <= 0f)
                {
                    // Retour instantane, collision encore desactivee : aucun joueur n'est pousse.
                    corps.linearVelocity = Vector2.zero;
                    corps.position = positionInitiale;
                    prochainePosition = positionInitiale;
                    spriteRenderer.enabled = true;
                    platformCollider.enabled = true;
                    etat = Etat.Disponible;
                }
                break;
        }
        corps.MovePosition(prochainePosition);
    }

    private void OnDisable()
    {
        if (corps != null) corps.linearVelocity = Vector2.zero;
    }
}
