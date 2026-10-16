using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class Player : MonoBehaviour {

	[SerializeField]
	private PlayerInformation information;

	[SerializeField]
	private BoostBarUI boostUI;

	[SerializeField]
	private PlayerRotationController rotationController;

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

	// 地面との距離
	[SerializeField]
	private float groundCheckDistance = 0.6f;

	// 地面のレイヤー
	[SerializeField]
	private LayerMask groundLayer;

	[SerializeField]
	private float rotationSpeed = 10.0f;

	[SerializeField]
	private BoostCoolTimeBarUI boostCoolTimeBarUI;

	[SerializeField]
	private Animator animator;

	// 姿勢を補正するか
	public bool isCorrectingRotation = false;

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

	private int trickCountX = 0;
	private int trickCountY = 0;

	private float boostTimer = 0;
	private float boostCoolTimer = 0;

	private bool isHorizontalTrick = false;

	[SerializeField]
	private int requiredHoldFrames = 5;

	[SerializeField]
	private int reverseAcceptFrames = 10;

	// 現在押している方向
	private int currentDirection = 0;

	// 押し続けているフレーム数
	private int holdFrames = 0;

	// 最後に入力されたフレーム
	private int lastInputFrame = -1;


	// 反転待ちの方向
	private int waitingDirection = 0;

	private Vector3 jumpForwardAngle = Vector3.zero;

	// 移動方向を決めるY軸回転
	private Quaternion moveRotation = Quaternion.identity;

	private Vector3 moveDirection;

	[Header("ボタンを何回押したら縦トリックアクションを行うか")]
	[SerializeField]
	private int spacePutCount = 5;

	private int currentSpacePutCount = 0;

	// getter関連
	public Vector3 MoveDirection => moveDirection;
	public Quaternion MoveRotation => moveRotation;

	public float BoostTime => boostTime;
	public float BoostCoolTime => boostCoolTime;
	public float BoostTimer => boostTimer;
	public float BoostCoolTimer => boostCoolTimer;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start() {
		currentSpeed = baseMoveSpeed;

		rb = GetComponent<Rigidbody>();

		if (rb == null) {
			Debug.Log("rigidBodyがアタッチされていません");
		}

		moveRotation = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f);

		currentSpacePutCount = 0;
	}

	// Update is called once per frame
	void Update() {
		if (!isCorrectingRotation) {
			// 入力受付
			InputReception();
		}

		// ブースト関連の時間の計測
		MeasureBoostMode();
	}

	// 遅れて更新
	private void FixedUpdate() {
		// 地面に着いているかの判定を行う
		if (IsGrounded()) {
			isGrounded = true;
			
			// 前フレームにトリックアクションをしていたら成功判定を行う
			if (!prevIsGrounded) {
				// 成功時は視聴者を増やす / 失敗したら減らす
				information.TrickViewer(IsTrickSuccessful(), trickCountX + 1, trickCountY + 1);

				// 姿勢のリセット
				ResetRotation();

				// キーを押した回数を元に戻す
				currentSpacePutCount = 0;
			}

			// トリックアクションをした回数を元に戻す
			trickCountX = 0;
			trickCountY = 0;

		} else {
			isGrounded = false;

			// 空中に出たframeならジャンプした際の前方方向を取る
			if (prevIsGrounded) {
				jumpForwardAngle = transform.forward;
				jumpForwardAngle.y = 0.0f;
				jumpForwardAngle = jumpForwardAngle.normalized;

				rotationController.enabled = false;
			}
		}


		// 回転
		MoveRotate();

		// 移動
		Move();

		// トリックアクションの実行
		TrickAction();

		prevIsGrounded = isGrounded;

		// UIに情報を渡す
		boostUI.SetValue(rb.linearVelocity.magnitude);
	}

	// 移動する
	private void Move() {

		Vector3 velocity = rb.linearVelocity;
		Vector3 direction = isGrounded ? transform.forward : moveRotation * Vector3.forward;

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
		} else {
			// 空中ではY軸回転だけを反映した方向に進む
			velocity.x = direction.x * currentSpeed;
			velocity.z = direction.z * currentSpeed;

			rb.linearVelocity = velocity;
		}

		moveDirection = direction;
	}

	// ブースト関連の時間の計測
	private void MeasureBoostMode() {
		// boostMode時の時間の計測
		if (isBoostMode) {
			boostTimer += Time.deltaTime;
			boostCoolTimeBarUI.SetValue(boostTime - boostTimer);
			if (boostTimer >= boostTime) {
				isBoostMode = false;
				isBoosCooldown = true;
				boostTimer = 0;
				boostCoolTimeBarUI.SetHighValue(boostCoolTime);
			}
		}

		// boostCooldown時の時間の計測
		if (isBoosCooldown) {
			boostCoolTimer += Time.deltaTime;
			boostCoolTimeBarUI.SetValue(boostCoolTimer);
			if (boostCoolTimer >= boostCoolTime) {
				isBoosCooldown = false;
				boostCoolTimer = 0;

				boostCoolTimeBarUI.SetHighValue(boostTime);
			}
		}

		
	}

	// 移動方向を取得する
	private void InputReception() {

		// 左右方向の指定
		horizontal = Input.GetAxisRaw("Horizontal");

		// 前方に進行する
		if (isGrounded) {
			if (Input.GetKey(KeyCode.Space)) {
				vertical = 1;
			} else {
				vertical = 0;
			}
		} else {

			if (IsPlayingAnimation()) {
				return;
			}

			if (Input.GetKeyDown(KeyCode.Space)) {
				currentSpacePutCount++;
			}

			isHorizontalTrick = CheckReverseInput();
		}
	}

	private bool CheckReverseInput() {
		int horizontal = (int)Input.GetAxisRaw("Horizontal");
		int frame = Time.frameCount;

		// 前回の入力条件を満たしていて、反対方向が押された場合
		if (waitingDirection != 0) {

			// 制限時間を超えたら無効
			if (frame - lastInputFrame > reverseAcceptFrames) {
				waitingDirection = 0;
			} else if (horizontal == -waitingDirection) {
				waitingDirection = 0;

				// 新しい方向の入力開始
				currentDirection = horizontal;
				holdFrames = 1;
				lastInputFrame = frame;

				return true;
			}
		}

		// 入力中
		if (horizontal != 0) {

			if (horizontal == currentDirection) {
				holdFrames++;
			} else {
				currentDirection = horizontal;
				holdFrames = 1;
			}

			lastInputFrame = frame;
		} else {
			// 一定フレーム以上押していた場合
			if (holdFrames >= requiredHoldFrames) {
				waitingDirection = currentDirection;
			}

			currentDirection = 0;
			holdFrames = 0;
		}

		return false;
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

	// 移動持の回転
	private void MoveRotate() {
		float rotateSpeedY = 0;

		if (isGrounded) {
			rotateSpeedY = rotateSpeed;
		} else {
			rotateSpeedY = airRotateSpeedY;
		}

		float angleY = rotateSpeedY * horizontal * Time.fixedDeltaTime;

		// 移動方向用のY軸回転を更新
		moveRotation =
			Quaternion.AngleAxis(angleY, Vector3.up) * moveRotation;

		// 前転はローカルX軸、左右回転はワールドY軸
		Quaternion yaw =
			Quaternion.AngleAxis(angleY, Vector3.up);

		rb.MoveRotation(yaw * rb.rotation);
	}

	// トリックアクションの実行
	private void TrickAction() {
		if (isGrounded) {
			return;
		}

		if (isHorizontalTrick) {
			trickCountX++;
			animator.SetTrigger("HorizontalTrick");
			isHorizontalTrick = false;
		}

		if (currentSpacePutCount >= spacePutCount) {
			trickCountY++;
			currentSpacePutCount = 0;
			animator.SetTrigger("VerticalTrick");
		}
	}

	// トリックアクションが成功したかどうかの判定を取る
	private bool IsTrickSuccessful() {
		if (IsPlayingAnimation()) {
			Debug.Log("トリックアクション失敗!!!");
			return false;
		}

		Debug.Log("トリックアクション成功!!!");
		return true;
	}

	// 姿勢を元に戻す
	public void ResetRotation() {
		rotationController.enabled = true;
		rotationController.ResetTimer();
	}

	// playerの状態を元に戻す
	private bool IsPlayingAnimation() {
		if (animator == null) {
			return false;
		}

		AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

		return !state.IsName("PlayerIdle");
	}
}
