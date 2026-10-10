using UnityEngine;
using UnityEngine.InputSystem;

// Ajoute les composants de physique et de collision nécessaires lors de l'ajout du script.
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Mouvements : MonoBehaviour
{
    // Réglages accessibles dans l'Inspecteur Unity, avec une valeur minimale de zéro.
    [SerializeField, Min(0f)] private float vitesseDeplacement = 10f;
    [SerializeField, Min(0f)] private float hauteurSaut = 2f;
    [SerializeField, Min(0.1f)] private float graviteJoueur = 2.5f;
    [SerializeField, Min(1f)] private float multiplicateurChute = 1.35f;
    [SerializeField] private CameraSuivi cameraSuivi;
    private CapsuleCollider2D collision;

    // Référence au composant qui gère la physique du personnage.
    private Rigidbody2D corps;
    // Tableau réutilisé pour lire les contacts sans créer un nouveau tableau à chaque fois.
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    // -1 = gauche, 0 = immobile, 1 = droite.
    private float direction;
    // Mémorise l'appui sur Espace jusqu'à la prochaine mise à jour de la physique.
    private bool sautDemande;
    private Animator animateur;
    private SpriteRenderer sprite;
    private PhysicsMaterial2D materiauJoueur;

    // Initialise les références au chargement du personnage.
    private void Awake()
    {
        corps = GetComponent<Rigidbody2D>();
        corps.gravityScale = graviteJoueur;
        collision = GetComponent<CapsuleCollider2D>();
        // Le joueur doit glisser le long des murs, sans rebond physique.
        materiauJoueur = new PhysicsMaterial2D("Joueur sans friction")
        {
            friction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine2D.Multiply,
            bounceCombine = PhysicsMaterialCombine2D.Multiply
        };
        collision.sharedMaterial = materiauJoueur;
        animateur = GetComponentInChildren<Animator>();
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (cameraSuivi == null)
            cameraSuivi = Object.FindFirstObjectByType<CameraSuivi>();
        // Empêche le personnage de basculer, tout en conservant les autres contraintes.
        corps.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }
 
    // Lit les touches à chaque image pour détecter même les appuis courts.
    private void Update()
    {
        Keyboard clavier = Keyboard.current;
        direction = 0f;

        // Évite une erreur si aucun clavier n'est disponible.
        if (clavier == null)
            return;

        bool gauche = clavier.FindKeyOnCurrentKeyboardLayout("q")?.isPressed == true
            || clavier.leftArrowKey.isPressed;
        bool droite = clavier.FindKeyOnCurrentKeyboardLayout("d")?.isPressed == true
            || clavier.rightArrowKey.isPressed;

        // Si les deux directions sont enfoncées, elles s'annulent.
        direction = (droite ? 1f : 0f) - (gauche ? 1f : 0f);
        // Conserve une demande déjà reçue ; maintenir Espace ne répète pas le saut.
        sautDemande |= clavier.spaceKey.wasPressedThisFrame;
    }

    // Applique les mouvements au rythme de la simulation physique de Unity.
    private void FixedUpdate()
    {
        // Modifie la vitesse horizontale en conservant la vitesse verticale (chute ou saut).
        Vector2 vitesse = corps.linearVelocity;
        vitesse.x = direction * vitesseDeplacement;

        // Donne une vitesse vers le haut seulement si le personnage touche le sol.
        if (sautDemande && EstAuSol())
        {
            float gravite = Mathf.Max(0f, -Physics2D.gravity.y * corps.gravityScale);
            // v = sqrt(2 * g * h), avec compensation du pas de physique.
            vitesse.y = hauteurSaut > 0f && gravite > 0f
                ? Mathf.Sqrt(2f * gravite * hauteurSaut)
                    + 0.5f * gravite * Time.fixedDeltaTime
                : 0f;
        }

        // Renforce seulement la descente, sans modifier la hauteur du saut.
        if (vitesse.y < 0f)
            vitesse.y += Physics2D.gravity.y * corps.gravityScale
                * (multiplicateurChute - 1f) * Time.fixedDeltaTime;

        if (cameraSuivi != null)
            cameraSuivi.LimiterDeplacement(corps, collision, ref vitesse);

        // Ne reapplique pas une vitesse qui pousse dans un mur deja touche.
        int nombreContacts = corps.GetContacts(contacts);
        for (int i = 0; i < nombreContacts; i++)
        {
            Vector2 normale = contacts[i].normal;
            if (Mathf.Abs(normale.x) > 0.9f && Mathf.Abs(normale.y) < 0.3f
                && vitesse.x * normale.x < 0f)
                vitesse.x = 0f;
        }

        corps.linearVelocity = vitesse;
        if (animateur != null && animateur.runtimeAnimatorController != null)
            animateur.SetFloat("Speed", Mathf.Abs(vitesse.x));
        if (sprite != null && direction != 0f)
            sprite.flipX = direction < 0f;
        // Consomme la demande, même si le saut était impossible en l'air.
        sautDemande = false;
    }

    // Cherche un contact qui soutient le personnage par-dessous.
    private bool EstAuSol()
    {
        int nombreContacts = corps.GetContacts(contacts);

        for (int i = 0; i < nombreContacts; i++)
        {
            // La normale indique la direction du contact : une composante Y supérieure
            // à 0.5 correspond à un appui suffisamment orienté vers le haut.
            // Un mur ou un plafond ne permet donc pas de sauter.
            if (contacts[i].normal.y > 0.5f)
                return true;
        }

        // Aucun contact avec un sol suffisamment horizontal n'a été trouvé.
        return false;
    }

    // Efface les entrées mémorisées lorsque le script ou le personnage est désactivé.
    private void OnDisable()
    {
        direction = 0f;
        sautDemande = false;
    }

    private void OnDestroy()
    {
        if (materiauJoueur != null) Destroy(materiauJoueur);
    }
}
