using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class Teleport : MonoBehaviour
{
    private bool teleportationEnCours;

    private void Awake()
    {
        // Le portail laisse passer le joueur et detecte son entree.
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (teleportationEnCours || other.GetComponentInParent<Mouvements>() == null)
            return;

        teleportationEnCours = true;
        SceneManager.LoadScene("BossScene");
    }
}
