/*
 *  It controls bow animatoin, audio and effect.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRTK;



public class Bow : MonoBehaviour
{
    [Range(0f, 1f)]
    public float touchVibration = 0.2f;
    [Tooltip("In seconds.")]
    public float vibrationDuration = 0.2f;
    public SoundBank weaponSFX;
    public SoundBank bowPhysicalSFX;
    public SoundBank bowVisualSFX;

    private BowAim aim;
    private AudioSource source;
    private Animator animator;
    private Rigidbody rigidBody;
    private Renderer[] bowRenderers;
    private VRTK_InteractableObject interact;
    private Outline outline;

    private int[] shotSounds;
    private int[] setArrowSounds;
    private int[] PullOneSounds;
    private int shotSoundsIdx;
    private int setArrowSoundsIdx;
    private int PullOneSoundsIdx;

    private void Awake()
    {
        aim = GetComponent<BowAim>();
        source = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        rigidBody = GetComponent<Rigidbody>();
        bowRenderers = GetComponentsInChildren<Renderer>();
        interact = GetComponent<VRTK_InteractableObject>();
        outline = GetComponent<Outline>();

        // set to folded animation
        animator.enabled = true;    
        rigidBody.isKinematic = true;
        outline.enabled = false;

        // initialize audio variables
        shotSounds = new int[] { 2, 4 };
        setArrowSounds = new int[] { 11, 12, 21 };
        PullOneSounds = new int[] { 3, 24 };
        shotSoundsIdx = 0;
        setArrowSoundsIdx = 0;
        PullOneSoundsIdx = 0;
    }

    private void Start()
    {
        animator.enabled = false;
        rigidBody.isKinematic = false;

        interact.InteractableObjectTouched += Touch;
        interact.InteractableObjectUntouched += Untouch;
        interact.InteractableObjectGrabbed += Grab;
        interact.InteractableObjectUngrabbed += Ungrab;
    }

    public bool IsFolded()
    {
        return animator.GetBool("isFolded");
    }

    public void SetPullAnimation(float blend)
    {
        animator.SetFloat("PullBlend", blend);
    }

    // === Glow

    public void GlowPulse(float minMult, float maxMult, float interpolation, bool breathAfter)
    {
        StartCoroutine(GlowPulseCoroutine(minMult, maxMult, interpolation, breathAfter));
    }

    private IEnumerator GlowPulseCoroutine(float minMult, float maxMult, float interpolation, bool breathAfter)
    {
        float lerpVal = 0;
        while (lerpVal < 1)
        {
            lerpVal += interpolation * Time.deltaTime;
            GlowSetMuliplier(Mathf.Lerp(minMult, maxMult, lerpVal));
            yield return null;
        }
        while (lerpVal > 0)
        {
            lerpVal -= interpolation * Time.deltaTime;
            GlowSetMuliplier(Mathf.Lerp(minMult, maxMult, lerpVal));
            yield return null;
        }
        GlowBreath(breathAfter);
    }

    public void GlowSetMuliplier(float mult)
    {
        foreach (var renderer in bowRenderers)
        {
            renderer.material.SetFloat("_glowColorMultiplier", mult);
        }
    }

    public void GlowBreath(bool b)
    {
        foreach (var renderer in bowRenderers)
        {
            renderer.material.SetInt("_isBreath", b ? 1 : 0);
        }
    }

    public void StopGlow()
    {
        StopAllCoroutines();
        GlowSetMuliplier(0);
        GlowBreath(false);
    }

    // === Play sound (Called by other scripts or animation events)

    public void PlayStringStretchSound(float volumeScale)
    {
        source.PlayOneShot(weaponSFX.audio[1].clip, volumeScale);
    }

    public void PlaySheathSound(float volumeScale)
    {
        source.PlayOneShot(weaponSFX.audio[3].clip, volumeScale);
    }

    public void PlayFoldSound(float volumeScale)
    {
        source.PlayOneShot(weaponSFX.audio[5].clip, volumeScale);
    }

    public void PlayOpenSound(float volumeScale)
    {
        source.PlayOneShot(weaponSFX.audio[6].clip, volumeScale);
    }

    public void PlayShotSound(float volumeScale)
    {
        source.PlayOneShot(weaponSFX.audio[shotSounds[shotSoundsIdx]].clip, volumeScale);
        shotSoundsIdx = (shotSoundsIdx + 1) % shotSounds.Length;
    }

    public void PlaySetArrowSound(float volumeScale)
    {
        source.PlayOneShot(bowPhysicalSFX.audio[setArrowSounds[setArrowSoundsIdx]].clip, volumeScale);
        setArrowSoundsIdx = (setArrowSoundsIdx + 1) % setArrowSounds.Length;
    }

    public void PlayPullSound(int chargeLevel, float volumePhysicalSFX, float volumeVisualSFX)
    {
        switch (chargeLevel)
        {
            case 1:
                source.PlayOneShot(bowPhysicalSFX.audio[PullOneSounds[PullOneSoundsIdx]].clip, volumePhysicalSFX);
                PullOneSoundsIdx = (PullOneSoundsIdx + 1) % PullOneSounds.Length;
                break;
            case 2:
                source.PlayOneShot(bowPhysicalSFX.audio[18].clip, volumePhysicalSFX);
                source.PlayOneShot(bowVisualSFX.audio[51].clip, volumeVisualSFX);
                break;
            case 3:
            default:
                source.PlayOneShot(bowVisualSFX.audio[42].clip, volumeVisualSFX);
                break;
        }
        PlayPullHoldSound(volumePhysicalSFX);
    }

    public void PlayPullHoldSound(float volumeScale)
    {
        source.clip = bowPhysicalSFX.audio[22].clip;
        source.loop = true;
        source.volume = volumeScale;
        source.Play();
    }

    public void StopSound()
    {
        if (source.isPlaying)
        {
            source.Stop();
        }
    }

    public void ToggleFold()
    {
        if (IsFolded())
        {
            animator.SetBool("isFolded", false);    // Open
        }
        else if (!aim.HasArrow())
        {
            animator.SetBool("isFolded", true);     // Fold
        }
    }

    // === VRTK_InteractableObject event callbacks.

    private IEnumerator HapticPulse(float intensity, float duration, bool isRHand)
    {
        if (isRHand)
        {
            OVRInput.SetControllerVibration(.1f, intensity, OVRInput.Controller.RTouch);
        }
        else
        {
            OVRInput.SetControllerVibration(.1f, intensity, OVRInput.Controller.LTouch);
        }

        yield return new WaitForSeconds(duration);

        if (isRHand)
        {
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        }
        else
        {
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
        }
    }

    protected virtual void Touch(object sender, InteractableObjectEventArgs e)
    {
        if (interact.IsInSnapDropZone())
        {
            outline.enabled = true;
            foreach (GameObject goTouching in interact.GetTouchingObjects())
            {
                if (VRTK_DeviceFinder.IsControllerRightHand(goTouching))
                {
                    StartCoroutine(HapticPulse(touchVibration, vibrationDuration, true));
                }
                else if (VRTK_DeviceFinder.IsControllerLeftHand(goTouching))
                {
                    StartCoroutine(HapticPulse(touchVibration, vibrationDuration, false));
                }
            }
        }
    }

    protected virtual void Untouch(object sender, InteractableObjectEventArgs e)
    {
        outline.enabled = false;
    }

    protected virtual void Grab(object sender, InteractableObjectEventArgs e)
    {
        outline.enabled = false;
        animator.enabled = true;
    }

    protected virtual void Ungrab(object sender, InteractableObjectEventArgs e)
    {
        SetPullAnimation(0f);
        animator.enabled = false;
    }

    // ===
}
