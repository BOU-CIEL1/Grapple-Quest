using System.Collections;
using UnityEngine;

public class UpDownPlatform : MonoBehaviour
{
    public float vitesse = 3f;
    public float attente = 2f;
    public float distance = 3f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(WaitUpDown());
    }

    // Update is called once per frame
    void Update()
    {

        // Faire monter et descendre les plateformes en continu
        // La faire descendre pendant une durée précise
        
        // La faire s'arrêter une durée précise
        // La faire remonter une durée précise
        
    }

    IEnumerator WaitUpDown()
    {
        while (true)
        {
            float tempsEcoule = 0f;
            float tempsTrajet = distance / vitesse;

            while (tempsEcoule < tempsTrajet)
            {
                transform.Translate(Vector2.down * Time.deltaTime * vitesse);
                tempsEcoule += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(attente);

            tempsEcoule = 0f;
            while(tempsEcoule < tempsTrajet)
            {
                transform.Translate(Vector2.up * Time.deltaTime * vitesse);
                tempsEcoule += Time.deltaTime;
                yield return null;
            }

            yield return new WaitForSeconds(attente);

        }
    }
}
