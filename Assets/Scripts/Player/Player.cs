using System;
using UnityEngine;

public class Player : MonoBehaviour {

	[SerializeField]
	private PlayerInformation information;

	[SerializeField]
	private BoostBarUI boostUI;

	// 基本速度
	[SerializeField]
	private float baseMoveSpeed = 5;

	// 最高速度
	[SerializeField]
	private float maxMoveSpeed = 15;

	// ブースト時の速度
	[SerializeField]
	private float boostSpeed = 80;

	// ブーストを開始する速度
	[SerializeField]
	private float boostStartSpeed = 60;

	// ブーストをする時間
	[SerializeField]
	private float boostTime = 1;

	// ブーストのクールタイム
	[SerializeField]
	private float boostCoolTime = 5;

	// 加速度
	[SerializeField]
	private float acceleration = 1.0f;

	// 加速度
	[SerializeField]
	private float deceleration = 1.0f;

	// 進行方向回転
	[SerializeField]
	private float rotateSpeed = 10.0f;

	// 空中x軸方向回転速度
	[SerializeField]
	private float airRotateSpeedX = 10.0f;

	// 空中y軸方向回転速度
	[SerializeField]
	private float airRotateSpeedY = 10.0f;

	// トリックアクション成功判定の値
	[SerializeField]
	private float toleranceAngleX = 45.0f;

	[SerializeField]
	private float toleranceAngleY = 45.0f;

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

	// 入力の器
	private float horizontal = 0;
	private float vertical = 0;

	// 地面に付いているかどうか
	public bool isGrounded = true;
	private bool prevIsGrounded = true;

	// ブースト状態かどうか
	private bool isBoostMode = false;
	private bool isBoosCooldown = false;

	// 回転送料
	private float totalAngleX = 0.0f;
	private float totalAngleY = 0.0f;

	private int trickCountX = 0;
	private int trickCountY = 0;

	private float boostTimer = 0;
	private float boostCoolTimer = 0;

	private Vector3 jumpForwardAngle = Vector3.zero;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start() {
		currentSpeed = baseMoveSpeed;

		rb = GetComponent<Rigidbody>();

		if (rb == null) {
			Debug.Log("rigidBodyがアタッチされていません");
		}
	}

	// Update is called once per frame
	void Update() {
		// 入力受付
		InputReception();

		// ブースト関連の時間の計測
		MeasureBoostMode();
	}

	// 遅れて更新
	private void FixedUpdate() {
		// 地面に着いているかの判定を行う
		if (IsGrounded()) {
			isGrounded = true;
			totalAngleX = 0.0f;
			totalAngleY = 0.0f;

			// 前フレームにトリックアクションをしていたら成功判定を行う
			if (!prevIsGrounded) {
				// 成功時は視聴者を増やす / 失敗したら減らす
				information.TrickViewer(IsTrickSuccessful(), trickCountX + 1, trickCountY + 1);
			}

			trickCountX = 0;
			trickCountY = 0;

		} else {
			isGrounded = false;

			// 空中に出たframeならジャンプした際の前方方向を取る
			if (prevIsGrounded) {
				jumpForwardAngle = transform.forward;
				jumpForwardAngle.y = 0.0f;
				jumpForwardAngle = jumpForwardAngle.normalized;
			}
		}

		// 移動
		Move();
		
		// 回転
		Rotate();

		// 回転数のカウント
		RotateCount();

		prevIsGrounded = isGrounded;

		// UIに情報を渡す
		boostUI.SetValue(rb.linearVelocity.magnitude);
	}

