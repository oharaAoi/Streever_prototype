
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

        if (targets.Count == 0)
            BuildTurnAndNextSegment();

        if (targets.Count == 0)
            return;

        Vector3 destination = targets.Peek();

        Vector3 delta =
            destination - transform.position;

        delta.y = 0;

        if (delta.sqrMagnitude < 0.06f)
        {
            transform.position = destination;
            targets.Dequeue();
            return;
        }

        if (CarAhead())
            return;

        float movement =
            speed * Time.deltaTime;

        Vector3 next = Vector3.MoveTowards(
            transform.position,
            destination,
            movement
        );

        Vector3 move =
            next - transform.position;

        if (move.sqrMagnitude > 0.000001f)
        {
            Quaternion desired = Quaternion.LookRotation(
                move.normalized,
                Vector3.up
            );

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desired,
                240f * Time.deltaTime
            );
        }

        transform.position = next;
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
