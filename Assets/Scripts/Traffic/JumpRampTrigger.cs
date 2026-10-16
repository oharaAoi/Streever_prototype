
using UnityEngine;

public class JumpRampTrigger : MonoBehaviour
{
    [Header("ジャンプ判定")]
    [SerializeField]
    private bool checkDirection = true;

    [Range(-1.0f, 1.0f)]
    [SerializeField]
    private float minimumDirectionDot = 0.3f;

    private void OnTriggerEnter(Collider other)
    {
        TrafficCar car =
            other.GetComponentInParent<TrafficCar>();

        if (car == null)
            return;

        // ジャンプ台へ正しい方向から
        // 進入したか確認する
        if (checkDirection)
        {
            Vector3 rampForward = transform.forward;
            rampForward.y = 0.0f;

            Vector3 carForward =
                car.transform.forward;

            carForward.y = 0.0f;

            if (rampForward.sqrMagnitude < 0.0001f ||
                carForward.sqrMagnitude < 0.0001f)
                return;

            rampForward.Normalize();
            carForward.Normalize();

            float dot = Vector3.Dot(
                carForward,
                rampForward
            );

            if (dot < minimumDirectionDot)
                return;
        }

        // ジャンプ開始
        car.BeginJump();
    }
}
