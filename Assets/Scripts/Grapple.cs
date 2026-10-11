using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Les clics et les impacts sont traites avant le controleur de mouvement.
[DefaultExecutionOrder(-10)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class Grapple : MonoBehaviour
{
    public enum EtatGrappin { EnMain, Lance, Accroche, Retour }

    [Header("Lancer")]
    [SerializeField, Min(0.1f)] private float porteeMax = 12f;
    [SerializeField, Min(0.1f)] private float vitesseLancer = 35f;
    [SerializeField, Min(0.1f)] private float vitesseRetour = 45f;
    [SerializeField, Min(0.01f)] private float rayonImpact = 0.08f;
    [SerializeField] private LayerMask surfaces = Physics2D.DefaultRaycastLayers;
    [SerializeField] private Camera cameraVisee;

    [Header("Retraction au clic droit")]
    [SerializeField, Min(0.1f)] private float vitesseRetraction = 6f;
    [SerializeField, Min(0.1f)] private float longueurMin = 1.2f;

    [Header("Main et sprite")]
    [Tooltip("Optionnel : un enfant du joueur place sur sa main droite.")]
    [SerializeField] private Transform main;
    [Tooltip("Position locale de la main droite, miroir automatique a gauche.")]
    [SerializeField] private Vector2 positionMain = new Vector2(0.085f, -0.005f);
    [SerializeField] private Sprite spriteGrappin;
    [SerializeField, Min(0.01f)] private float tailleVisuel = 0.8f;
    [SerializeField] private float angleSprite = -135f;
    [SerializeField, Min(0.001f)] private float epaisseurCorde = 0.045f;
    [SerializeField] private Color couleurCorde = new Color(0.35f, 0.25f, 0.15f);

    public EtatGrappin Etat { get; private set; }
    public bool EstAccroche => Etat == EtatGrappin.Accroche;

    private Rigidbody2D corps;
    private Player joueur;
    private Mouvements mouvements;
    private SpriteRenderer spriteJoueur;
    private SpriteRenderer crochet;
    private LineRenderer corde;
    private Material materiauCorde;
    private DistanceJoint2D articulation;
    private GrappleAnchor anneau;
    private Collider2D collisionAnneau;
    private ContactFilter2D filtre;
    private readonly List<RaycastHit2D> impacts = new List<RaycastHit2D>(32);
    private Vector2 tete;
    private Vector2 directionLancer;
    private Vector2 directionVisee = Vector2.right;
    private Vector2 cibleClic;
    private float distanceParcourue;
    private bool clicDemande;
    private bool retractionDemandee;

    private void Awake()
    {
        corps = GetComponent<Rigidbody2D>();
        joueur = GetComponent<Player>();
        mouvements = GetComponent<Mouvements>();
        spriteJoueur = GetComponentInChildren<SpriteRenderer>();
        articulation = gameObject.AddComponent<DistanceJoint2D>();
        articulation.enabled = false;
        articulation.autoConfigureConnectedAnchor = false;
        articulation.autoConfigureDistance = false;
        // La corde peut se detendre : elle ne pousse jamais le joueur.
        articulation.maxDistanceOnly = true;
        articulation.enableCollision = true;
        articulation.anchor = Vector2.zero;
        filtre = new ContactFilter2D { useTriggers = true };
        filtre.SetLayerMask(surfaces);
        CreerVisuels();
    }

    private void Update()
    {
        retractionDemandee = false;
        if (!PeutJouer())
        {
            clicDemande = false;
            if ((joueur != null && joueur.EstMort) || !corps.simulated)
                Reinitialiser();
            return;
        }
        Mouse souris = Mouse.current;
        if (souris == null) return;
        bool surInterface = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        retractionDemandee = souris.rightButton.isPressed && !surInterface;
        if (cameraVisee == null) cameraVisee = Camera.main;
        if (cameraVisee == null) return;
        Ray rayon = cameraVisee.ScreenPointToRay(souris.position.ReadValue());
        Plane plan = new Plane(Vector3.forward, transform.position);
        if (!plan.Raycast(rayon, out float distance)) return;
        Vector2 cible = rayon.GetPoint(distance);
        Vector2 visee = cible - PositionMain();
        if (visee.sqrMagnitude > 0.0001f) directionVisee = visee.normalized;
        if (souris.leftButton.wasPressedThisFrame && !surInterface)
        {
            cibleClic = cible;
            clicDemande = true;
        }
    }

    private bool PeutJouer()
    {
        return Time.timeScale > 0f && corps.simulated
            && (joueur == null || !joueur.EstMort)
            && (mouvements == null || mouvements.isActiveAndEnabled);
    }

    private void FixedUpdate()
    {
        if (!PeutJouer()) return;
        if (clicDemande)
        {
            clicDemande = false;
            if (Etat == EtatGrappin.EnMain) LancerVers(cibleClic);
            else if (Etat != EtatGrappin.Retour) CommencerRetour();
        }
        switch (Etat)
        {
            case EtatGrappin.Lance:
                Avancer();
                break;
            case EtatGrappin.Accroche:
                if (anneau == null || !anneau.isActiveAndEnabled
                    || collisionAnneau == null || !collisionAnneau.enabled
                    || !collisionAnneau.gameObject.activeInHierarchy || !articulation.enabled)
                    CommencerRetour();
                else
                {
                    tete = anneau.Position;
                    ActualiserAncrage();
                    if (retractionDemandee) Retracter();
                }
                break;
            case EtatGrappin.Retour:
                Vector2 destination = PositionMain();
                tete = Vector2.MoveTowards(tete, destination, vitesseRetour * Time.fixedDeltaTime);
                if (Vector2.Distance(tete, destination) < 0.01f) Etat = EtatGrappin.EnMain;
                break;
        }
    }

    private void LancerVers(Vector2 cible)
    {
        Vector2 depart = PositionMain();
        Vector2 visee = cible - depart;
        if (visee.sqrMagnitude < 0.0001f) return;
        tete = depart;
        directionLancer = visee.normalized;
        distanceParcourue = 0f;
        Etat = EtatGrappin.Lance;
    }

    private void Avancer()
    {
        float pas = Mathf.Min(vitesseLancer * Time.fixedDeltaTime, porteeMax - distanceParcourue);
        if (pas <= 0f || Vector2.Distance(PositionMain(), tete) > porteeMax)
        {
            CommencerRetour();
            return;
        }
        // Un cast sur tout le trajet evite de traverser un petit anneau entre deux images.
        int nombre = Physics2D.CircleCast(tete, rayonImpact, directionLancer, filtre, impacts, pas);
        RaycastHit2D premier = default;
        float distanceImpact = float.PositiveInfinity;
        for (int i = 0; i < nombre; i++)
        {
            Collider2D autre = impacts[i].collider;
            if (autre == null || autre.attachedRigidbody == corps || autre.transform.IsChildOf(transform))
                continue;
            GrappleAnchor cibleAnneau = autre.GetComponentInParent<GrappleAnchor>();
            Ennemy ennemi = autre.GetComponentInParent<Ennemy>();
            Boss boss = autre.GetComponentInParent<Boss>();
            bool autorise = (cibleAnneau != null && cibleAnneau.isActiveAndEnabled)
                || (ennemi != null && ennemi.isActiveAndEnabled && !ennemi.EstMort)
                || (boss != null && boss.isActiveAndEnabled && !boss.EstMort);
            // Ignore les triggers decoratifs, pieces, portails et zones de detection.
            if (autre.isTrigger && !autorise) continue;
            if (impacts[i].distance < distanceImpact)
            {
                premier = impacts[i];
                distanceImpact = premier.distance;
            }
        }
        if (premier.collider != null)
        {
            tete += directionLancer * premier.distance;
            if (Vector2.Distance(PositionMain(), tete) > porteeMax)
            {
                CommencerRetour();
                return;
            }
            Ennemy ennemi = premier.collider.GetComponentInParent<Ennemy>();
            Boss boss = premier.collider.GetComponentInParent<Boss>();
            GrappleAnchor cibleAnneau = premier.collider.GetComponentInParent<GrappleAnchor>();
            if (ennemi != null && ennemi.isActiveAndEnabled)
            {
                ennemi.PrendreDegats(Random.Range(3, 7));
                CommencerRetour();
            }
            else if (boss != null && boss.isActiveAndEnabled)
            {
                boss.PrendreDegats(Random.Range(3, 7));
                CommencerRetour();
            }
            else if (cibleAnneau != null && cibleAnneau.isActiveAndEnabled)
                Accrocher(cibleAnneau, premier.collider);
            else
                CommencerRetour();
            return;
        }
        tete += directionLancer * pas;
        distanceParcourue += pas;
        Vector2 depuisMain = tete - PositionMain();
        if (depuisMain.magnitude > porteeMax)
        {
            tete = PositionMain() + depuisMain.normalized * porteeMax;
            CommencerRetour();
        }
        else if (distanceParcourue >= porteeMax) CommencerRetour();
    }

    private void Accrocher(GrappleAnchor cible, Collider2D collision)
    {
        anneau = cible;
        collisionAnneau = collision;
        tete = anneau.Position;
        if (Vector2.Distance(PositionMain(), tete) > porteeMax)
        {
            CommencerRetour();
            return;
        }
        articulation.connectedBody = collision.attachedRigidbody;
        ActualiserAncrage();
        // Ancrage au centre physique : pas de teleportation, ni de changement de vitesse.
        articulation.distance = Mathf.Max(0.1f, Vector2.Distance(corps.position, tete));
        articulation.enabled = true;
        Etat = EtatGrappin.Accroche;
        if (mouvements != null) mouvements.ConserverElanGrappin();
    }

    private void ActualiserAncrage()
    {
        Rigidbody2D support = articulation.connectedBody;
        articulation.connectedAnchor = support != null
            ? (Vector2)support.transform.InverseTransformPoint(tete) : tete;
    }

    private void Retracter()
    {
        float ancienneLongueur = articulation.distance;
        // Une accroche deja tres proche ne doit jamais etre allongee.
        float minimum = Mathf.Min(longueurMin, ancienneLongueur);
        float nouvelleLongueur = Mathf.MoveTowards(ancienneLongueur, minimum,
            vitesseRetraction * Time.fixedDeltaTime);
        articulation.distance = nouvelleLongueur;
        float raccourcissement = ancienneLongueur - nouvelleLongueur;
        if (raccourcissement <= 0f) return;

        Vector2 versAnneau = tete - corps.position;
        // Enroule d'abord le mou avant de tirer sur le joueur.
        if (versAnneau.magnitude < nouvelleLongueur - 0.05f) return;
        Vector2 direction = versAnneau.normalized;
        Rigidbody2D support = articulation.connectedBody;
        Vector2 vitesseSupport = support != null ? support.GetPointVelocity(tete) : Vector2.zero;
        float vitesseRadiale = Vector2.Dot(corps.linearVelocity - vitesseSupport, direction);
        float vitesseTirage = raccourcissement / Time.fixedDeltaTime;
        // Ajoute une vitesse de remontee sans effacer l'elan tangent au balancier.
        // Raccourcir uniquement le joint corrigerait la position sans donner cet elan.
        if (vitesseRadiale < vitesseTirage)
            corps.linearVelocity += direction * (vitesseTirage - vitesseRadiale);
    }

    private void CommencerRetour()
    {
        articulation.enabled = false;
        articulation.connectedBody = null;
        anneau = null;
        collisionAnneau = null;
        Etat = EtatGrappin.Retour;
    }

    private Vector2 PositionMain()
    {
        Vector3 locale = main != null ? transform.InverseTransformPoint(main.position) : (Vector3)positionMain;
        if (spriteJoueur != null && spriteJoueur.flipX) locale.x = -locale.x;
        return transform.TransformPoint(locale);
    }

    private void CreerVisuels()
    {
        crochet = new GameObject("Crochet visuel").AddComponent<SpriteRenderer>();
        crochet.transform.SetParent(transform, false);
        crochet.sprite = spriteGrappin;
        corde = new GameObject("Corde du grappin").AddComponent<LineRenderer>();
        corde.transform.SetParent(transform, false);
        corde.useWorldSpace = true;
        corde.positionCount = 2;
        corde.startWidth = corde.endWidth = epaisseurCorde;
        corde.startColor = corde.endColor = couleurCorde;
        corde.numCapVertices = 2;
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
            ?? Shader.Find("Sprites/Default");
        if (shader != null)
        {
            materiauCorde = new Material(shader);
            corde.sharedMaterial = materiauCorde;
            crochet.sharedMaterial = materiauCorde;
        }
        corde.enabled = false;
    }

    private void LateUpdate()
    {
        if (crochet == null || corde == null) return;
        bool visible = isActiveAndEnabled && (joueur == null || !joueur.EstMort)
            && (spriteJoueur == null || spriteJoueur.enabled);
        crochet.enabled = visible;
        corde.enabled = visible && Etat != EtatGrappin.EnMain;
        Vector2 depart = PositionMain();
        Vector2 positionTete = Etat == EtatGrappin.EnMain ? depart : tete;
        if (EstAccroche && anneau != null) positionTete = anneau.Position;
        Vector2 orientation = Etat == EtatGrappin.EnMain ? directionVisee
            : Etat == EtatGrappin.Lance ? directionLancer : positionTete - depart;
        if (orientation.sqrMagnitude < 0.0001f) orientation = directionLancer;
        if (orientation.sqrMagnitude < 0.0001f) orientation = Vector2.right;
        orientation.Normalize();
        Vector2 centre = positionTete + orientation * tailleVisuel
            * (Etat == EtatGrappin.EnMain ? 0.25f : -0.25f);
        crochet.transform.position = new Vector3(centre.x, centre.y, transform.position.z);
        crochet.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(orientation.y, orientation.x) * Mathf.Rad2Deg + angleSprite);
        if (spriteGrappin != null)
        {
            float echelle = tailleVisuel / Mathf.Max(0.001f, spriteGrappin.bounds.size.x);
            Vector3 parentScale = transform.lossyScale;
            crochet.transform.localScale = new Vector3(echelle / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                echelle / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
        }
        if (spriteJoueur != null)
        {
            crochet.sortingLayerID = corde.sortingLayerID = spriteJoueur.sortingLayerID;
            corde.sortingOrder = spriteJoueur.sortingOrder + 1;
            crochet.sortingOrder = spriteJoueur.sortingOrder + 2;
        }
        corde.SetPosition(0, new Vector3(depart.x, depart.y, transform.position.z));
        corde.SetPosition(1, new Vector3(positionTete.x, positionTete.y, transform.position.z));
    }

    private void Reinitialiser()
    {
        clicDemande = false;
        retractionDemandee = false;
        if (articulation != null)
        {
            articulation.enabled = false;
            articulation.connectedBody = null;
        }
        anneau = null;
        collisionAnneau = null;
        Etat = EtatGrappin.EnMain;
        if (crochet != null) crochet.enabled = false;
        if (corde != null) corde.enabled = false;
    }

    private void OnDisable() => Reinitialiser();

    private void OnDestroy()
    {
        if (articulation != null) Destroy(articulation);
        if (crochet != null) Destroy(crochet.gameObject);
        if (corde != null) Destroy(corde.gameObject);
        if (materiauCorde != null) Destroy(materiauCorde);
    }
}
