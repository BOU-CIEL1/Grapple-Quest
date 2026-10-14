using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UpDownPlatform : MonoBehaviour
{
    [Min(0f)] public float vitesse = 3f;
    [Min(0f)] public float attente = 2f;
    [Min(0f)] public float distance = 3f;

    private Rigidbody2D corps;
    private Vector2 positionHaute;
    private Vector2 positionBasse;
    private bool descend = true;
    private float attenteRestante;

    private void Awake()
    {
        corps = GetComponent<Rigidbody2D>();
        corps.bodyType = RigidbodyType2D.Kinematic;
        corps.interpolation = RigidbodyInterpolation2D.Interpolate;
        positionHaute = corps.position;
        positionBasse = positionHaute + Vector2.down * distance;
    }

    private void FixedUpdate()
    {
        // Un MovePosition par pas, meme a l'arret, pour ne pas garder de vitesse residuelle.
        Vector2 prochainePosition = corps.position;
        if (attenteRestante > 0f)
            attenteRestante = Mathf.Max(0f, attenteRestante - Time.fixedDeltaTime);
        else
        {
            Vector2 cible = descend ? positionBasse : positionHaute;
            prochainePosition = Vector2.MoveTowards(corps.position, cible,
                Mathf.Max(0f, vitesse) * Time.fixedDeltaTime);
            if (prochainePosition == cible)
            {
                descend = !descend;
                attenteRestante = Mathf.Max(0f, attente);
            }
        }
        corps.MovePosition(prochainePosition);
    }

    private void OnDisable()
    {
        if (corps != null) corps.linearVelocity = Vector2.zero;
    }
}
