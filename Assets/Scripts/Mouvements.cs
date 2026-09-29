using UnityEngine;
using UnityEngine.InputSystem;

// Ajoute les composants de physique et de collision nécessaires lors de l'ajout du script.
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class Mouvements : MonoBehaviour
{
    // Réglages accessibles dans l'Inspecteur Unity, avec une valeur minimale de zéro.
    [SerializeField, Min(0f)] private float vitesseDeplacement = 5f;
    [SerializeField, Min(0f)] private float vitesseSaut = 5f;

    // Référence au composant qui gère la physique du personnage.
    private Rigidbody2D corps;
    // Tableau réutilisé pour lire les contacts sans créer un nouveau tableau à chaque fois.
    private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
    // -1 = gauche, 0 = immobile, 1 = droite.
    private float direction;
    // Mémorise l'appui sur Espace jusqu'à la prochaine mise à jour de la physique.
    private bool sautDemande;

    // Initialise les références au chargement du personnage.
    private void Awake()
    {
        corps = GetComponent<Rigidbody2D>();
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
            vitesse.y = vitesseSaut;

        corps.linearVelocity = vitesse;
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
}
