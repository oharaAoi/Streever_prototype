
using UnityEngine;

public class CarRampAttachment : MonoBehaviour
{
    [Header("ジャンプ台Prefab")]
    [SerializeField]
    private GameObject jumpingPlatformPrefab;

    [Header("ジャンプ台の取り付け位置")]
    [SerializeField]
    private Transform rampAnchor;

    [Header("ジャンプ台の位置調整")]
    [SerializeField]
    private Vector3 rampLocalPosition = Vector3.zero;

    [Header("ジャンプ台の回転調整")]
    [SerializeField]
    private Vector3 rampLocalRotation = Vector3.zero;

    [Header("ジャンプ台の倍率")]
    [SerializeField]
    private Vector3 rampLocalScale = Vector3.one;

    private GameObject spawnedRamp;

    private void Start()
    {
        CreateRamp();
    }

    private void CreateRamp()
    {
        if (jumpingPlatformPrefab == null)
        {
            Debug.LogWarning(
                "ジャンプ台Prefabが設定されていません。",
                this
            );
            return;
        }

        if (rampAnchor == null)
        {
            Debug.LogWarning(
                "RampAnchorが設定されていません。",
                this
            );
            return;
        }

        if (spawnedRamp != null)
            return;

        // ジャンプ台をRampAnchorの子に生成
        spawnedRamp = Instantiate(
            jumpingPlatformPrefab,
            rampAnchor
        );

        spawnedRamp.name = "AttachedJumpingPlatform";

        // Prefab元の位置・回転を上書き
        spawnedRamp.transform.localPosition =
            rampLocalPosition;

        // Prefabにもともと設定されている回転を取得
        Quaternion originalRotation =
            jumpingPlatformPrefab.transform.localRotation;

        // 元の回転に追加回転を適用
        spawnedRamp.transform.localRotation =
            Quaternion.Euler(rampLocalRotation) * originalRotation;

        // Prefab元のスケールを基準に倍率をかける
        Vector3 originalScale =
            jumpingPlatformPrefab.transform.localScale;

        spawnedRamp.transform.localScale =
            Vector3.Scale(
                originalScale,
                rampLocalScale
            );
    }
}
