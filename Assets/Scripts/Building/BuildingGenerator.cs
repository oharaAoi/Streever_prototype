
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.AI;

public class BuildingGenerator : MonoBehaviour
{
    [Header("地面に合わせる")]
    [SerializeField] private GroundSlope groundSlope;
    [SerializeField] private float buildingGroundOffset = 0f;
    [SerializeField] private float poleGroundOffset = 0f;

    [Header("生成数")]
    public int countX = 9;
    public int countZ = 11;

    [Header("ビル同士の間隔")]
    public float spacingX = 110f;
    public float spacingZ = 90f;

    [Header("ビルの横幅")]
    public Vector2 widthRange = new Vector2(40f, 60f);

    [Header("ビルの奥行き")]
    public Vector2 depthRange = new Vector2(40f, 60f);

    [Header("ビルの高さ")]
    public Vector2 heightRange = new Vector2(60f, 150f);

    [Header("中央道路の判定幅")]
    public float roadHalfWidth = 30f;

    [Header("中央道路の細さ")]
    [Range(0.5f, 1.2f)]
    public float roadScale = 0.8f;

    [Header("配達先の設定")]
    [SerializeField]
    private GameObject deliveryAddressPrefab;

    [Header("UI Prefab")]
    [SerializeField]
    private WorldTargetUI markerPrefab;

    [Header("Canvas")]
    [SerializeField]
    private RectTransform canvasRect;

    [Header("Camera")]
    [SerializeField]
    private Camera targetCamera;

    [Header("ポール")]
    [SerializeField]
    private GameObject pole;

    [SerializeField]
    private float poleOffset = 10.0f;

    [SerializeField, Min(0)]
    private int deliveryAddressCount = 5;

    private readonly List<GameObject> buildings = new();
    private readonly List<DeliveryAddress> activeAddresses = new();

    private void Start()
    {
        InitializeBuildings();
    }

    private void InitializeBuildings()
    {
        buildings.Clear();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Building_"))
            {
                buildings.Add(child.gameObject);
            }
        }

        Debug.Log($"建物を{buildings.Count}個取得しました");
    }

    // 地面の高さを取得
    private float GetGroundHeight(float x, float z)
    {
        if (groundSlope != null)
        {
            return groundSlope.GetHeightAt(x, z);
        }

        return transform.position.y;
    }

    [ContextMenu("Generate Buildings")]
    public void GenerateBuildings()
    {
        ClearBuildings();

        for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float indexX = x - (countX - 1) * 0.5f;
                float indexZ = z - (countZ - 1) * 0.5f;

                float posX = 0.0f;
                float posZ = 0.0f;

                // X方向
                if (indexX != 0.0f)
                {
                    posX = Mathf.Sign(indexX) *
                        (
                            spacingX * roadScale +
                            (Mathf.Abs(indexX) - 1.0f) * spacingX
                        );
                }

                // Z方向
                if (indexZ != 0.0f)
                {
                    posZ = Mathf.Sign(indexZ) *
                        (
                            spacingZ * roadScale +
                            (Mathf.Abs(indexZ) - 1.0f) * spacingZ
                        );
                }

                // 中央の十字部分には生成しない
                if (Mathf.Abs(posX) < roadHalfWidth ||
                    Mathf.Abs(posZ) < roadHalfWidth)
                {
                    continue;
                }

                float width = Random.Range(
                    widthRange.x, widthRange.y);

                float depth = Random.Range(
                    depthRange.x, depthRange.y);

                float height = Random.Range(
                    heightRange.x, heightRange.y);

                // ビル生成
                GameObject building =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);

                building.name = $"Building_{x}_{z}";
                building.transform.SetParent(transform);

                // 先にXZを設定
                building.transform.localPosition =
                    new Vector3(posX, 0f, posZ);

                building.transform.localScale =
                    new Vector3(width, height, depth);

                // 地面の高さを取得
                Vector3 worldPosition =
                    building.transform.position;

                float groundY = GetGroundHeight(
                    worldPosition.x,
                    worldPosition.z
                );

                // ビルの底面を地面に合わせる
                worldPosition.y =
                    groundY +
                    building.transform.lossyScale.y * 0.5f +
                    buildingGroundOffset;

                building.transform.position = worldPosition;

                // NavMeshObstacle追加
                NavMeshObstacle obstacle =
                    building.AddComponent<NavMeshObstacle>();

                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = Vector3.zero;
                obstacle.size = Vector3.one;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;

                // ビルを管理
                buildings.Add(building);
                building.layer = 7;

                // ポール生成
                SpawnPole(building, width, depth);
            }
        }
    }

    [ContextMenu("Clear Buildings")]
    public void ClearBuildings()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        if (canvasRect != null)
        {
            WorldTargetUI[] markers =
                canvasRect.GetComponentsInChildren<WorldTargetUI>(true);

            foreach (WorldTargetUI marker in markers)
            {
                if (marker == null)
                    continue;

                if (Application.isPlaying)
                {
                    Destroy(marker.gameObject);
                }
                else
                {
                    DestroyImmediate(marker.gameObject);
                }
            }
        }

        buildings.Clear();
        activeAddresses.Clear();
    }

    // 配達完了時
    public void CompleteDelivery(DeliveryAddress address)
    {
        if (address == null)
            return;

        if (!activeAddresses.Remove(address))
            return;

        Destroy(address.gameObject);

        // 必要に応じて新しい配達先を生成
        // SpawnDeliveryAddress();
    }

    // 建物1個につきポールを1個生成
    private void SpawnPole(
        GameObject building,
        float width,
        float depth)
    {
        if (pole == null)
            return;

        float halfWidth = width * 0.5f;
        float halfDepth = depth * 0.5f;

        Vector3 localPosition = Vector3.zero;
        Quaternion rotation = Quaternion.identity;

        // 4つの壁からランダムに選択
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0: // 前面
                localPosition = new Vector3(
                    Random.Range(-halfWidth, halfWidth),
                    0.0f,
                    halfDepth + poleOffset
                );
                break;

            case 1: // 背面
                localPosition = new Vector3(
                    Random.Range(-halfWidth, halfWidth),
                    0.0f,
                    -halfDepth - poleOffset
                );
                break;

            case 2: // 右側
                localPosition = new Vector3(
                    halfWidth + poleOffset,
                    0.0f,
                    Random.Range(-halfDepth, halfDepth)
                );
                break;

            case 3: // 左側
                localPosition = new Vector3(
                    -halfWidth - poleOffset,
                    0.0f,
                    Random.Range(-halfDepth, halfDepth)
                );
                break;
        }

        Vector3 buildingPosition = building.transform.position;

        Vector3 position = new Vector3(
            buildingPosition.x + localPosition.x,
            0.0f,
            buildingPosition.z + localPosition.z
        );

        // ポールの位置で地面の高さを取得
        position.y = GetGroundHeight(
            position.x,
            position.z
        ) + poleGroundOffset;

        Instantiate(
            pole,
            position,
            rotation,
            transform
        );
    }
}
