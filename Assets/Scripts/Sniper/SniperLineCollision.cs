using UnityEngine;

public class SniperLineCollision : MonoBehaviour
{
    
	[SerializeField]
	private LineRenderer lineRenderer;

	[SerializeField]
	private LayerMask buildingLayer;

	private void Update() {
		Vector3 start = lineRenderer.GetPosition(0);
		Vector3 end = lineRenderer.GetPosition(1);

		// ü‚Ì•ûŒü‚Æ’·‚³
		Vector3 direction = end - start;
		float distance = direction.magnitude;

		// Œš•¨‚Æ‚ÌÕ“Ë”»’è
		bool isHit = Physics.Raycast(
			start,
			direction.normalized,
			distance,
			buildingLayer
		);

		// Œš•¨‚É“–‚½‚Á‚Ä‚¢‚½‚çü‚ğ”ñ•\¦
		lineRenderer.enabled = !isHit;

		if (!isHit) {
			lineRenderer.positionCount = 2;
			lineRenderer.SetPosition(0, start);
			lineRenderer.SetPosition(1, end);
		}
	}
}
