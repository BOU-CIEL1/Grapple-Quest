using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraSuivi : MonoBehaviour
{
    [SerializeField] private Transform joueur;
    [SerializeField] private float limiteGauche = -6f;
    [SerializeField, Range(0.1f, 0.5f)] private float positionEcran = 0.33f;
    [SerializeField, Min(0.01f)] private float amortissement = 0.18f;
    [SerializeField, Min(0f)] private float zoneVerticale = 2f;
    [SerializeField, Min(0f)] private float margeBord = 0.05f;

    private Camera vue;
    private Collider2D collisionJoueur;
    private Vector2 vitesseSuivi;
    private float hauteurInitiale;
    private float decalageVertical;

    private void Awake()
    {
        vue = GetComponent<Camera>();
        hauteurInitiale = transform.position.y;
        if (joueur == null)
            return;

        collisionJoueur = joueur.GetComponent<Collider2D>();
        decalageVertical = hauteurInitiale - joueur.position.y;
        Rigidbody2D corps = joueur.GetComponent<Rigidbody2D>();
        if (corps != null)
            corps.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // Limite physique lorsque la camera atteint sa position minimale.
    // Le calcul reste valable lorsque le format de la fenetre change.
    public void LimiterDeplacement(Rigidbody2D corps, Collider2D collision, ref Vector2 vitesse)
    {
        if (!isActiveAndEnabled || vue == null || !vue.orthographic)
            return;

        float bord = limiteGauche - vue.orthographicSize * vue.aspect + margeBord;
        float largeurGauche = collision.transform.position.x - collision.bounds.min.x;
        float minimum = bord + largeurGauche;
        if (corps.position.x < minimum)
            corps.position = new Vector2(minimum, corps.position.y);

        float vitesseMinimum = (minimum - corps.position.x) / Time.fixedDeltaTime;
        vitesse.x = Mathf.Max(vitesse.x, vitesseMinimum);
    }

    private void LateUpdate()
    {
        if (joueur == null || !vue.orthographic)
            return;

        float demiLargeur = vue.orthographicSize * vue.aspect;
        float cibleX = Mathf.Max(limiteGauche,
            joueur.position.x + (0.5f - positionEcran) * demiLargeur * 2f);
        Vector3 position = transform.position;
        position.x = Mathf.Max(limiteGauche, Mathf.SmoothDamp(position.x,
            cibleX, ref vitesseSuivi.x, amortissement));

        // Rattrape immediatement un retour rapide a gauche pour garder le joueur visible.
        float bordJoueur = collisionJoueur != null ? collisionJoueur.bounds.min.x : joueur.position.x;
        float maximumX = Mathf.Max(limiteGauche, bordJoueur + demiLargeur - margeBord);
        if (position.x > maximumX)
        {
            position.x = maximumX;
            vitesseSuivi.x = 0f;
        }

        float ecartY = joueur.position.y + decalageVertical - hauteurInitiale;
        float cibleY = hauteurInitiale + Mathf.Sign(ecartY) * Mathf.Max(0f, Mathf.Abs(ecartY) - zoneVerticale);
        position.y = Mathf.SmoothDamp(position.y, cibleY, ref vitesseSuivi.y, amortissement * 1.5f);
        transform.position = position;
    }
}
