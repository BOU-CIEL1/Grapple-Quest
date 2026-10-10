using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Ennemy : MonoBehaviour
{
    [Header("Patrouille")]
    [SerializeField, Min(0f)] private float vitesse = 2f;
    [SerializeField] private bool commencerVersDroite = false;
    [SerializeField] private bool spriteRegardeADroite = true;
    [SerializeField] private LayerMask terrain = ~0;
    [SerializeField, Min(0.01f)] private float distanceObstacle = 0.15f;
    [SerializeField, Min(0.05f)] private float profondeurSol = 0.4f;
    [Header("Vol")]
    [SerializeField] private bool volant;
    [SerializeField, Min(0.1f)] private float demiLargeurPatrouille = 4f;

    private Rigidbody2D corps;
    private BoxCollider2D collision;
    private SpriteRenderer sprite;
    private Animator animateur;
    private ContactFilter2D filtre;
    private readonly RaycastHit2D[] resultats = new RaycastHit2D[32];
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    private int direction;
    private Vector2 origine;
    private static readonly int Speed = Animator.StringToHash("Speed");
    private bool utiliseSpeed;

    private void Awake()
    {
        corps = GetComponent<Rigidbody2D>();
        collision = GetComponent<BoxCollider2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        animateur = GetComponentInChildren<Animator>();
        collision.isTrigger = false;
        corps.simulated = true;
        corps.bodyType = RigidbodyType2D.Dynamic;
        corps.gravityScale = volant ? 0f : 2.5f;
        corps.constraints = RigidbodyConstraints2D.FreezeRotation
            | (volant ? RigidbodyConstraints2D.FreezePositionY : RigidbodyConstraints2D.None);
        corps.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corps.interpolation = RigidbodyInterpolation2D.Interpolate;
        direction = commencerVersDroite ? 1 : -1;
        filtre = new ContactFilter2D { useTriggers = false };
        filtre.SetLayerMask(terrain);
        if (animateur != null)
            foreach (AnimatorControllerParameter parametre in animateur.parameters)
                if (parametre.nameHash == Speed && parametre.type == AnimatorControllerParameterType.Float)
                    utiliseSpeed = true;
        Orienter();
    }

    private void Start() => origine = corps.position;

    private void FixedUpdate()
    {
        Bounds bounds = collision.bounds;
        bool auSol = !volant && EstAuSol();
        float vitesseX = direction * vitesse;
        if (DoitTourner(direction, bounds, auSol))
        {
            // Sur une plateforme trop petite ou dans un passage ferme, reste sur place.
            if (DoitTourner(-direction, bounds, auSol))
                vitesseX = 0f;
            else
            {
                direction = -direction;
                vitesseX = direction * vitesse;
            }
        }
        corps.linearVelocity = new Vector2(vitesseX, volant ? 0f : corps.linearVelocity.y);
        Orienter();
        if (utiliseSpeed) animateur.SetFloat(Speed, Mathf.Abs(vitesseX));
    }

    private bool DoitTourner(int sens, Bounds bounds, bool auSol)
    {
        float avance = vitesse * Time.fixedDeltaTime + distanceObstacle;
        // Les contacts donnent une vraie normale, meme si le cast part en contact.
        int nombreContacts = corps.GetContacts(contacts);
        for (int i = 0; i < nombreContacts; i++)
            if (contacts[i].normal.x * sens < -0.7f
                && Mathf.Abs(contacts[i].normal.y) < 0.5f
                && (EstTerrain(contacts[i].collider) || EstTerrain(contacts[i].otherCollider)))
                return true;
        Vector2 taille = new Vector2(bounds.size.x, Mathf.Max(0.01f, bounds.size.y - 0.1f));
        int nombre = Physics2D.BoxCast(bounds.center, taille, 0f, Vector2.right * sens,
            filtre, resultats, avance);
        for (int i = 0; i < nombre; i++)
            // Un chevauchement initial donne une normale opposee au cast,
            // meme pour le sol : ne pas l'interpreter comme un mur.
            if (resultats[i].distance > 0f && EstTerrain(resultats[i].collider)
                && resultats[i].normal.x * sens < -0.5f)
                return true;
        if (volant)
        {
            float prochainX = corps.position.x + sens * vitesse * Time.fixedDeltaTime;
            return sens > 0 ? prochainX > origine.x + demiLargeurPatrouille
                : prochainX < origine.x - demiLargeurPatrouille;
        }
        if (!auSol) return false; // Laisse tomber les ennemis places initialement en l'air.
        Vector2 devant = new Vector2(bounds.center.x + sens * (bounds.extents.x + avance), bounds.min.y + 0.1f);
        nombre = Physics2D.Raycast(devant, Vector2.down, filtre, resultats, profondeurSol + 0.1f);
        for (int i = 0; i < nombre; i++)
            if (EstTerrain(resultats[i].collider) && resultats[i].normal.y > 0.5f)
                return false;
        return true;
    }

    private bool EstTerrain(Collider2D autre)
    {
        return autre != null && !autre.isTrigger && autre.attachedRigidbody != corps
            && autre.GetComponentInParent<Player>() == null
            && autre.GetComponentInParent<Ennemy>() == null
            && autre.GetComponentInParent<Boss>() == null;
    }

    private bool EstAuSol()
    {
        int nombre = corps.GetContacts(contacts);
        for (int i = 0; i < nombre; i++)
            if (contacts[i].normal.y > 0.5f
                && (EstTerrain(contacts[i].collider) || EstTerrain(contacts[i].otherCollider)))
                return true;
        return false;
    }

    private void Orienter()
    {
        if (sprite != null) sprite.flipX = spriteRegardeADroite ? direction < 0 : direction > 0;
    }

    private void OnDisable()
    {
        if (corps != null) corps.linearVelocity = new Vector2(0f, corps.linearVelocity.y);
        if (utiliseSpeed && animateur != null) animateur.SetFloat(Speed, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!volant) return;
        Vector3 centre = Application.isPlaying ? (Vector3)origine : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(centre + Vector3.left * demiLargeurPatrouille,
            centre + Vector3.right * demiLargeurPatrouille);
    }
}
