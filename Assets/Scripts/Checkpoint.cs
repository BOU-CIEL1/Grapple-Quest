using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("Position de reapparition par rapport au pied du panneau, au-dessus du sol.")]
    [SerializeField] private Vector3 decalageReapparition = new Vector3(0f, 1f, 0f);
    [SerializeField] private Vector3 decalageTexte = new Vector3(0f, 1.85f, 0f);
    [SerializeField, Min(0f)] private float amplitudeFlottement = 0.08f;
    [SerializeField, Min(0f)] private float vitesseFlottement = 2.5f;

    private TextMeshPro texte;
    private Vector3 PositionReapparition => transform.TransformPoint(decalageReapparition);

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        // Garde l'indication visible apres une mort et un rechargement du niveau.
        Player joueur = Object.FindFirstObjectByType<Player>();
        if (joueur != null && joueur.EstCheckpointActif(PositionReapparition))
            AfficherTexte();
    }

    private void OnTriggerEnter2D(Collider2D other) => Toucher(other);
    private void OnTriggerStay2D(Collider2D other) => Toucher(other);

    private void Toucher(Collider2D other)
    {
        Player joueur = other.GetComponentInParent<Player>();
        if (joueur != null && joueur.ActiverCheckpoint(PositionReapparition))
            AfficherTexte();
    }

    private void AfficherTexte()
    {
        if (texte != null) return;
        GameObject objetTexte = new GameObject("Checkpoint !", typeof(TextMeshPro));
        objetTexte.transform.SetParent(transform, false);
        objetTexte.transform.localPosition = decalageTexte;
        texte = objetTexte.GetComponent<TextMeshPro>();
        texte.text = "Checkpoint !";
        texte.fontSize = 4.5f;
        texte.alignment = TextAlignmentOptions.Center;
        texte.textWrappingMode = TextWrappingModes.NoWrap;
        texte.rectTransform.sizeDelta = new Vector2(4f, 1f);
        texte.color = new Color(1f, 0.88f, 0.3f);
        texte.outlineColor = new Color32(45, 30, 15, 255);
        texte.outlineWidth = 0.18f;
        SpriteRenderer panneau = GetComponent<SpriteRenderer>();
        if (panneau != null) texte.renderer.sortingLayerID = panneau.sortingLayerID;
        texte.renderer.sortingOrder = panneau != null ? panneau.sortingOrder + 100 : 100;
    }

    private void Update()
    {
        if (texte == null) return;
        texte.transform.localPosition = decalageTexte + Vector3.up
            * (Mathf.Sin(Time.time * vitesseFlottement) * amplitudeFlottement);
    }
}
