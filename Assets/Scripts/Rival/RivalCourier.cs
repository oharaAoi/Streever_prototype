
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RivalCourier : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private BuildingGenerator buildingGenerator;
    [SerializeField] private RoadWaypointGenerator roadGenerator;

    [Header("走行")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 12f;
    [SerializeField, Min(0.1f)] private float turnSpeed = 8f;
    [SerializeField, Min(0.1f)] private float arrivalDistance = 1.5f;
    [SerializeField, Min(0f)] private float deliveryWait = 0.5f;

    private Rigidbody rb;
    private DeliveryAddress target;
    private readonly List<RoadWaypoint> route = new();
    private int routeIndex;
    private float waitTime;
    private bool waiting;
    private int successfulDeliveries;
    public int SuccessfulDeliveries => successfulDeliveries;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        if (buildingGenerator != null) buildingGenerator.DeliveryCompleted += HandleDeliveryCompleted;
    }
    private void OnDisable()
    {
        if (buildingGenerator != null) buildingGenerator.DeliveryCompleted -= HandleDeliveryCompleted;
    }
    private void HandleDeliveryCompleted(DeliveryAddress address)
    {
        if (target == address) ResetTarget();
    }

    private void FixedUpdate()
    {
        if (buildingGenerator == null || roadGenerator == null || !roadGenerator.IsReady) return;
        // Destroyはフレーム末尾なので、リスト上の存在も確認する
        if (target != null && !ContainsAddress(target)) ResetTarget();
        if (target == null) { SelectNearestDelivery(); return; }

        if (waiting)
        {
            waitTime += Time.fixedDeltaTime;
            if (waitTime >= deliveryWait)
            {
                var completed = target;
                ResetTarget();
                if (buildingGenerator.TryCompleteDelivery(completed))
                {
                    successfulDeliveries++;
                    Debug.Log($"ライバル配達成功: {successfulDeliveries}件", this);
                }
            }
            return;
        }
        MoveAlongRoute();
    }

    private bool ContainsAddress(DeliveryAddress address)
    {
        foreach (var item in buildingGenerator.ActiveAddresses)
            if (item == address) return true;
        return false;
    }

    private void ResetTarget()
    {
        target = null;
        route.Clear();
        routeIndex = 0;
        waiting = false;
        waitTime = 0f;
    }

    private void SelectNearestDelivery()
    {
        var start = roadGenerator.Nearest(rb.position);
        if (start == null) return;

        float best = float.PositiveInfinity;
        DeliveryAddress bestAddress = null;
        List<RoadWaypoint> bestRoute = null;

        foreach (var address in buildingGenerator.ActiveAddresses)
        {
            if (address == null) continue;
            var goal = roadGenerator.Nearest(address.transform.position);
            if (goal == null) continue;
            var candidateRoute = ShortestPath(start, goal);
            if (candidateRoute == null) continue;

            // 実際のWaypoint経路距離で比較（先頭/末尾の取り付き距離も加算）
            float cost = HorizontalDistance(rb.position, start.transform.position)
                       + HorizontalDistance(goal.transform.position, address.transform.position);
            for (int i = 1; i < candidateRoute.Count; i++)
                cost += HorizontalDistance(candidateRoute[i - 1].transform.position,
                                           candidateRoute[i].transform.position);
            if (cost >= best) continue;
            best = cost;
            bestAddress = address;
            bestRoute = candidateRoute;
        }
        if (bestAddress == null) return;
        target = bestAddress;
        route.Clear();
        route.AddRange(bestRoute);
        routeIndex = 0;
        Debug.Log($"ライバルの目標: {target.name} / 推定走行距離: {best:F1}", this);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
        => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    // 道路区間の距離を重みとするDijkstra探索
    private static List<RoadWaypoint> ShortestPath(RoadWaypoint start, RoadWaypoint goal)
    {
        var dist = new Dictionary<RoadWaypoint, float> { [start] = 0f };
        var previous = new Dictionary<RoadWaypoint, RoadWaypoint> { [start] = null };
        var open = new List<RoadWaypoint> { start };
        while (open.Count > 0)
        {
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
                if (dist[open[i]] < dist[open[bestIndex]]) bestIndex = i;
            var current = open[bestIndex];
            open.RemoveAt(bestIndex);
            if (current == goal) break;
            foreach (var next in current.neighbors)
            {
                if (next == null) continue;
                float cost = dist[current] + HorizontalDistance(current.transform.position, next.transform.position);
                if (dist.TryGetValue(next, out float old) && cost >= old) continue;
                dist[next] = cost;
                previous[next] = current;
                if (!open.Contains(next)) open.Add(next);
            }
        }
        if (!previous.ContainsKey(goal)) return null;
        var result = new List<RoadWaypoint>();
        for (var node = goal; node != null; node = previous[node]) result.Add(node);
        result.Reverse();
        return result;
    }

    private void MoveAlongRoute()
    {
        if (routeIndex >= route.Count) { waiting = true; waitTime = 0f; return; }
        var point = route[routeIndex];
        if (point == null) { ResetTarget(); return; }
        var destination = point.transform.position;
        var origin = rb.position;
        destination.y = origin.y; // 平坦な道路用。斜面対応は別途実装
        var direction = destination - origin;
        if (direction.magnitude <= arrivalDistance) { routeIndex++; return; }
        rb.MovePosition(Vector3.MoveTowards(origin, destination, moveSpeed * Time.fixedDeltaTime));
        if (direction.sqrMagnitude > 0.0001f)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation,
                Quaternion.LookRotation(direction),
                Mathf.Clamp01(turnSpeed * Time.fixedDeltaTime)));
    }
}
