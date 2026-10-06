using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BossCollision : MonoBehaviour
{
    private BoxCollider2D collisionSol;
    private Collider2D[] collisionsJoueur;

    private void Awake()
    {
        collisionSol = GetComponent<BoxCollider2D>();
        // Une collision solide permet au sol de soutenir le boss.
        collisionSol.isTrigger = false;

        Rigidbody2D corps = GetComponent<Rigidbody2D>();
        corps.bodyType = RigidbodyType2D.Dynamic;
        corps.simulated = true;
        if (corps.gravityScale <= 0f)
            corps.gravityScale = 1f;
        corps.constraints |= RigidbodyConstraints2D.FreezeRotation;
        corps.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corps.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void OnEnable()
    {
        // Le joueur est reconnu par son script, sans imposer un tag ou un layer.
        Mouvements joueur = Object.FindFirstObjectByType<Mouvements>();
        collisionsJoueur = joueur != null
            ? joueur.GetComponentsInChildren<Collider2D>(true)
            : System.Array.Empty<Collider2D>();
        IgnorerCollisionsJoueur();
    }

    private void FixedUpdate()
    {
        // Unity peut perdre les exclusions lorsqu'un collider est reactive.
        IgnorerCollisionsJoueur();
    }

    private void IgnorerCollisionsJoueur()
    {
        foreach (Collider2D collisionJoueur in collisionsJoueur)
        {
            // Conserve les triggers pour les futures zones de detection/attaque.
            if (collisionJoueur != null && !collisionJoueur.isTrigger
                && collisionJoueur.isActiveAndEnabled && collisionSol.isActiveAndEnabled)
                Physics2D.IgnoreCollision(collisionSol, collisionJoueur, true);
        }
    }
}
