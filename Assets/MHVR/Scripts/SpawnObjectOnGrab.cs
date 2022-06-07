using Tilia.Interactions.Interactables.Interactables;
using Tilia.Interactions.Interactables.Interactors;
using UnityEngine;

public class SpawnObjectOnGrab : MonoBehaviour
{
    public InteractableFacade spawnerInteractable;
    public GameObject objectPrefab;

    private void FixedUpdate()
    {
        foreach (InteractorFacade interactor in spawnerInteractable.TouchingInteractors)
        {
            if (interactor.GrabbedObjects.Count == 0 && interactor.GrabAction.IsActivated)
            {
                GameObject obj = Instantiate(objectPrefab);
                InteractableFacade interactable = obj.GetComponent<InteractableFacade>();
                interactor.Grab(interactable);
            }
        }
    }
}
