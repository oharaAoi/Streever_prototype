
using System.Collections;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class NavMeshSceneInitializer : MonoBehaviour
{
    [SerializeField]
    private NavMeshSurface navMeshSurface;

    private IEnumerator Start()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError(
                "NavMeshSurfaceが設定されていません");
            yield break;
        }

        // シーン内の初期化を1フレーム待つ
        yield return null;

        // NavMeshSurfaceを再登録
        navMeshSurface.enabled = false;
        navMeshSurface.enabled = true;

        yield return null;

        NavMeshTriangulation triangulation =
            NavMesh.CalculateTriangulation();

        Debug.Log(
            $"NavMesh頂点数: {triangulation.vertices.Length}"
        );
    }
}
