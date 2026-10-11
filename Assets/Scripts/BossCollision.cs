using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BossCollision : MonoBehaviour
{
    private BoxCollider2D collisionSol;


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

        // Le corps reste solide pour le decor, mais le joueur le traverse.
        // Un second collider detecte le contact sans bloquer ses mouvements.
        BoxCollider2D contact = gameObject.AddComponent<BoxCollider2D>();
        contact.size = collisionSol.size;
        contact.offset = collisionSol.offset;
        contact.isTrigger = true;
    }

    private void Start()
    {
        foreach (Player personnage in Object.FindObjectsByType<Player>(FindObjectsSortMode.None))
            IgnorerJoueur(personnage);
    }

    public void IgnorerJoueur(Player personnage)
    {
        if (collisionSol == null) collisionSol = GetComponent<BoxCollider2D>();
        foreach (Collider2D collision in personnage.GetComponentsInChildren<Collider2D>())
            Physics2D.IgnoreCollision(collisionSol, collision);
    }

    private void OnCollisionEnter2D(Collision2D contact)
    {
        // Prend aussi en charge un joueur instancie apres le lancement de la scene.
        Player personnage = contact.collider.GetComponentInParent<Player>();
        if (personnage != null) IgnorerJoueur(personnage);
    }

}
