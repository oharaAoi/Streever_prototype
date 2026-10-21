
using System.Collections.Generic;
using UnityEngine;

public class RoadWaypoint : MonoBehaviour
{
    public readonly List<RoadWaypoint> neighbors = new();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 1.5f);
        Gizmos.color = Color.green;
        foreach (var neighbor in neighbors)
            if (neighbor != null) Gizmos.DrawLine(transform.position, neighbor.transform.position);
    }
}
