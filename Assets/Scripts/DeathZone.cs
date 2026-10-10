using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class DeathZone : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other) => Toucher(other);
    private void OnTriggerStay2D(Collider2D other) => Toucher(other);

    private void Toucher(Collider2D other)
    {
        Player joueur = other.GetComponentInParent<Player>();
        if (joueur != null) joueur.Tuer();
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D zone = GetComponent<BoxCollider2D>();
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(zone.offset, zone.size);
    }
}
