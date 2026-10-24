
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class GroundSlope : MonoBehaviour
{
    [Header("地面の分割数")]
    [SerializeField, Range(10, 300)]
    private int xSegments = 220;

    [SerializeField, Range(10, 200)]
    private int zSegments = 100;

    [Header("上り坂（ワールドX座標）")]
    [SerializeField]
    private float uphillStart = -350f;

    [SerializeField]
    private float uphillEnd = -200f;

    [Header("下り坂（ワールドX座標）")]
    [SerializeField]
    private float downhillStart = 150f;

    [SerializeField]
    private float downhillEnd = 300f;

    [Header("高い平地の高さ")]
    [SerializeField, Min(0f)]
    private float plateauHeight = 15f;

    private Mesh generatedMesh;
    private Mesh originalMesh;
    private int previousHash;

    // 地面の高さを取得する
    public float GetHeightAt(float worldX, float worldZ)
    {
        MeshCollider meshCollider =
            GetComponent<MeshCollider>();

        if (meshCollider != null &&
            meshCollider.sharedMesh != null)
        {
            Bounds bounds = meshCollider.bounds;

            Ray ray = new Ray(
                new Vector3(
                    worldX,
                    bounds.max.y + 100f,
                    worldZ
                ),
                Vector3.down
            );

            if (meshCollider.Raycast(
                ray,
                out RaycastHit hit,
                bounds.size.y + 200f))
            {
                return hit.point.y;
            }
        }

        return transform.position.y;
    }

    private void OnEnable()
    {
        MeshFilter filter = GetComponent<MeshFilter>();

        if (originalMesh == null &&
            filter.sharedMesh != generatedMesh)
        {
            originalMesh = filter.sharedMesh;
        }

        Rebuild();
    }

    private void Update()
    {
        if (GetSettingsHash() != previousHash)
        {
            Rebuild();
        }
    }

    private int GetSettingsHash()
    {
        unchecked
        {
            int hash = 17;

            hash = hash * 31 + xSegments;
            hash = hash * 31 + zSegments;

            hash = hash * 31 + uphillStart.GetHashCode();
            hash = hash * 31 + uphillEnd.GetHashCode();

            hash = hash * 31 + downhillStart.GetHashCode();
            hash = hash * 31 + downhillEnd.GetHashCode();

            hash = hash * 31 + plateauHeight.GetHashCode();

            hash = hash * 31 +
                transform.localToWorldMatrix.GetHashCode();

            return hash;
        }
    }

    // 0～1で滑らかに変化する補間
    private float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);

        return t * t * (3f - 2f * t);
    }

    // ワールドX座標から高さを計算
    private float HeightOffset(float worldX)
    {
        float uphill = Smooth01(
            Mathf.InverseLerp(
                uphillStart,
                uphillEnd,
                worldX
            )
        );

        float downhill = Smooth01(
            Mathf.InverseLerp(
                downhillStart,
                downhillEnd,
                worldX
            )
        );

        return plateauHeight * (uphill - downhill);
    }

    private void Rebuild()
    {
        if (uphillEnd <= uphillStart ||
            downhillStart < uphillEnd ||
            downhillEnd <= downhillStart)
        {
            Debug.LogWarning(
                "GroundSlope: 坂道の座標順序を確認してください。",
                this
            );
            return;
        }

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshCollider meshCollider =
            GetComponent<MeshCollider>();

        Mesh newMesh = new Mesh();

        newMesh.name = "Generated Ground Slope";
        newMesh.hideFlags = HideFlags.DontSave;

        int vertexCount =
            (xSegments + 1) * (zSegments + 1);

        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];

        int[] triangles =
            new int[xSegments * zSegments * 6];

        // 頂点生成
        for (int z = 0; z <= zSegments; z++)
        {
            for (int x = 0; x <= xSegments; x++)
            {
                int index = z * (xSegments + 1) + x;

                float u = (float)x / xSegments;
                float v = (float)z / zSegments;

                Vector3 localPosition = new Vector3(
                    Mathf.Lerp(-5f, 5f, u),
                    0f,
                    Mathf.Lerp(-5f, 5f, v)
                );

                Vector3 worldPosition =
                    transform.TransformPoint(localPosition);

                // 上り坂・平地・下り坂
                worldPosition.y +=
                    HeightOffset(worldPosition.x);

                vertices[index] =
                    transform.InverseTransformPoint(worldPosition);

                uvs[index] = new Vector2(u, v);
            }
        }

        // 三角形生成
        int triangleIndex = 0;

        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int a = z * (xSegments + 1) + x;
                int b = a + 1;
                int c = a + xSegments + 1;
                int d = c + 1;

                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;

                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = d;
            }
        }

        newMesh.vertices = vertices;
        newMesh.uv = uvs;
        newMesh.triangles = triangles;

        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();

        Mesh oldMesh = generatedMesh;
        generatedMesh = newMesh;

        filter.sharedMesh = generatedMesh;

        // Colliderも地面に合わせる
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = generatedMesh;

        previousHash = GetSettingsHash();

        if (oldMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(oldMesh);
            }
            else
            {
                DestroyImmediate(oldMesh);
            }
        }
    }

    private void OnDisable()
    {
        if (generatedMesh == null)
            return;

        MeshFilter filter = GetComponent<MeshFilter>();
        MeshCollider meshCollider =
            GetComponent<MeshCollider>();

        if (filter.sharedMesh == generatedMesh)
        {
            filter.sharedMesh = originalMesh;
        }

        if (meshCollider.sharedMesh == generatedMesh)
        {
            meshCollider.sharedMesh = originalMesh;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = null;
    }
}
