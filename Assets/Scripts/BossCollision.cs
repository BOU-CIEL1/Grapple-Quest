using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BossCollision : MonoBehaviour
{
    private BoxCollider2D collisionSol;


    private void Awake()
    {
        collisionSol = GetComponent<BoxCollider2D>();
        // Une collision solide permet au sol de soutenir le boss.
        collisionSol.isTrigger = false;

        Rigidbody2D corps = GetComponent<Rigidbody2D>();
        corps.bodyType = RigidbodyType2D.Dynamic;
        corps.simulated = true;
        if (corps.gravityScale <= 0f)
            corps.gravityScale = 1f;
        corps.constraints |= RigidbodyConstraints2D.FreezeRotation;
        corps.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        corps.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

}
