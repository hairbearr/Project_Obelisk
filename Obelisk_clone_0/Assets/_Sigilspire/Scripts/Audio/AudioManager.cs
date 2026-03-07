using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Volume Controls")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;

    [Header("Player Sounds")]
    [SerializeField] private AudioClip[] swordSwingSounds;
    [SerializeField] private AudioClip[] swordHitSounds;
    [SerializeField] private AudioClip[] playerHurtSounds;
    [SerializeField] private AudioClip[] playerDeathSounds;
    [SerializeField] private AudioClip[] grappleFireSounds;
    [SerializeField] private AudioClip[] grapplePullSounds;
    [SerializeField] private AudioClip[] shieldBlockSounds;

    [Header("Enemy Sounds")]
    [SerializeField] private AudioClip[] enemyMeleeAttackSounds;
    [SerializeField] private AudioClip[] enemyHitSounds;
    [SerializeField] private AudioClip[] enemyDeathSounds;
    [SerializeField] private AudioClip[] archerArrowFireSounds;
    [SerializeField] private AudioClip[] arrowImpactWallSounds;
    [SerializeField] private AudioClip[] arrowImpactFleshSounds;
    [SerializeField] private AudioClip[] mageFireballFireSounds;
    [SerializeField] private AudioClip[] mageFireballHitSounds;

    [Header("Boss Sounds")]
    [SerializeField] private AudioClip[] groundPoundSounds; // Sequential for one ability
    [SerializeField] private AudioClip[] stoneFistSounds;
    [SerializeField] private AudioClip[] runeBarrageSounds;
    [SerializeField] private AudioClip[] chargeWindupSounds;
    [SerializeField] private AudioClip[] chargeImpactSounds;
    [SerializeField] private AudioClip[] gargoyleSummonSounds;

    [Header("UI Sounds")]
    [SerializeField] private AudioClip[] menuHoverSounds;
    [SerializeField] private AudioClip[] menuClickSounds;
    [SerializeField] private AudioClip[] victorySounds;
    [SerializeField] private AudioClip[] defeatSounds;

    [Header("Music")]
    [SerializeField] private AudioClip gameplayMusic;
    [SerializeField] private AudioClip bossFightMusic;
    [SerializeField] private AudioClip victoryJingle;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Create audio sources if not assigned
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        // Auto-start gameplay music when scene loads
        PlayGameplayMusic();
    }

    private void Update()
    {
        if(musicSource != null && musicSource.isPlaying)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    // ==================== PLAYER SOUNDS ====================
    public void PlaySwordSwing(Vector3 position) => PlayRandomSound(swordSwingSounds, position);
    public void PlaySwordHit(Vector3 position) => PlayRandomSound(swordHitSounds, position);
    public void PlayPlayerHurt(Vector3 position) => PlayRandomSound(playerHurtSounds, position);
    public void PlayPlayerDeath(Vector3 position) => PlayRandomSound(playerDeathSounds, position);
    public void PlayGrappleFire(Vector3 position) => PlayRandomSound(grappleFireSounds, position);
    public void PlayGrapplePull(Vector3 position) => PlayRandomSound(grapplePullSounds, position);
    public void PlayShieldBlock(Vector3 position) => PlayRandomSound(shieldBlockSounds, position);

    // ==================== ENEMY SOUNDS ====================
    public void PlayEnemyMeleeAttack(Vector3 position) => PlayRandomSound(enemyMeleeAttackSounds, position);
    public void PlayEnemyHit(Vector3 position) => PlayRandomSound(enemyHitSounds, position);
    public void PlayEnemyDeath(Vector3 position) => PlayRandomSound(enemyDeathSounds, position);
    public void PlayArcherArrowFire(Vector3 position) => PlayRandomSound(archerArrowFireSounds, position);
    public void PlayArrowImpactWall(Vector3 position) => PlayRandomSound(arrowImpactWallSounds, position);
    public void PlayArrowImpactFlesh(Vector3 position) => PlayRandomSound(arrowImpactFleshSounds, position);
    public void PlayMageFireballFire(Vector3 position) => PlayRandomSound(mageFireballFireSounds, position);
    public void PlayMageFireballHit(Vector3 position) => PlayRandomSound(mageFireballHitSounds, position);

    // ==================== BOSS SOUNDS ====================
    public void PlayGroundPound(Vector3 position, MonoBehaviour caller)
    {
        // Special: Play multiple sounds in sequence
        if (groundPoundSounds.Length > 0)
        {
            caller.StartCoroutine(PlaySequentialSounds(groundPoundSounds, position, 0.15f));
        }
    }

    public void PlayStoneFist(Vector3 position) => PlayRandomSound(stoneFistSounds, position);
    public void PlayRuneBarrage(Vector3 position) => PlayRandomSound(runeBarrageSounds, position);
    public void PlayChargeWindup(Vector3 position) => PlayRandomSound(chargeWindupSounds, position);
    public void PlayChargeImpact(Vector3 position) => PlayRandomSound(chargeImpactSounds, position);
    public void PlayGargoyleSummon(Vector3 position) => PlayRandomSound(gargoyleSummonSounds, position);

    // ==================== UI SOUNDS ====================
    public void PlayMenuHover()
    {
        if (menuHoverSounds == null || menuHoverSounds.Length == 0) return;

        AudioClip clip = menuHoverSounds[Random.Range(0, menuHoverSounds.Length)];
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    public void PlayMenuClick()
    {
        if (menuClickSounds == null || menuClickSounds.Length == 0) return;

        AudioClip clip = menuClickSounds[Random.Range(0, menuClickSounds.Length)];
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }
    public void PlayVictory()
    {
        if (victorySounds == null || victorySounds.Length == 0) return;

        AudioClip clip = victorySounds[Random.Range(0, victorySounds.Length)];
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    public void PlayDefeat()
    {
        if (defeatSounds == null || defeatSounds.Length == 0) return;

        AudioClip clip = defeatSounds[Random.Range(0, defeatSounds.Length)];
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }

    // ==================== MUSIC ====================
    public void PlayGameplayMusic()
    {
        if (gameplayMusic != null)
        {
            musicSource.clip = gameplayMusic;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    public void PlayBossFightMusic()
    {
        if (bossFightMusic != null)
        {
            musicSource.clip = bossFightMusic;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    public void PlayVictoryJingle()
    {
        if (victoryJingle != null)
        {
            musicSource.Stop();
            musicSource.loop = false;
            musicSource.clip = victoryJingle;
            musicSource.volume = musicVolume * masterVolume;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    // ==================== HELPER METHODS ====================
    private void PlayRandomSound(AudioClip[] clips, Vector3 position)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
        }
    }

    private IEnumerator PlaySequentialSounds(AudioClip[] clips, Vector3 position, float delay)
    {
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                AudioSource.PlayClipAtPoint(clip, position, sfxVolume * masterVolume);
                yield return new WaitForSeconds(delay);
            }
        }
    }

    // Optional: Play sound without position (2D sound)
    public void PlaySound2D(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
        }
    }
}
