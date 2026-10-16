
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 交差点ごとに次の方向を選び、
/// 曲がり角をベジェ曲線で走行するNPC車。
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

    // ジャンプ中の高さを計算
    private float GetJumpOffset()
    {
        if (!isJumping)
            return 0.0f;

        float t = Mathf.Clamp01(
            jumpTimer / Mathf.Max(0.01f, jumpDuration)
        );

        return Mathf.Sin(t * Mathf.PI) * jumpHeight;
    }

    // ジャンプ開始
    public void BeginJump()
    {
        if (!ready || isJumping)
            return;

        isJumping = true;
        jumpTimer = 0.0f;
    }


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

        speed = Mathf.Max(0.1f, drivingSpeed);
        groundY = manager.Junction(from).y;
        isJumping = false;
        jumpTimer = 0.0f;
        segmentDistance = 0f;

        targets.Clear();

        ready = manager.IsInside(to);

        if (!ready)
            return;

        // 最初は道路途中から次の交差点の進入点へ
        Vector3 begin =
            manager.Junction(from) + manager.Lane(direction);

        Vector3 end =
            manager.Junction(to) + manager.Lane(direction);

        float radius = RadiusForSegment(begin, end);

        targets.Enqueue(
            end - TrafficManager.ToVector3(direction) * radius
        );
    }

    private float RadiusForSegment(Vector3 a, Vector3 b)
    {
        return Mathf.Min(
            manager.turnRadius,
            Vector3.Distance(a, b) * 0.25f
        );
    }


    private void Update()
    {
        if (!ready || manager == null)
            return;

        // ジャンプ時間を進める
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;

            if (jumpTimer >= jumpDuration)
            {
                jumpTimer = jumpDuration;
            }
        }

        if (targets.Count == 0)
            BuildTurnAndNextSegment();

        if (targets.Count == 0)
            return;

        Vector3 destination = targets.Peek();

        // Y方向を無視して道路上の移動方向を求める
        Vector3 currentFlat = transform.position;
        currentFlat.y = groundY;

        Vector3 targetFlat = destination;
        targetFlat.y = groundY;

        Vector3 delta = targetFlat - currentFlat;

        // 目標地点に到着
        if (delta.sqrMagnitude < 0.06f)
        {
            currentFlat = targetFlat;
            targets.Dequeue();
        }
        else
        {
            // ジャンプ中は前方車両で停止しない
            if (!isJumping && CarAhead())
                return;

            Vector3 next = Vector3.MoveTowards(
                currentFlat,
                targetFlat,
                speed * Time.deltaTime
            );

            Vector3 move = next - currentFlat;

            if (move.sqrMagnitude > 0.000001f)
            {
                Quaternion desired =
                    Quaternion.LookRotation(
                        move.normalized,
                        Vector3.up
                    );

                transform.rotation =
                    Quaternion.RotateTowards(
                        transform.rotation,
                        desired,
                        240f * Time.deltaTime
                    );
            }

            currentFlat = next;
        }

        // ジャンプの放物線を高さに加算
        currentFlat.y = groundY + GetJumpOffset();

        transform.position = currentFlat;

        // ジャンプ終了
        if (isJumping && jumpTimer >= jumpDuration)
        {
            isJumping = false;
            jumpTimer = 0.0f;

            Vector3 position = transform.position;
            position.y = groundY;

            transform.position = position;
        }
    }


    private void BuildTurnAndNextSegment()
    {
        Vector3 junction =
            manager.Junction(to);

        Vector2Int nextDir =
            manager.NextDirection(to, direction);

        Vector2Int nextTo =
            to + nextDir;

        if (!manager.IsInside(nextTo))
            return;

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
            junction - dirIn * radius + inLane;

        Vector3 exit =
            junction + dirOut * radius + outLane;

        // 2次ベジェ曲線
        // 車線を維持しながら交差点を曲がる
        Vector3 control =
            junction + inLane + outLane;

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
            manager.Junction(to) + manager.Lane(direction);

        float nextRadius = RadiusForSegment(
            junction,
            manager.Junction(to)
        );

        targets.Enqueue(
            nextEnd - dirOut * nextRadius
        );
    }

    private bool CarAhead()
    {
        if (manager.followingDistance <= 0f)
            return false;

        Vector3 origin =
            transform.position + Vector3.up * 0.5f;

        // 他の車のみ検知
        // ビルや地面は停止対象外
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            transform.forward,
            manager.followingDistance
        );

        foreach (RaycastHit hit in hits)
        {
            TrafficCar other =
                hit.collider.GetComponentInParent<TrafficCar>();

            if (other != null && other != this)
                return true;
        }

        return false;
    }
}
