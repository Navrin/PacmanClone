
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SFXCollection", menuName = "ScriptableObjects/SFXCollection")]
public class PacSFXCollection : ScriptableObject
{
    public AudioClip move;
    public AudioClip collide;
    public AudioClip eat;
    public AudioClip death;
    public AudioClip powerup;

    public AudioMixerGroup moveSoundGroup;
    public AudioMixerGroup sfxSoundGroup;
}