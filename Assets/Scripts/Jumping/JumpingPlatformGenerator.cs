using System.Collections.Generic;
using UnityEngine;

public class JumpingPlatformGenerator : MonoBehaviour
{
    [Header("ジャンプ台Prefab")]
    [SerializeField]
    private GameObject jumpingPlatformPrefab;

    [Header("生成済みビル群")]
    [SerializeField]
    private Transform buildingsRoot;

    [Header("生成するジャンプ台ペア数")]
    [SerializeField]
    private int pairCount = 12;

    [Header("ジャンプ台の高さ")]
    [SerializeField]
    private float yPosition = 0.0f;

    [Header("2つのジャンプ台の間隔")]
    [SerializeField]
    private float pairDistance = 18.0f;

    [Header("他のジャンプ台との最低距離")]
    [SerializeField]
    private float minDistance = 70.0f;

    [Header("交差点から離す距離")]
    [SerializeField]
    private float intersectionClearance = 35.0f;

    [Header("中央十字路から離す距離")]
    [SerializeField]
    private float centerClearance = 100.0f;

    [Header("マップ端から離す距離")]
    [SerializeField]
    private float edgeClearance = 40.0f;

    [Header("ジャンプ台の傾き")]
    [SerializeField]
    private float rampAngle = 26.0f;

    [Header("ジャンプ台の占有サイズ")]
    [SerializeField]
    private Vector3 platformCheckSize =
        new Vector3(14.0f, 10.0f, 22.0f);

    [Header("進行方向の確認距離")]
    [SerializeField]
    private float roadCheckDistance = 50.0f;

    [Header("進行方向の確認幅")]
    [SerializeField]
    private float roadCheckWidth = 16.0f;

    [Header("最大生成試行回数")]
    [SerializeField]
    private int maxAttemptsPerArea = 500;

    [Header("中央交差点そのものから離す距離")]
    [SerializeField]
    private float centerIntersectionClearance = 70.0f;


    private readonly List<float> buildingXPositions =
        new List<float>();

    private readonly List<float> buildingZPositions =
        new List<float>();

    private readonly List<float> verticalRoadXPositions =
        new List<float>();

    private readonly List<float> horizontalRoadZPositions =
        new List<float>();


    private enum Area
    {
        CenterCross,
        LeftUpper,
        LeftLower,
        RightUpper,
        RightLower
    }


    [ContextMenu("Generate Jumping Platforms")]
    public void GenerateJumpingPlatforms()
    {
        ClearJumpingPlatforms();

        if (jumpingPlatformPrefab == null)
        {
            Debug.LogWarning(
                "JumpingPlatform Prefab が設定されていません。"
            );
            return;
        }

        if (buildingsRoot == null)
        {
            Debug.LogWarning(
                "Buildings Root が設定されていません。"
            );
            return;
        }

        if (buildingsRoot.childCount == 0)
        {
            Debug.LogWarning(
                "Buildings の子にビルがありません。" +
                "先に Generate Buildings を実行してください。"
            );
            return;
        }

        // 実際のビル位置から道路を計算
        CollectBuildingGrid();

        if (verticalRoadXPositions.Count == 0 ||
            horizontalRoadZPositions.Count == 0)
        {
            Debug.LogWarning(
                "ビル間道路を計算できませんでした。"
            );
            return;
        }

        // マップ範囲
        float minX = buildingXPositions[0];
        float maxX =
            buildingXPositions[buildingXPositions.Count - 1];

        float minZ = buildingZPositions[0];
        float maxZ =
            buildingZPositions[buildingZPositions.Count - 1];

        // 4方向になるべく均等に分配
        // 5エリアへ均等に分配
        int baseCount = pairCount / 5;
        int remainder = pairCount % 5;

        int centerCount =
            baseCount + (remainder > 0 ? 1 : 0);

        int leftUpperCount =
            baseCount + (remainder > 1 ? 1 : 0);

        int leftLowerCount =
            baseCount + (remainder > 2 ? 1 : 0);

        int rightUpperCount =
            baseCount + (remainder > 3 ? 1 : 0);

        int rightLowerCount =
            baseCount;

        int generated = 0;

        generated += GenerateForArea(
            Area.CenterCross,
            centerCount,
            minX,
            maxX,
            minZ,
            maxZ,
            generated
        );

        generated += GenerateForArea(
            Area.LeftUpper,
            leftUpperCount,
            minX,
            maxX,
            minZ,
            maxZ,
            generated
        );

        generated += GenerateForArea(
            Area.LeftLower,
            leftLowerCount,
            minX,
            maxX,
            minZ,
            maxZ,
            generated
        );

        generated += GenerateForArea(
            Area.RightUpper,
            rightUpperCount,
            minX,
            maxX,
            minZ,
            maxZ,
            generated
        );

        generated += GenerateForArea(
            Area.RightLower,
            rightLowerCount,
            minX,
            maxX,
            minZ,
            maxZ,
            generated
        );

        Debug.Log(
            $"ジャンプ台ペアを {generated} 組生成しました。"
        );
    }


