using UnityEngine;

namespace OriginalKartRacer.CameraSystem
{
    /// <summary>车尾弹簧跟随相机；LateUpdate 避免物理更新后的画面抖动。</summary>
    public sealed class ThirdPersonKartCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, 3.1f, -6.4f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.1f, 2.3f);
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.12f;
        [SerializeField, Min(0f)] private float rotationSharpness = 10f;
        [SerializeField, Min(0f)] private float speedLookAhead = 0.035f;
        [SerializeField] private Rigidbody targetBody;

        private Vector3 positionVelocity;

        private void LateUpdate()
        {
            if (target == null)
                return;

            float forwardSpeed = targetBody == null
                ? 0f
                : Vector3.Dot(targetBody.velocity, target.forward);
            Quaternion heading=Quaternion.Euler(0,target.eulerAngles.y,0);
            Vector3 desiredPosition = target.position+heading*localOffset;
            Vector3 lookPoint = target.position+heading*lookOffset
                                + target.forward * (forwardSpeed * speedLookAhead);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref positionVelocity,
                positionSmoothTime);

            Quaternion desiredRotation = Quaternion.LookRotation(
                lookPoint - transform.position,
                Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }
    }
}
