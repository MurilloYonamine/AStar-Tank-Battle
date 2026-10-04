using UnityEngine;

/// <summary>Cosmetic animation only: the tank hull and A* own all gameplay movement.</summary>
public sealed class TankCharacterAnimation : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField, Min(0f)] private float deathDuration = 1.8f;
    private Vector3 anchorPosition;
    private Vector3 originalAnchorPosition;
    private Quaternion anchorRotation;
    private bool dead;
    private bool dancing;

    public Animator Animator => characterAnimator;
    public float DeathDuration => Ready ? deathDuration : 0f;
    private bool Ready => characterAnimator != null && characterAnimator.isActiveAndEnabled &&
                          characterAnimator.runtimeAnimatorController != null;

    private void Awake()
    {
        if (characterAnimator == null) characterAnimator = GetComponentInChildren<Animator>(true);
        if (characterAnimator == null) return;
        anchorPosition = characterAnimator.transform.localPosition;
        originalAnchorPosition = anchorPosition;
        anchorRotation = characterAnimator.transform.localRotation;
        characterAnimator.applyRootMotion = false;
        characterAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        characterAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    private void LateUpdate()
    {
        // Lock only the model root, not the bones: punches, dance and death stay expressive.
        if (characterAnimator != null)
            characterAnimator.transform.SetLocalPositionAndRotation(anchorPosition, anchorRotation);
    }

    public void SetVisualHeightOffset(float worldHeight)
    {
        if (characterAnimator == null) return;
        Transform parent = characterAnimator.transform.parent;
        anchorPosition = originalAnchorPosition + (parent != null
            ? parent.InverseTransformVector(Vector3.up * worldHeight)
            : Vector3.up * worldHeight);
        characterAnimator.transform.localPosition = anchorPosition;
    }

    public void PlayIdle()
    {
        dead = dancing = false;
        if (!Ready) return;
        characterAnimator.ResetTrigger("Attack");
        characterAnimator.ResetTrigger("Hit");
        characterAnimator.SetBool("Dead", false);
        characterAnimator.SetBool("Dancing", false);
        characterAnimator.Play("Base Layer.Idle", 0, 0f);
    }

    public void PlayAttack()
    {
        if (!Ready || dead || dancing) return;
        characterAnimator.ResetTrigger("Hit");
        characterAnimator.SetTrigger("Attack");
    }

    public void PlayHit()
    {
        if (!Ready || dead || dancing) return;
        characterAnimator.ResetTrigger("Attack");
        characterAnimator.SetTrigger("Hit");
    }

    public float PlayDeath()
    {
        dead = true;
        dancing = false;
        if (!Ready) return 0f;
        characterAnimator.ResetTrigger("Attack");
        characterAnimator.ResetTrigger("Hit");
        characterAnimator.SetBool("Dancing", false);
        characterAnimator.SetBool("Dead", true);
        return deathDuration;
    }

    public void PlayDance()
    {
        PlayIdle();
        dancing = true;
        if (Ready) characterAnimator.SetBool("Dancing", true);
    }
}
