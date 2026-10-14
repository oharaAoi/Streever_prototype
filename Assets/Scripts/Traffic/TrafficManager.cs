
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BuildingGenerator と同じ計算で「ビルの間の道路中心線」を復元します。
/// BuildingGenerator 自体の変更は不要です。回転・スケールは 0 / 1 前提です。
/// </summary>
public class TrafficManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private BuildingGenerator buildingGenerator;
    [SerializeField] private GameObject carPrefab;

    [Header("生成")]
    [Min(0)] public int carCount = 20;
    [SerializeField] private bool generateOnStart = true;

    [Tooltip("道路面から車PrefabのPivotまでの高さ")]
    public float carY = 0.5f;

    [Min(2f)] public float minimumSpawnDistance = 12f;

    [Header("走行")]
    [Tooltip("道路中心から車線中心までの距離。日本式の左側通行です")]
    [Min(0f)] public float laneOffset = 3f;

    [Min(1f)] public float turnRadius = 8f;

    public Vector2 speedRange = new Vector2(10f, 16f);

    [Range(0f, 1f)]
    public float turnProbability = 0.35f;

    [Min(0f)] public float followingDistance = 7f;

    [Header("表示")]
    public bool drawRoadGizmos = true;

    private readonly List<TrafficCar> cars = new();

    private float[] roadX;
    private float[] roadZ;

    private void Start()
    {
        if (generateOnStart)
            GenerateTraffic();
    }

    [ContextMenu("Generate Traffic")]
    public void GenerateTraffic()
    {
        ClearTraffic();

        if (buildingGenerator == null || carPrefab == null)
        {
            Debug.LogWarning(
                "TrafficManager: BuildingGenerator と Car Prefab を設定してください。",
                this
            );
            return;
        }

        BuildRoads();

        if (roadX.Length < 2 || roadZ.Length < 2)
        {
            Debug.LogWarning(
                "TrafficManager: 車の走行に必要な道路がありません。",
                this
            );
            return;
        }

        // 重複を避けながら道路に車を並べる
        int attempts = Mathf.Max(20, carCount * 30);

        while (cars.Count < carCount && attempts-- > 0)
        {
            int ix = Random.Range(0, roadX.Length);
            int iz = Random.Range(0, roadZ.Length);

            Vector2Int dir = RandomDirection();

            Vector2Int next = new Vector2Int(
                ix + dir.x,
                iz + dir.y
            );

            if (!IsInside(next))
                continue;

            Vector3 a = Junction(new Vector2Int(ix, iz));
            Vector3 b = Junction(next);

            float fraction = Random.Range(0.2f, 0.75f);

            Vector3 pos =
                Vector3.Lerp(a, b, fraction) + Lane(dir);

            bool crowded = false;

            foreach (TrafficCar other in cars)
            {
                if (other != null &&
                    Vector3.Distance(
                        pos,
                        other.transform.position
                    ) < minimumSpawnDistance)
                {
                    crowded = true;
                    break;
                }
            }

            if (crowded)
                continue;

            GameObject obj = Instantiate(
                carPrefab,
                pos,
                Quaternion.LookRotation(ToVector3(dir)),
                transform
            );

            obj.name = $"TrafficCar_{cars.Count:00}";

            TrafficCar car = obj.GetComponent<TrafficCar>();

            if (car == null)
                car = obj.AddComponent<TrafficCar>();

            car.Initialize(
                this,
                new Vector2Int(ix, iz),
                dir,
                Random.Range(
                    Mathf.Min(speedRange.x, speedRange.y),
                    Mathf.Max(speedRange.x, speedRange.y)
                ),
                fraction
            );

            cars.Add(car);
        }
    }

    [ContextMenu("Clear Traffic")]
    public void ClearTraffic()
    {
        cars.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child =
                transform.GetChild(i).gameObject;

            if (child.GetComponent<TrafficCar>() == null)
                continue;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void BuildRoads()
    {
        roadX = CalculateRoads(
            buildingGenerator.countX,
            buildingGenerator.spacingX,
            buildingGenerator.roadScale
        );

        roadZ = CalculateRoads(
            buildingGenerator.countZ,
            buildingGenerator.spacingZ,
            buildingGenerator.roadScale
        );
    }

    private static float[] CalculateRoads(
        int count,
        float spacing,
        float scale)
    {
        List<float> centers = new();

        for (int i = 0; i < count; i++)
        {
            float index = i - (count - 1) * 0.5f;

            if (Mathf.Approximately(index, 0f))
                continue;

            float coord =
                Mathf.Sign(index) *
                (
                    spacing * scale +
                    (Mathf.Abs(index) - 1f) * spacing
                );

            centers.Add(coord);
        }

        centers.Sort();

        List<float> roads = new();

        for (int i = 0; i < centers.Count - 1; i++)
        {
            float mid =
                (centers[i] + centers[i + 1]) * 0.5f;

            // 中央は座標0を通る十字路として扱う
            if (centers[i] < 0f && centers[i + 1] > 0f)
                mid = 0f;

            roads.Add(mid);
        }

        if (!roads.Exists(v => Mathf.Abs(v) < 0.001f))
            roads.Add(0f);

        roads.Sort();

        return roads.ToArray();
    }

    public Vector3 Junction(Vector2Int index)
    {
        return buildingGenerator.transform.position +
            new Vector3(
                roadX[index.x],
                carY,
                roadZ[index.y]
            );
    }

    public Vector3 Lane(Vector2Int dir)
    {
        // 左側通行
        // +Z で -X 側、+X で +Z 側にオフセット
        return new Vector3(
            -dir.y,
            0f,
            dir.x
        ) * laneOffset;
    }

    public bool IsInside(Vector2Int p)
    {
        return roadX != null &&
               roadZ != null &&
               p.x >= 0 &&
               p.x < roadX.Length &&
               p.y >= 0 &&
               p.y < roadZ.Length;
    }

    public Vector2Int NextDirection(
        Vector2Int at,
        Vector2Int current)
    {
        Vector2Int left = new Vector2Int(
            -current.y,
            current.x
        );

        Vector2Int right = new Vector2Int(
            current.y,
            -current.x
        );

        List<Vector2Int> choices = new();

        if (IsInside(at + current))
            choices.Add(current);

        if (IsInside(at + left))
            choices.Add(left);

        if (IsInside(at + right))
            choices.Add(right);

        // 端でのみ折り返す
        if (choices.Count == 0)
            return -current;

        if (choices.Contains(current) &&
            Random.value > turnProbability)
        {
            return current;
        }

        return choices[
            Random.Range(0, choices.Count)
        ];
    }

    public static Vector3 ToVector3(Vector2Int d)
    {
        return new Vector3(
            d.x,
            0f,
            d.y
        );
    }

    private static Vector2Int RandomDirection()
    {
        switch (Random.Range(0, 4))
        {
            case 0:
                return Vector2Int.right;

            case 1:
                return Vector2Int.left;

            case 2:
                return Vector2Int.up;

            default:
                return Vector2Int.down;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRoadGizmos || buildingGenerator == null)
            return;

        float[] xs = CalculateRoads(
            buildingGenerator.countX,
            buildingGenerator.spacingX,
            buildingGenerator.roadScale
        );

        float[] zs = CalculateRoads(
            buildingGenerator.countZ,
            buildingGenerator.spacingZ,
            buildingGenerator.roadScale
        );

        if (xs.Length == 0 || zs.Length == 0)
            return;

        Vector3 origin =
            buildingGenerator.transform.position +
            Vector3.up * (carY + 0.1f);

        Gizmos.color = Color.cyan;

        foreach (float x in xs)
        {
            Gizmos.DrawLine(
                origin + new Vector3(x, 0, zs[0]),
                origin + new Vector3(
                    x,
                    0,
                    zs[zs.Length - 1]
                )
            );
        }

        foreach (float z in zs)
        {
            Gizmos.DrawLine(
                origin + new Vector3(xs[0], 0, z),
                origin + new Vector3(
                    xs[xs.Length - 1],
                    0,
                    z
                )
            );
        }
    }
}
