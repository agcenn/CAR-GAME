using UnityEngine;

namespace OriginalKartRacer.Track
{
    /// <summary>让悬浮导航箭头产生轻微呼吸运动；箭头模型请使用原创网格。</summary>
    public sealed class DirectionalArrow : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float bobHeight = 0.18f;
        [SerializeField, Min(0f)] private float bobFrequency = 1.5f;
        private Vector3 origin;

        private void Awake() => origin = transform.localPosition;

        private void Update()
        {
            transform.localPosition = origin
                                      + Vector3.up * (Mathf.Sin(Time.time * bobFrequency) * bobHeight);
        }
    }
}
