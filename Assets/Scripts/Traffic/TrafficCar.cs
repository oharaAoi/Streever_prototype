
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交差点ごとに次の方向を選び、
/// 曲がり角をベジェ曲線で走行するNPC車。
/// GroundSlopeの勾配に対応。
/// </summary>
public class TrafficCar : MonoBehaviour
{
    private TrafficManager manager;

    private Vector2Int from;
    private Vector2Int direction;
    private Vector2Int to;

    private readonly Queue<Vector3> targets = new();

    private float speed;
    private float segmentDistance;
    private bool ready;

    [Header("ジャンプ設定")]
    [SerializeField]
    private float jumpHeight = 12.0f;

    [SerializeField]
    private float jumpDuration = 1.2f;

    private bool isJumping = false;
    private float jumpTimer = 0.0f;
    private float groundY = 0.0f;

    [Header("坂道への追従")]
    [SerializeField]
    private bool followGroundPitch = true;

    [SerializeField, Min(0.1f)]
    private float pitchLookAhead = 3f;

    [SerializeField, Min(1f)]
    private float rotationSpeed = 240f;

    // ジャンプ中の高さ
    private float GetJumpOffset()
    {
        if (!isJumping)
        {
            return 0.0f;
        }

        float t = Mathf.Clamp01(
            jumpTimer /
            Mathf.Max(0.01f, jumpDuration)
        );

        return Mathf.Sin(t * Mathf.PI) *
            jumpHeight;
    }

    // ジャンプ開始
    public void BeginJump()
    {
        if (!ready || isJumping)
        {
            return;
        }

        isJumping = true;
        jumpTimer = 0.0f;
    }

    // 初期化
    public void Initialize(
        TrafficManager traffic,
        Vector2Int start,
        Vector2Int dir,
        float drivingSpeed,
        float fraction)
    {
        manager = traffic;

        from = start;
        direction = dir;
        to = from + direction;

        speed = Mathf.Max(
            0.1f,
            drivingSpeed
        );

        groundY = manager.GetRoadHeight(
            transform.position.x,
            transform.position.z
        );

        isJumping = false;
        jumpTimer = 0.0f;
        segmentDistance = 0f;

        targets.Clear();

        ready = manager.IsInside(to);

        if (!ready)
        {
            return;
        }

        // 道路途中から次の交差点へ
        Vector3 begin =
            manager.Junction(from) +
            manager.Lane(direction);

        Vector3 end =
            manager.Junction(to) +
            manager.Lane(direction);

        float radius = RadiusForSegment(
            begin,
            end
        );

        targets.Enqueue(
            end -
            TrafficManager.ToVector3(direction) *
            radius
        );
    }

    // 曲がる半径を取得
    private float RadiusForSegment(
        Vector3 a,
        Vector3 b)
    {
        return Mathf.Min(
            manager.turnRadius,
            Vector3.Distance(a, b) * 0.25f
        );
    }

