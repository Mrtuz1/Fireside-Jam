using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

[System.Serializable]
public class Sound
{
    public string name;
    public AudioClip clip;

    [HideInInspector]
    public float volume = 0.8f;
    [HideInInspector]
    public float pitch = 1f;

    public bool loop;

    [HideInInspector]
    public AudioSource source;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio List")]
    public List<Sound> sounds;

    [Header("Settings")]
    public bool dontDestroyOnLoad = true;

    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Initialize sounds as child objects
        foreach (Sound s in sounds)
        {
            GameObject child = new GameObject("Sound_" + s.name);
            child.transform.SetParent(transform);
            
            s.source = child.AddComponent<AudioSource>();
            s.source.playOnAwake = false; 
            s.source.clip = s.clip;
            s.source.Stop(); // Garantiye almak için durdur
            
            // Volume ayarları
            string sName = s.name.Trim();
            if (sName.Equals("BackgroundMusic", System.StringComparison.OrdinalIgnoreCase))
            {
                s.source.volume = 0.3f; 
            }
            else if (sName.Equals("Bell", System.StringComparison.OrdinalIgnoreCase) || 
                     sName.Equals("Angry", System.StringComparison.OrdinalIgnoreCase))
            {
                s.source.volume = 1.0f; // Duyulmayan sesler fulendi
            }
            else
            {
                s.source.volume = 0.8f;
            }

            s.source.pitch = 1f; 
            s.source.loop = s.loop;
            s.source.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Play background music automatically
        Play("BackgroundMusic");

        // Optional: Global Button Click Listener
        SetupButtonClickSounds();

        // Check for AudioListener
        if (FindObjectOfType<AudioListener>() == null)
        {
            Debug.LogError("AudioManager: No AudioListener found in the scene! Please ensure your Main Camera has an AudioListener component.");
        }

        // GameManager olayına abone ol
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSanityChanged += UpdateGlobalPitch;
            // İlk değeri uygula
            UpdateGlobalPitch(GameManager.Instance.sanity);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnSanityChanged -= UpdateGlobalPitch;
        }
    }

    private void UpdateGlobalPitch(int sanity)
    {
        // Sanity azaldıkça pitch artar (100 sanity -> 1.0 pitch, 0 sanity -> 1.5 pitch)
        float dangerLevel = Mathf.Clamp01(1f - (sanity / 100f));
        float targetPitch = 1f + (dangerLevel * 0.5f);

        foreach (Sound s in sounds)
        {
            if (s.source != null)
            {
                s.source.pitch = targetPitch;
            }
        }
    }

    /// <summary>
    /// Finds all buttons in the scene and adds the click sound.
    /// Note: This only works for buttons existing at Start. 
    /// For dynamically created buttons, call this again or add manually.
    /// </summary>
    public void SetupButtonClickSounds()
    {
        UnityEngine.UI.Button[] buttons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
        foreach (var btn in buttons)
        {
            // Only add if it's in the scene (not a prefab)
            if (btn.gameObject.scene.name != null)
            {
                btn.onClick.RemoveListener(() => PlayOneShot("Click")); // Prevent double assignment
                btn.onClick.AddListener(() => PlayOneShot("Click"));
            }
        }
    }

    /// <summary>
    /// Plays a sound by name. If the sound is already playing, it will restart (standard Play behavior).
    /// </summary>
    public void Play(string name)
    {
        Sound s = sounds.Find(sound => sound.name.Trim().Equals(name.Trim(), System.StringComparison.OrdinalIgnoreCase));
        if (s == null)
        {
            Debug.LogWarning("AudioManager: Sound not found -> " + name);
            return;
        }
        
        if (s.clip == null)
        {
            Debug.LogWarning("AudioManager: Clip is missing for sound -> " + name);
            return;
        }

        s.source.Play();
        Debug.Log("AudioManager: Playing " + name);
    }

    /// <summary>
    /// Plays a sound once. This allows overlapping sounds if called multiple times.
    /// Ideal for SFX like button clicks or hits.
    /// </summary>
    public void PlayOneShot(string name)
    {
        Sound s = sounds.Find(sound => sound.name.Trim().Equals(name.Trim(), System.StringComparison.OrdinalIgnoreCase));
        if (s == null)
        {
            Debug.LogWarning("AudioManager: Sound not found -> " + name);
            return;
        }

        if (s.clip == null)
        {
            Debug.LogWarning("AudioManager: Clip is missing for sound -> " + name);
            return;
        }

        s.source.PlayOneShot(s.clip);
    }

    /// <summary>
    /// Stops a sound by name.
    /// </summary>
    public void Stop(string name)
    {
        Sound s = sounds.Find(sound => sound.name.Trim().Equals(name.Trim(), System.StringComparison.OrdinalIgnoreCase));
        if (s == null)
        {
            Debug.LogWarning("AudioManager: Sound not found -> " + name);
            return;
        }
        s.source.Stop();
    }

    /// <summary>
    /// Checks if a sound is currently playing.
    /// </summary>
    public bool IsPlaying(string name)
    {
        Sound s = sounds.Find(sound => sound.name.Trim().Equals(name.Trim(), System.StringComparison.OrdinalIgnoreCase));
        if (s != null)
        {
            return s.source.isPlaying;
        }
        return false;
    }

    /// <summary>
    /// Adjusts the volume of a specific sound at runtime.
    /// </summary>
    public void SetVolume(string name, float volume)
    {
        Sound s = sounds.Find(sound => sound.name.Trim().Equals(name.Trim(), System.StringComparison.OrdinalIgnoreCase));
        if (s != null)
        {
            s.source.volume = volume;
        }
    }
}
