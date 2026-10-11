using UnityEngine;

// A placer uniquement sur l'anneau, pas sur la chaine ni la plateforme.
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class GrappleAnchor : MonoBehaviour
{
    [SerializeField] private Transform pointAccroche;
    public Vector2 Position => pointAccroche != null ? (Vector2)pointAccroche.position : transform.position;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Position, 0.15f);
    }
}