    private void Update()
    {
        if (!ready || manager == null)
        {
            return;
        }

        // ジャンプ時間を更新
        if (isJumping)
        {
            jumpTimer = Mathf.Min(
                jumpTimer + Time.deltaTime,
                jumpDuration
            );
        }

        // 次の目的地を設定
        if (targets.Count == 0)
        {
            BuildTurnAndNextSegment();
        }

        if (targets.Count == 0)
        {
            return;
        }

        Vector3 destination = targets.Peek();

        // 移動方向はXZ平面で計算
        Vector3 currentFlat = transform.position;
        currentFlat.y = 0f;

        Vector3 targetFlat = destination;
        targetFlat.y = 0f;

        Vector3 delta =
            targetFlat - currentFlat;

        Vector3 nextFlat = currentFlat;

        // 目標地点に到着
        if (delta.sqrMagnitude < 0.06f)
        {
            nextFlat = targetFlat;
            targets.Dequeue();
        }
        else if (!isJumping && CarAhead())
        {
            // 前方に車がいる場合は停止
            // 停止中もGroundの高さに合わせる
            groundY = manager.GetRoadHeight(
                currentFlat.x,
                currentFlat.z
            );

            Vector3 stopped =
                transform.position;

            stopped.y = groundY;

            transform.position = stopped;

            return;
        }
        else
        {
            // XZ方向へ移動
            nextFlat = Vector3.MoveTowards(
                currentFlat,
                targetFlat,
                speed * Time.deltaTime
            );

            Vector3 move =
                nextFlat - currentFlat;

            if (move.sqrMagnitude > 0.000001f)
            {
                Vector3 desiredForward =
                    move.normalized;

                // 坂道の高低差を取得
                if (!isJumping && followGroundPitch)
                {
                    Vector3 ahead =
                        nextFlat +
                        desiredForward *
                        pitchLookAhead;

                    float hereY =
                        manager.GetRoadHeight(
                            nextFlat.x,
                            nextFlat.z
                        );

                    float aheadY =
                        manager.GetRoadHeight(
                            ahead.x,
                            ahead.z
                        );

                    // 前後の高低差から
                    // 車体の進行方向を計算
                    desiredForward = new Vector3(
                        desiredForward.x * pitchLookAhead,
                        aheadY - hereY,
                        desiredForward.z * pitchLookAhead
                    ).normalized;
                }

                Quaternion desired =
                    Quaternion.LookRotation(
                        desiredForward,
                        Vector3.up
                    );

                // 車体を滑らかに回転
                transform.rotation =
                    Quaternion.RotateTowards(
                        transform.rotation,
                        desired,
                        rotationSpeed *
                        Time.deltaTime
                    );
            }
        }

        // 毎フレームGroundの高さを取得
        groundY = manager.GetRoadHeight(
            nextFlat.x,
            nextFlat.z
        );

        // 地面の高さとジャンプの高さを合成
        nextFlat.y =
            groundY + GetJumpOffset();

        transform.position = nextFlat;

        // ジャンプ終了
        if (isJumping &&
            jumpTimer >= jumpDuration)
        {
            isJumping = false;
            jumpTimer = 0.0f;

            Vector3 landed =
                transform.position;

            landed.y = groundY;

            transform.position = landed;
        }
    }

    // 次の交差点・道路を設定
    private void BuildTurnAndNextSegment()
    {
        Vector3 junction =
            manager.Junction(to);

        Vector2Int nextDir =
            manager.NextDirection(
                to,
                direction
            );

        Vector2Int nextTo =
            to + nextDir;

        if (!manager.IsInside(nextTo))
        {
            return;
        }

        Vector3 dirIn =
            TrafficManager.ToVector3(direction);

        Vector3 dirOut =
            TrafficManager.ToVector3(nextDir);

        Vector3 inLane =
            manager.Lane(direction);

        Vector3 outLane =
            manager.Lane(nextDir);

        float radius = RadiusForSegment(
            junction,
            manager.Junction(from)
        );

        radius = Mathf.Min(
            radius,
            RadiusForSegment(
                junction,
                manager.Junction(nextTo)
            )
        );

        Vector3 enter =
            junction -
            dirIn * radius +
            inLane;

        Vector3 exit =
            junction +
            dirOut * radius +
            outLane;

        // 2次ベジェ曲線
        // 車線を維持して交差点を曲がる
        Vector3 control =
            junction +
            inLane +
            outLane;

        const int samples = 10;

        for (int i = 1; i <= samples; i++)
        {
            float t =
                i / (float)samples;

            Vector3 p =
                (1f - t) * (1f - t) * enter +
                2f * (1f - t) * t * control +
                t * t * exit;

            targets.Enqueue(p);
        }

        from = to;
        to = nextTo;
        direction = nextDir;

        Vector3 nextEnd =
            manager.Junction(to) +
            manager.Lane(direction);

        float nextRadius = RadiusForSegment(
            junction,
            manager.Junction(to)
        );

        targets.Enqueue(
            nextEnd -
            dirOut * nextRadius
        );
    }

    // 前方車両の確認
    private bool CarAhead()
    {
        if (manager.followingDistance <= 0f)
        {
            return false;
        }

        Vector3 origin =
            transform.position +
            Vector3.up * 0.5f;

        // 他の車だけを検知
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            transform.forward,
            manager.followingDistance
        );

        foreach (RaycastHit hit in hits)
        {
            TrafficCar other =
                hit.collider
                    .GetComponentInParent<TrafficCar>();

            if (other != null &&
                other != this)
            {
                return true;
            }
        }

        return false;
    }
}