    // =========================================================
    // 実際のBuilding位置を取得
    // =========================================================

    private void CollectBuildingGrid()
    {
        buildingXPositions.Clear();
        buildingZPositions.Clear();

        foreach (Transform building in buildingsRoot)
        {
            AddUnique(
                buildingXPositions,
                building.position.x
            );

            AddUnique(
                buildingZPositions,
                building.position.z
            );
        }

        buildingXPositions.Sort();
        buildingZPositions.Sort();

        verticalRoadXPositions.Clear();
        horizontalRoadZPositions.Clear();

        // 隣り合うビル列の真ん中
        for (int i = 0;
             i < buildingXPositions.Count - 1;
             i++)
        {
            float middle =
                (
                    buildingXPositions[i] +
                    buildingXPositions[i + 1]
                ) * 0.5f;

            verticalRoadXPositions.Add(middle);
        }

        // 隣り合うビル行の真ん中
        for (int i = 0;
             i < buildingZPositions.Count - 1;
             i++)
        {
            float middle =
                (
                    buildingZPositions[i] +
                    buildingZPositions[i + 1]
                ) * 0.5f;

            horizontalRoadZPositions.Add(middle);
        }
    }


    private void AddUnique(
        List<float> values,
        float value)
    {
        const float tolerance = 0.1f;

        foreach (float existing in values)
        {
            if (Mathf.Abs(existing - value)
                < tolerance)
            {
                return;
            }
        }

        values.Add(value);
    }


    // =========================================================
    // 各エリアへ均等生成
    // =========================================================

    private int GenerateForArea(
        Area area,
        int targetCount,
        float minX,
        float maxX,
        float minZ,
        float maxZ,
        int nameOffset)
    {
        int generated = 0;
        int attempts = 0;

        while (generated < targetCount &&
               attempts < maxAttemptsPerArea)
        {
            attempts++;

            bool verticalRoad =
    Random.value > 0.5f;

            Vector3 center;

            if (area == Area.CenterCross)
            {
                if (verticalRoad)
                {
                    center =
                        GetCenterVerticalRoadPosition(
                            minZ,
                            maxZ
                        );
                }
                else
                {
                    center =
                        GetCenterHorizontalRoadPosition(
                            minX,
                            maxX
                        );
                }
            }
            else
            {
                if (verticalRoad)
                {
                    center =
                        GetVerticalRoadPosition(
                            area,
                            minX,
                            maxX,
                            minZ,
                            maxZ
                        );
                }
                else
                {
                    center =
                        GetHorizontalRoadPosition(
                            area,
                            minX,
                            maxX,
                            minZ,
                            maxZ
                        );
                }
            }

            if (!IsValidArea(center, area))
            {
                continue;
            }

            if (IsInsideCenterIntersection(center))
            {
                continue;
            }

            if (IsNearRoadIntersection(center))
            {
                continue;
            }

            if (!CanPlacePair(center))
            {
                continue;
            }

            Vector3 positionA;
            Vector3 positionB;

            float rotationA;
            float rotationB;

            if (verticalRoad)
            {
                positionA =
                    center +
                    new Vector3(
                        0.0f,
                        0.0f,
                        -pairDistance * 0.5f
                    );

                positionB =
                    center +
                    new Vector3(
                        0.0f,
                        0.0f,
                        pairDistance * 0.5f
                    );

                // / \
                rotationA = 180.0f;
                rotationB = 0.0f;
            }
            else
            {
                positionA =
                    center +
                    new Vector3(
                        -pairDistance * 0.5f,
                        0.0f,
                        0.0f
                    );

                positionB =
                    center +
                    new Vector3(
                        pairDistance * 0.5f,
                        0.0f,
                        0.0f
                    );

                // / \
                rotationA = 270.0f;
                rotationB = 90.0f;
            }

            if (IsOverlappingBuilding(positionA) ||
                IsOverlappingBuilding(positionB))
            {
                continue;
            }

            if (!HasClearRoad(
                    positionA,
                    rotationA) ||
                !HasClearRoad(
                    positionB,
                    rotationB))
            {
                continue;
            }

            int index =
                nameOffset + generated;

            CreatePlatform(
                positionA,
                rotationA,
                $"JumpPair_{index}_A"
            );

            CreatePlatform(
                positionB,
                rotationB,
                $"JumpPair_{index}_B"
            );

            generated++;
        }

        return generated;
    }


