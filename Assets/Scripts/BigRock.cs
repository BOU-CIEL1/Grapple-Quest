using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BigRock : MonoBehaviour
{
    [Header("Resistance au grappin")]
    [SerializeField, Min(1)] private int coupsPourDetruire = 3;
    [SerializeField, Min(0)] private int coupsRecus;
    public int CoupsRecus => coupsRecus;
    public bool EstDetruit => coupsRecus >= coupsPourDetruire;

    [Header("Tremblement visuel")]
    [SerializeField] private Transform visuel;
    [SerializeField, Min(0f)] private float amplitude = 0.035f;
    [SerializeField, Min(0.01f)] private float dureeTremblement = 0.18f;
    [SerializeField, Min(0f)] private float frequence = 24f;
    private Vector3 positionRepos;
    private float tempsRestant;

    private void Awake()
    {
        // Le rocher reste a sa position de scene, sans chute ni rotation physique.
        Rigidbody2D corps = GetComponent<Rigidbody2D>();
        corps.bodyType = RigidbodyType2D.Static;
        if (visuel != null && visuel != transform)
            positionRepos = visuel.localPosition;
        else
            visuel = null;
    }

    // Chaque lancer compte comme un coup, independamment des degats des ennemis.
    public void RecevoirCoupGrappin()
    {
        if (EstDetruit) return;
        coupsRecus++;
        if (EstDetruit)
        {
            foreach (Collider2D collision in GetComponentsInChildren<Collider2D>())
                collision.enabled = false;
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        tempsRestant = dureeTremblement;
        ActualiserTremblement();
    }

    private void Update()
    {
        if (tempsRestant <= 0f) return;
        tempsRestant = Mathf.Max(0f, tempsRestant - Time.deltaTime);
        ActualiserTremblement();
    }

    private void ActualiserTremblement()
    {
        if (visuel == null) return;
        if (tempsRestant <= 0f)
        {
            visuel.localPosition = positionRepos;
            return;
        }
        float progression = (dureeTremblement - tempsRestant) / dureeTremblement;
        float phase = progression * dureeTremblement * frequence * Mathf.PI * 2f;
        Vector3 decalage = new Vector3(Mathf.Sin(phase), Mathf.Cos(phase) * 0.35f, 0f)
            * amplitude * (1f - progression);
        // Amplitude en unites monde, meme si le prefab est agrandi ou retourne.
        Vector3 decalageLocal = visuel.parent != null
            ? visuel.parent.InverseTransformVector(decalage) : decalage;
        visuel.localPosition = positionRepos + decalageLocal;
    }

    private void OnDisable()
    {
        tempsRestant = 0f;
        if (visuel != null) visuel.localPosition = positionRepos;
    }
}
