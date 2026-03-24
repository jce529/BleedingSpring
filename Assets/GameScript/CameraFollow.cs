using UnityEngine;

/// <summary>
/// 카메라가 플레이어를 부드럽게 따라다닙니다.
/// Main Camera 오브젝트에 붙이고 Target에 플레이어를 연결하세요.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform target;

    [Header("오프셋 (카메라와 플레이어 간 거리)")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f);

    [Header("부드러움 (값이 낮을수록 더 부드럽게 따라감)")]
    [SerializeField, Range(0.01f, 1f)] private float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);
    }
}