    // =========================================================
    // 縦道路候補
    // =========================================================

    private Vector3 GetVerticalRoadPosition(
        Area area,
        float minX,
        float maxX,
        float minZ,
        float maxZ)
    {
        float x =
            verticalRoadXPositions[
                Random.Range(
                    0,
                    verticalRoadXPositions.Count
                )
            ];

        float z = Random.Range(
            minZ + edgeClearance,
            maxZ - edgeClearance
        );

        Vector3 position =
            new Vector3(
                x,
                yPosition,
                z
            );

        return position;
    }


    // =========================================================
    // 横道路候補
    // =========================================================

    private Vector3 GetHorizontalRoadPosition(
        Area area,
        float minX,
        float maxX,
        float minZ,
        float maxZ)
    {
        float x = Random.Range(
            minX + edgeClearance,
            maxX - edgeClearance
        );

        float z =
            horizontalRoadZPositions[
                Random.Range(
                    0,
                    horizontalRoadZPositions.Count
                )
            ];

        return new Vector3(
            x,
            yPosition,
            z
        );
    }


    // =========================================================
    // 指定エリアか
    // =========================================================

    private bool IsValidArea(
    Vector3 position,
    Area area)
    {
        switch (area)
        {
            case Area.CenterCross:
                {
                    // X=0付近 または Z=0付近なら中央十字
                    bool nearVerticalCenterRoad =
                        Mathf.Abs(position.x) <
                        centerClearance;

                    bool nearHorizontalCenterRoad =
                        Mathf.Abs(position.z) <
                        centerClearance;

                    return
                        nearVerticalCenterRoad ||
                        nearHorizontalCenterRoad;
                }


            case Area.LeftUpper:
                return
                    position.x < -centerClearance &&
                    position.z > centerClearance;


            case Area.LeftLower:
                return
                    position.x < -centerClearance &&
                    position.z < -centerClearance;


            case Area.RightUpper:
                return
                    position.x > centerClearance &&
                    position.z > centerClearance;


            case Area.RightLower:
                return
                    position.x > centerClearance &&
                    position.z < -centerClearance;
        }

        return false;
    }

    // =========================================================
    // ビル群内の道路交差点を避ける
    // =========================================================

    private bool IsNearRoadIntersection(
        Vector3 position)
    {
        foreach (float roadX
                 in verticalRoadXPositions)
        {
            if (Mathf.Abs(
                    position.x - roadX)
                > intersectionClearance)
            {
                continue;
            }

            foreach (float roadZ
                     in horizontalRoadZPositions)
            {
                if (Mathf.Abs(
                        position.z - roadZ)
                    < intersectionClearance)
                {
                    return true;
                }
            }
        }

        return false;
    }


    // =========================================================
    // 中央の大十字路を避ける
    // =========================================================

    private bool IsNearCenterIntersection(
        Vector3 position)
    {
        return
            Mathf.Abs(position.x)
                < centerClearance &&
            Mathf.Abs(position.z)
                < centerClearance;
    }


    // =========================================================
    // ビルとの重なり
    // =========================================================

