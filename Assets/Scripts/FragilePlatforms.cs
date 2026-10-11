using UnityEngine;
using System.Collections;

public class FragilePlatforms : MonoBehaviour
{
    private float vitesse = 5f;
    private float chute = 5f;
    private float attente = 10f;
    private float delai_entree = 1f;

    private SpriteRenderer spriteRenderer;
    private Collider2D platformCollider;
    private Vector2 positionInitiale;
    private bool Falling = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        platformCollider = GetComponent<Collider2D>();
        positionInitiale = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        // Quand le joueur va sur la plateforme, la plateforme tombe
        // La plateforme réapparait au bout de 10 secondes
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "Player" && !Falling)
        {
            StartCoroutine(WaitFall());
        }
    }

    IEnumerator WaitFall()
    {
        yield return new WaitForSeconds(delai_entree);
        
        Falling = true;
        float tempsEcoule = 0f;
        float tempsChute = chute / vitesse;

        while (tempsEcoule < tempsChute)
        {
            transform.Translate(Vector2.down * Time.deltaTime * vitesse);
            tempsEcoule += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.enabled = false;
        platformCollider.enabled = false;

        yield return new WaitForSeconds(attente);

        transform.position = positionInitiale;
        spriteRenderer.enabled = true;
        platformCollider.enabled = true;
        Falling = false;
    }
}
