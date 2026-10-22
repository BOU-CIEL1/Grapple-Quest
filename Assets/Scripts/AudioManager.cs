using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioClip[] playlist;
    public AudioSource audioSource;
    private float musicTime = 0f;
    public AudioMixerGroup sfxMixer;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += MusicOnSceneLoad;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= MusicOnSceneLoad;
    }

    private void MusicOnSceneLoad(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            audioSource.clip = playlist[0];
            audioSource.Play();
        }
        else if (scene.name == "Level1")
        {
            audioSource.clip = playlist[1];
            audioSource.Play();
            
        }
        else if (scene.name == "BossScene")
        {
            audioSource.clip = playlist[2];
            audioSource.Play();

        }
        else if (scene.name == "Credits")
        {
            audioSource.clip = playlist[3];
            audioSource.Play();
        }
    }

    public void PauseMusic()
    {
        musicTime = audioSource.time;
        audioSource.Stop();
    }

    public void ResumeMusic()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "Level1")
        {
            audioSource.clip = playlist[1];
        }
        else if (currentScene == "BossScene")
        {
            audioSource.clip = playlist[2];
        }

        audioSource.Play();
        audioSource.time = musicTime;
    }

    public void PlayPauseMenuMusic()
    {
        audioSource.clip = playlist[4];
        audioSource.Play();
    }

    public AudioSource PlayClipAt(AudioClip clip, Vector3 pos)
    {
        GameObject tempGameObject = new GameObject("tempAudio");
        tempGameObject.transform.position = pos;
        AudioSource audioSource = tempGameObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.outputAudioMixerGroup = sfxMixer;
        audioSource.Play();
        Destroy(tempGameObject, clip.length);
        return audioSource;
    }

}
