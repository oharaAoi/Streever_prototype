using UnityEngine;

public class Player : MonoBehaviour
{

    // 基本速度
    [SerializeField] 
	private float baseMoveSpeed = 5;

	// 最高速度
	[SerializeField]
	private float maxMoveSpeed = 15;

	// 加速度
	[SerializeField]
	private float acceleration = 1.0f;

	// 加速度
	[SerializeField]
	private float deceleration = 1.0f;

	// 進行方向回転
	[SerializeField]
	private float rotateSpeed = 10.0f;

	// 空中方向回転
	[SerializeField]
	private float airRotateSpeed = 10.0f;

	// 地面との距離
	[SerializeField]
	private float groundCheckDistance = 0.6f;

	// 地面のレイヤー
	[SerializeField]
	private LayerMask groundLayer;

	// private 
	private Rigidbody rb;

	// 現在のスピード
	private float currentSpeed = 0;

	private float horizontal = 0;
	private float vertical = 0;

	public bool isGrounded = true;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		currentSpeed = baseMoveSpeed;

		rb = GetComponent<Rigidbody>();

		if (rb == null) {
            Debug.Log("rigidBodyがアタッチされていません");
        }
	}

    // Update is called once per frame
    void Update()
    {
        // 入力受付
        InputReception();

	}

	// 遅れて更新
	private void FixedUpdate() {
		if (IsGrounded()) {
			isGrounded = true;
		} else {
			isGrounded = false;
		}

		// 移動
		Move();

		Vector3 direction =
		transform.right * horizontal;
		RotateToMoveDirection(direction.normalized);

		if(vertical == 1) {
			Debug.Log("縦回転を行います");
			AirRotate();
		}
	}

	// 移動する
	private void Move() {

		Vector3 velocity = rb.linearVelocity;
		Vector3 direction = transform.forward;

		// 地面なら加速を行う
		if (isGrounded) {
			if (Input.GetKey(KeyCode.Space)) {
				// 徐々に加速する
				currentSpeed += acceleration * Time.deltaTime;
				currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);

				// 速度を求める
				velocity.x = direction.x * currentSpeed;
				velocity.z = direction.z * currentSpeed;

				rb.linearVelocity = velocity;
			} else {
				velocity.x = 0.0f;
				velocity.z = 0.0f;

				// 徐々に減速する
				currentSpeed -= deceleration * Time.deltaTime;
				currentSpeed = Mathf.Max(currentSpeed, 0.0f);
			}

		} 
	}

	// 移動方向を取得する
	private void InputReception() {
		horizontal = Input.GetAxisRaw("Horizontal");
		if (!isGrounded) {
			Debug.Log("空中です");
			if (Input.GetKey(KeyCode.Space)) {
				vertical = 1;
			} else {
				vertical = 0;
			}
		} else {
			vertical = 0;
		}
	}

	// 進行方向にキャラを向ける
	public void RotateToMoveDirection(Vector3 moveDirection) {
		if (moveDirection.sqrMagnitude <= 0.01f) {
			return;
		}

		moveDirection.y = 0.0f;
		moveDirection.Normalize();

		Quaternion targetRotation =
			Quaternion.LookRotation(moveDirection);

		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			targetRotation,
			rotateSpeed * Time.deltaTime
		);
	}

	// 地上にいるかの判定を行う
	private bool IsGrounded() {
		return Physics.Raycast(
			transform.position,
			Vector3.down,
			groundCheckDistance,
			groundLayer
		);
	}

	private void AirRotate() {
		float angle = airRotateSpeed * Time.fixedDeltaTime;

		Quaternion rotation =
			Quaternion.AngleAxis(angle, transform.right);

		rb.MoveRotation(rotation * rb.rotation);
	}
}
