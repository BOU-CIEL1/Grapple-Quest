using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CollectCoin : MonoBehaviour
{
    // Empêche de compter plusieurs fois la même pièce.
    private bool collected;

    private void Awake()
    {
        // Détecte le contact sans bloquer le passage du joueur.
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    // Appelé lorsqu'un collider entre dans celui de la pièce.
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        // Cherche le compteur sur le joueur ou le parent du collider touché.
        PlayerCoins player = other.GetComponentInParent<PlayerCoins>();
        if (player == null)
            return;

        // Ajoute une pièce au compteur du joueur.
        collected = true;
        player.AddCoin();
        // Cache immédiatement la pièce, puis supprime son objet.
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
