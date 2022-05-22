using Tilia.Interactions.Interactables.Interactables;
using UnityEngine;
using Zinnia.Tracking.Collision;

public class ArrowController : MonoBehaviour
{
    [Tooltip("The actual Interactable arrow.")]
    public InteractableFacade arrowInteractable;

    [Tooltip("The game object containing colliders associated with the arrow.")]
    [SerializeField]
    private GameObject arrowColliderContainer;

    [Tooltip("The amount of time to pass before destroying the arrow after being fired.")]
    [SerializeField]
    private float timeTillDestroy = 20f;

    [HideInInspector]
    public Collider[] arrowColliders;

    private bool inFlight;

    public void CheckHit(CollisionNotifier.EventData data)
    {
        int ignoreLayer = LayerMask.GetMask("Ignore Raycast");
        if (!inFlight
            || data.ColliderData.attachedRigidbody == arrowInteractable.InteractableRigidbody
            || ignoreLayer == (ignoreLayer | (1 << data.ColliderData.gameObject.layer)))
        {
            return;
        }

        inFlight = false;
        arrowInteractable.gameObject.SetActive(false);
        Destroy(arrowInteractable.gameObject, 2f);
    }

    public void Fire(float force)
    {
        arrowInteractable.InteractableRigidbody.isKinematic = false;
        arrowInteractable.InteractableRigidbody.velocity = arrowInteractable.transform.forward * force;
        inFlight = true;
        Destroy(arrowInteractable.gameObject, timeTillDestroy);
    }

    public void ToggleColliderTrigger(bool isTrigger)
    {
        for (int i = 0; i < arrowColliders.Length; i++)
        {
            arrowColliders[i].isTrigger = isTrigger;
        }
    }

    private void OnEnable()
    {
        arrowColliders = arrowColliderContainer.GetComponentsInChildren<Collider>();
    }

    private void FixedUpdate()
    {
        if (inFlight && arrowInteractable != null)
        {
            arrowInteractable.transform.LookAt(arrowInteractable.transform.position + arrowInteractable.InteractableRigidbody.velocity);
        }
    }
}