	// 移動する
	private void Move() {

		Vector3 velocity = rb.linearVelocity;
		Vector3 direction = transform.forward;

		// 地面なら加速を行う
		if (isGrounded) {
			if (vertical == 1) {
				// 徐々に加速する
				currentSpeed += acceleration * Time.deltaTime;
				currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);

				// ブーストに入る速度に達したら
				if (!isBoosCooldown) {
					if (currentSpeed >= boostStartSpeed) {
						isBoostMode = true;
						Debug.Log("ブースト開始");
					}

					if (isBoostMode) {
						currentSpeed = boostSpeed;
					}
				}

				// 速度を求める
				velocity.x = direction.x * currentSpeed;
				velocity.z = direction.z * currentSpeed;

				rb.linearVelocity = velocity;
			} else {

				// 徐々に減速する
				currentSpeed -= deceleration * Time.deltaTime;
				currentSpeed = Mathf.Max(currentSpeed, 0.0f);

				velocity.x = direction.x * currentSpeed;
				velocity.z = direction.z * currentSpeed;

				rb.linearVelocity = velocity;
			}
		}
	}

	// ブースト関連の時間の計測
	private void MeasureBoostMode() {
		// boostMode時の時間の計測
		if (isBoostMode) {
			boostTimer += Time.deltaTime;
			if (boostTimer >= boostTime) {
				isBoostMode = false;
				isBoosCooldown = true;
				boostTimer = 0;
				Debug.Log("クールダウン中");
			}
		}

		// boostCooldown時の時間の計測
		if (isBoosCooldown) {
			boostCoolTimer += Time.deltaTime;
			if (boostCoolTimer >= boostCoolTime) {
				isBoosCooldown = false;
				boostCoolTimer = 0;
			}
		}
	}

	// 移動方向を取得する
	private void InputReception() {
		horizontal = Input.GetAxisRaw("Horizontal");

		if (Input.GetKey(KeyCode.Space)) {
			vertical = 1;
		} else {
			vertical = 0;
		}
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

	// 空中での回転
	private void Rotate() {
		// ベースの回転速度を設定
		float rotateSpeedX = airRotateSpeedX;
		float rotateSpeedY = rotateSpeed;

		// 空中かどうかで回転するかどうかを決める
		if (!isGrounded) {
			rotateSpeedY = airRotateSpeedY;
		} else {
			rotateSpeedX = 0;
		}

		// 回転量を求める
		float angleX = rotateSpeedX * vertical * Time.fixedDeltaTime;
		float angleY = rotateSpeedY * horizontal * Time.fixedDeltaTime;

		// 回転量を累積
		totalAngleX += angleX;
		totalAngleY += angleY;

		// 回転後のQuaternionを求める
		Quaternion rotation =
			Quaternion.AngleAxis(angleX, transform.right) *
			Quaternion.AngleAxis(angleY, transform.up);

		rb.MoveRotation(rotation * rb.rotation);
	}

	// 何回転したかを取得する
	private void RotateCount() {
		if (!isGrounded) {
			trickCountX = Mathf.FloorToInt(Mathf.Abs(totalAngleX) / 360.0f);
			trickCountY = Mathf.FloorToInt(Mathf.Abs(totalAngleY) / 360.0f);
		}
	}

	// トリックアクションが成功したかどうかの判定を取る
	private bool IsTrickSuccessful() {

		bool isSuccessX = false;
		bool isSuccessY = false;

		//x軸方向のトリックアクションが成功しているかどうかを判定する
		float dotX = Vector3.Dot(transform.up, Vector3.up);
		if (dotX >= Mathf.Cos(toleranceAngleX * Mathf.Deg2Rad)) {
			isSuccessX = true;

		} else {
			Debug.Log("x軸 失敗！");
		}

		// y軸方向のトリックアクションが成功しているかどうかを判定する
		float dotY = Vector3.Dot(transform.forward, jumpForwardAngle);
		if (dotY >= Mathf.Cos(toleranceAngleY * Mathf.Deg2Rad)) {
			isSuccessY = true;
		} else {
			Debug.Log("y軸 失敗！");
		}

		if(isSuccessX && isSuccessY) {
			Debug.Log("トリックアクション成功!!");
		}

		return isSuccessX && isSuccessY;
	}
}