    private bool IsOverlappingBuilding(
        Vector3 position)
    {
        Vector3 center =
            position +
            Vector3.up *
            platformCheckSize.y * 0.5f;

        Bounds platformBounds =
            new Bounds(
                center,
                platformCheckSize
            );

        foreach (Transform building
                 in buildingsRoot)
        {
            Collider collider =
                building.GetComponent<Collider>();

            if (collider == null)
            {
                continue;
            }

            if (platformBounds.Intersects(
                    collider.bounds))
            {
                return true;
            }
        }

        return false;
    }


    // =========================================================
    // ジャンプ後の進行方向が空いているか
    // =========================================================

    private bool HasClearRoad(
        Vector3 position,
        float yRotation)
    {
        Quaternion rotation =
            Quaternion.Euler(
                0.0f,
                yRotation,
                0.0f
            );

        Vector3 forward =
            rotation * Vector3.forward;

        Vector3 checkCenter =
            position +
            forward *
            (roadCheckDistance * 0.5f);

        checkCenter.y =
            yPosition + 3.0f;

        Vector3 checkSize;

        if (yRotation == 90.0f ||
            yRotation == 270.0f)
        {
            checkSize =
                new Vector3(
                    roadCheckDistance,
                    6.0f,
                    roadCheckWidth
                );
        }
        else
        {
            checkSize =
                new Vector3(
                    roadCheckWidth,
                    6.0f,
                    roadCheckDistance
                );
        }

        Bounds roadBounds =
            new Bounds(
                checkCenter,
                checkSize
            );

        foreach (Transform building
                 in buildingsRoot)
        {
            Collider collider =
                building.GetComponent<Collider>();

            if (collider == null)
            {
                continue;
            }

            if (roadBounds.Intersects(
                    collider.bounds))
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // 既存ジャンプ台との距離
    // =========================================================

    private bool CanPlacePair(
        Vector3 position)
    {
        foreach (Transform child
                 in transform)
        {
            Vector3 a =
                child.position;

            Vector3 b =
                position;

            a.y = 0.0f;
            b.y = 0.0f;

            if (Vector3.Distance(a, b)
                < minDistance)
            {
                return false;
            }
        }

        return true;
    }


    // =========================================================
    // 実際にPrefab生成
    // =========================================================

    private void CreatePlatform(
        Vector3 position,
        float yRotation,
        string objectName)
    {
        Quaternion rotation =
            Quaternion.Euler(
                rampAngle,
                yRotation,
                0.0f
            );

        GameObject platform =
            Instantiate(
                jumpingPlatformPrefab,
                position,
                rotation,
                transform
            );

        platform.name = objectName;
    }


    // =========================================================
    // 全ジャンプ台削除
    // =========================================================

    [ContextMenu("Clear Jumping Platforms")]
    public void ClearJumpingPlatforms()
    {
        for (int i =
             transform.childCount - 1;
             i >= 0;
             i--)
        {
            DestroyImmediate(
                transform.GetChild(i).gameObject
            );
        }
    }

    private bool IsInsideCenterIntersection(
    Vector3 position)
    {
        return
            Mathf.Abs(position.x)
                < centerIntersectionClearance &&
            Mathf.Abs(position.z)
                < centerIntersectionClearance;
    }

    private Vector3 GetCenterVerticalRoadPosition(
    float minZ,
    float maxZ)
    {
        float closestX =
            GetClosestToZero(
                verticalRoadXPositions
            );

        float z =
            Random.Range(
                minZ + edgeClearance,
                maxZ - edgeClearance
            );

        return new Vector3(
            closestX,
            yPosition,
            z
        );
    }


    private Vector3 GetCenterHorizontalRoadPosition(
        float minX,
        float maxX)
    {
        float closestZ =
            GetClosestToZero(
                horizontalRoadZPositions
            );

        float x =
            Random.Range(
                minX + edgeClearance,
                maxX - edgeClearance
            );

        return new Vector3(
            x,
            yPosition,
            closestZ
        );
    }

    private float GetClosestToZero(
    List<float> values)
    {
        float closest =
            values[0];

        float closestDistance =
            Mathf.Abs(closest);

        for (int i = 1;
             i < values.Count;
             i++)
        {
            float distance =
                Mathf.Abs(values[i]);

            if (distance <
                closestDistance)
            {
                closest =
                    values[i];

                closestDistance =
                    distance;
            }
        }

        return closest;
    }
}