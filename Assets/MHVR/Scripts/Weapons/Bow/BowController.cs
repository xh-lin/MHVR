using Tilia.Interactions.Interactables.Interactables;
using Tilia.Interactions.Interactables.Interactors;
using Tilia.Interactions.SnapZone;
using UnityEngine;
using Zinnia.Extension;
using Zinnia.Tracking.Collision;

public class BowController : MonoBehaviour
{
    #region Bow Settings
    [Header("Bow Settings")]
    [Tooltip("The actual Interactable bow.")]
    [SerializeField]
    private InteractableFacade bowInteractable;

    [Tooltip("The game object containing colliders associated with the bow.")]
    [SerializeField]
    private GameObject bowColliderContainer;

    [Tooltip("The snap zone used for nocking the arrow to the bow.")]
    [SerializeField]
    private SnapZoneFacade arrowSnapZone;

    [SerializeField]
    [Tooltip("The animator associated with the bow.")]
    private Animator bowAnimator;

    [SerializeField]
    [Tooltip("The animation parameter name in the animator for controlling fold animation.")]
    private string foldAnimationParameterName = "isFolded";

    [SerializeField]
    [Tooltip("The animation parameter name in the animator for controlling pull animation.")]
    private string pullAnimationParameterName = "pullBlend";
    #endregion

    #region String Settings
    [Header("String Settings")]
    [Tooltip("The nocking point on the string.")]
    [SerializeField]
    private GameObject nockingPoint;

    [Tooltip("The speed in which the string returns to the idle position.")]
    [SerializeField]
    private float stringReturnSpeed = 150f;

    [Tooltip("The maximum length the string can be pulled back.")]
    [SerializeField]
    private float maxStringPull = 1f;

    [Tooltip("The power produced to propel the arrow forward by the string when fully pulled back.")]
    [SerializeField]
    private float stringPower = 50f;

    [Tooltip("Name of the game object for nocking collision detection.")]
    [SerializeField]
    private string arrowNockName = "ArrowNock";
    #endregion

    private bool isStringGrabbed;
    private float stringRestingDistance;
    private float normalizedPullDistance;
    private Collider[] bowColliders;
    private InteractorFacade secondaryInteractor;
    private ArrowController nockedArrow;
    private InteractorFacade arrowInteractor;

    private bool IsFolded
    {
        get { return bowAnimator.GetBool(foldAnimationParameterName); }
        set { bowAnimator.SetBool(foldAnimationParameterName, value); }
    }

    public void ToggleFold()
    {
        if (IsFolded)
        {
            IsFolded = false;
            arrowSnapZone.gameObject.SetActive(true);
        }
        else
        {
            if (normalizedPullDistance == 0f && !isStringGrabbed && arrowSnapZone.SnappedGameObject == null)
            {
                IsFolded = true;
                arrowSnapZone.gameObject.SetActive(false);
            }
        }
    }

    public void GrabBow()
    {
        if (!IsFolded)
        {
            arrowSnapZone.gameObject.SetActive(true);
        }
    }

    public void GrabString()
    {
        if (!IsFolded)
        {
            isStringGrabbed = true;
        }
    }

    public void UngrabString()
    {
        if (isStringGrabbed)
        {
            AttemptFireArrow();
        }

        isStringGrabbed = false;
    }

    public void AttemptArrowNock(CollisionNotifier.EventData data)
    {
        if (IsFolded
            || arrowSnapZone.SnappedGameObject != null
            || !data.ColliderData.name.Equals(arrowNockName))
        {
            return;
        }

        InteractableFacade arrow = data.ColliderData.GetAttachedRigidbody().GetComponent<InteractableFacade>();
        if (arrow == null)
        {
            return;
        }

        arrowInteractor = arrow.GrabbingInteractors.Count > 0 ? arrow.GrabbingInteractors[0] : null;
        if (arrowInteractor == null)
        {
            return;
        }

        arrowInteractor.Ungrab();
        arrowInteractor.SimulateUntouch(arrow);
        arrowInteractor.Grab(bowInteractable);
        arrowSnapZone.Snap(arrow);

        nockedArrow = arrow.GetComponentInChildren<ArrowController>();
        ToggleColliders(bowColliders, nockedArrow.arrowColliders, true);
        nockedArrow.ToggleColliderTrigger(true);
    }

    public void AttemptFireArrow()
    {
        if (arrowInteractor != null
            && nockedArrow != null
            && nockedArrow.arrowInteractable.gameObject.Equals(arrowSnapZone.SnappedGameObject))
        {
            arrowSnapZone.Unsnap();
            bowInteractable.Ungrab(1);
            arrowInteractor.SimulateUntouch(bowInteractable);
            nockedArrow.Fire(normalizedPullDistance * stringPower);
        }
        else if (arrowSnapZone.SnappedGameObject != null)
        {
            nockedArrow = arrowSnapZone.SnappedGameObject.GetComponentInChildren<ArrowController>();
            if (nockedArrow != null)
            {
                ToggleColliders(bowColliders, nockedArrow.arrowColliders, true);
                nockedArrow.ToggleColliderTrigger(true);

                arrowSnapZone.Unsnap();
                bowInteractable.Ungrab(1);
                secondaryInteractor = bowInteractable.GrabbingInteractors.Count > 1 ? bowInteractable.GrabbingInteractors[1] : null;
                if (secondaryInteractor != null)
                {
                    secondaryInteractor.SimulateUntouch(bowInteractable);
                }
                nockedArrow.Fire(normalizedPullDistance * stringPower);
            }
        }
    }

    private void OnEnable()
    {
        arrowSnapZone.gameObject.SetActive(false);
        stringRestingDistance = nockingPoint.transform.localPosition.z;
        bowColliders = bowColliderContainer.GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (isStringGrabbed)
        {
            secondaryInteractor = bowInteractable.GrabbingInteractors.Count > 1 ? bowInteractable.GrabbingInteractors[1] : null;
            if (secondaryInteractor != null)
            {
                float pullDistance = bowInteractable.transform.InverseTransformPoint(secondaryInteractor.GrabAttachPoint.transform.position).z - stringRestingDistance;
                normalizedPullDistance = Mathf.InverseLerp(0f, maxStringPull, -pullDistance);
                bowAnimator.SetFloat(pullAnimationParameterName, normalizedPullDistance);
            }
        }
        else
        {
            if (normalizedPullDistance > 0f)
            {
                normalizedPullDistance = Mathf.Clamp01(normalizedPullDistance - stringReturnSpeed * Time.deltaTime);
                bowAnimator.SetFloat(pullAnimationParameterName, normalizedPullDistance);
            }
        }
    }

    private void ToggleColliders(Collider[] sources, Collider[] targets, bool ignore)
    {
        foreach (Collider source in sources)
        {
            foreach (Collider target in targets)
            {
                Physics.IgnoreCollision(source, target, ignore);
            }
        }
    }
}