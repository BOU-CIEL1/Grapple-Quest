using UnityEngine;

public class PassDialogue : MonoBehaviour
{

    [SerializeField] private GameObject dialogueUI;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PasserDialogue()
    {
        dialogueUI.SetActive(false);
    }

}
