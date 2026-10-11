using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class CollectDiamond : MonoBehaviour
{
    private bool collected;

    private void Awake()
    {
        // Le diamant detecte le joueur sans servir de plateforme.
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        PlayerCoins inventory = other.GetComponentInParent<PlayerCoins>();
        if (inventory == null) return;
        Player player = inventory.GetComponent<Player>();
        if (player != null && player.EstMort) return;

        // Protege aussi les contacts simultanes de plusieurs colliders du joueur.
        collected = true;
        inventory.AddDiamond();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
