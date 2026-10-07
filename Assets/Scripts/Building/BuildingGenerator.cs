using UnityEngine;

public class BuildingGenerator : MonoBehaviour
{
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
                    posX =
                        Mathf.Sign(indexX) *
                        (
                            spacingX * roadScale +
                            (Mathf.Abs(indexX) - 1.0f) * spacingX
                        );
                }

                // Z方向
                if (indexZ != 0.0f)
                {
                    posZ =
                        Mathf.Sign(indexZ) *
                        (
                            spacingZ * roadScale +
                            (Mathf.Abs(indexZ) - 1.0f) * spacingZ
                        );
                }

                // 中央の十字部分にはビルを生成しない
                if (Mathf.Abs(posX) < roadHalfWidth ||
                    Mathf.Abs(posZ) < roadHalfWidth)
                {
                    continue;
                }

                float width =
                    Random.Range(widthRange.x, widthRange.y);

                float depth =
                    Random.Range(depthRange.x, depthRange.y);

                float height =
                    Random.Range(heightRange.x, heightRange.y);

                GameObject building =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);

                building.name = $"Building_{x}_{z}";

                building.transform.SetParent(transform);

                building.transform.localPosition =
                    new Vector3(
                        posX,
                        height * 0.5f,
                        posZ
                    );

                building.transform.localScale =
                    new Vector3(
                        width,
                        height,
                        depth
                    );
            }
        }
    }

    [ContextMenu("Clear Buildings")]
    public void ClearBuildings()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(
                transform.GetChild(i).gameObject
            );
        }
    }
}