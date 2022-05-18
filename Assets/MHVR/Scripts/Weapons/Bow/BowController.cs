using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField]
    private Animator bowAnimator;

    [SerializeField]
    private string foldAnimationParameter = "isFolded";

    public void toggleFold()
    {
        bowAnimator.SetBool(foldAnimationParameter, !bowAnimator.GetBool(foldAnimationParameter));
    }
}