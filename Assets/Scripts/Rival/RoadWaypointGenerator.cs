
using System.Collections.Generic;
using UnityEngine;

public class RoadWaypointGenerator : MonoBehaviour
{
    [SerializeField] private BuildingGenerator buildingGenerator;
    [SerializeField] private float waypointHeight = 0.5f;

    private readonly List<RoadWaypoint> waypoints = new();
    public IReadOnlyList<RoadWaypoint> Waypoints => waypoints;
    public bool IsReady => waypoints.Count > 0;

    private void Awake() => Generate();

    [ContextMenu("Generate Waypoints")]
    public void Generate()
    {
        if (buildingGenerator == null) { Debug.LogError("BuildingGeneratorを指定してください", this); return; }
        Clear();
        var xs = GetRoadCoordinates(buildingGenerator.countX, buildingGenerator.spacingX, buildingGenerator.roadScale);
        var zs = GetRoadCoordinates(buildingGenerator.countZ, buildingGenerator.spacingZ, buildingGenerator.roadScale);
        var grid = new RoadWaypoint[xs.Count, zs.Count];

        for (int x = 0; x < xs.Count; ++x)
            for (int z = 0; z < zs.Count; ++z)
            {
                var go = new GameObject($"Waypoint_{x}_{z}");
                go.transform.SetParent(transform, false);
                go.transform.position = buildingGenerator.transform.TransformPoint(new Vector3(xs[x], waypointHeight, zs[z]));
                var point = go.AddComponent<RoadWaypoint>();
                grid[x, z] = point;
                waypoints.Add(point);
            }
        for (int x = 0; x < xs.Count; ++x)
            for (int z = 0; z < zs.Count; ++z)
            {
                var p = grid[x, z];
                if (x > 0) p.neighbors.Add(grid[x - 1, z]);
                if (x + 1 < xs.Count) p.neighbors.Add(grid[x + 1, z]);
                if (z > 0) p.neighbors.Add(grid[x, z - 1]);
                if (z + 1 < zs.Count) p.neighbors.Add(grid[x, z + 1]);
            }
        Debug.Log($"Waypoint生成: {waypoints.Count}個", this);
    }

    private static List<float> GetRoadCoordinates(int count, float spacing, float scale)
    {
        var buildings = new List<float>();
        for (int i = 0; i < count; i++)
        {
            float index = i - (count - 1) * 0.5f;
            if (Mathf.Approximately(index, 0f)) continue;
            float pos = Mathf.Sign(index) * (spacing * scale + (Mathf.Abs(index) - 1f) * spacing);
            buildings.Add(pos);
        }
        buildings.Sort();
        var roads = new List<float> { 0f };
        for (int i = 0; i + 1 < buildings.Count; i++)
        {
            float midpoint = (buildings[i] + buildings[i + 1]) * 0.5f;
            if (!Mathf.Approximately(midpoint, 0f)) roads.Add(midpoint);
        }
        roads.Sort();
        return roads;
    }

    private void Clear()
    {
        var previous = GetComponentsInChildren<RoadWaypoint>(true);
        foreach (var point in previous)
        {
            point.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(point.gameObject);
            else DestroyImmediate(point.gameObject);
        }
        waypoints.Clear();
    }

    public RoadWaypoint Nearest(Vector3 position)
    {
        RoadWaypoint nearest = null;
        float best = float.PositiveInfinity;
        foreach (var waypoint in waypoints)
        {
            if (waypoint == null) continue;
            Vector3 d = waypoint.transform.position - position;
            float dist = d.x * d.x + d.z * d.z;
            if (dist < best) { best = dist; nearest = waypoint; }
        }
        return nearest;
    }
}
