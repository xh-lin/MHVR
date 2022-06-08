using UnityEngine;

[System.Serializable]
class MapTransform
{
    public Transform VRTarget;
    public Transform IKTarget;
    public Vector3 PositionOffset;
    public Vector3 RotationOffset;

    public void MapVRAvatar()
    {
        IKTarget.SetPositionAndRotation(
            VRTarget.TransformPoint(PositionOffset),
            VRTarget.rotation * Quaternion.Euler(RotationOffset));
    }
}

public class AvatarController : MonoBehaviour
{
    [SerializeField]
    private MapTransform head;
    [SerializeField]
    private MapTransform leftHand;
    [SerializeField]
    private MapTransform rightHand;
    [SerializeField]
    private Transform avatar;
    [SerializeField]
    private float bodyTurnSmoothness = 5f;

    private Vector3 headBodyOffset;

    private void Start()
    {
        headBodyOffset = avatar.position - head.IKTarget.position;
    }

    void LateUpdate()
    {
        avatar.position = head.IKTarget.position + headBodyOffset;
        avatar.forward = Vector3.Lerp(
            avatar.forward,
            Vector3.ProjectOnPlane(head.IKTarget.forward, Vector3.up).normalized,
            Time.deltaTime * bodyTurnSmoothness);
        head.MapVRAvatar();
        leftHand.MapVRAvatar();
        rightHand.MapVRAvatar();
    }
}
