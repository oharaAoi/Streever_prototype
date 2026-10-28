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

	[SerializeField]
	private SlowMotion slowMotion;

	[SerializeField]
	private GameObject shieldObject;

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

	// 減速度
	[SerializeField]
	private float deceleration = 1.0f;

	// トリックアクション時の加速度
	[SerializeField]
	private float trickAcceleration = 5.0f;

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

	[Header("ボタンを何回押したら縦トリックアクションを行うか")]
	[SerializeField]
	private int spacePutCount = 5;

	[SerializeField]
	private int requiredHoldFrames = 5;

	[SerializeField]
	private int reverseAcceptFrames = 10;

	// トリックアクションをしてxフレーム以内に着地をした化
	[SerializeField]
	private int landingBoostFrame = 30;

	[SerializeField]
	private int landingBoostSpeed = 10;

	// 落下速度
	[SerializeField]
	private float fallSpeed = 10.0f;

	[SerializeField, Header("スナイパーの攻撃を回避した時の増える視聴者数")]
	private int sniperAvoidIncrease = 100;

	[SerializeField, Header("1回のジャンプで何人増えたらバリアをはるか")]
	private int shieledUpNumberOfPeople = 2000;

	[SerializeField]
	private UIDocument groundUI;

	[SerializeField]
	private UIDocument airUI;

	[SerializeField]
	private CameraShake cameraShake;

	// 姿勢を補正するか
	public bool isCorrectingRotation = false;

	// private 
	private Rigidbody rb;

	// トリックアクションをした際に最低限のプラス値
	private int defaultTrickAddCount = 1;

	// 加速度倍率
	private float accelerationRaito = 1.3f;

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

	// 移動方向
	private Vector3 moveDirection;

	// spaceを何回押したか
	private int currentSpacePutCount = 0;

	// 壁に衝突したか
	public Vector3 wallNormal = Vector3.zero;

	// トリックアクション中か
	private bool isTrickPlaying = false;

	// トリックアクションを終了してからのフレームカウント
	private int trickFinishFrameCount = 0;

	// トリックアクションを終了したか
	private bool isFinishTrick = false;

	// 落下するかどうか
	private bool isFall = false;

	private bool isStan = false;
	private float stanTime = 0;
	private float stanTimer = 0;

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

		if (shieldObject) {
			shieldObject.SetActive(false);
		}
		moveRotation = Quaternion.Euler(0.0f, transform.eulerAngles.y, 0.0f);

		currentSpacePutCount = 0;
		isFall = false;
	}

	// Update is called once per frame
	void Update() {
		if (isStan) {
			return;
		}

		if (!isCorrectingRotation) {
			// 入力受付
			InputReception();
		}

		// ブースト関連の時間の計測
		MeasureBoostMode();

		// UIに情報を渡す
		boostUI.SetValue(rb.linearVelocity.magnitude);
		boostUI.SetHighValue(maxMoveSpeed);

		information.UpdateBuff(this);

		if (groundUI != null && airUI != null) {
			if (isGrounded) {

				groundUI.enabled = true;
				airUI.enabled = false;
			} else {
				groundUI.enabled = false;
				airUI.enabled = true;
			}
		}
	}

	// 遅れて更新
	private void FixedUpdate() {

		// 着地判定を行う
		LandingDetection();

		if (information.IsShieldUp(shieledUpNumberOfPeople)) {
			shieldObject.SetActive(true);
		}

		if (isStan) {
			stanTimer += Time.deltaTime;
			if (stanTimer >= stanTime) {
				isStan = false;
				stanTimer = 0;
			}

			Fall();
			return;
		}

		if (isFall) {
			Fall();

		} else {
			// 回転
			MoveRotate();

			// 移動
			Move();

			// トリックアクションの実行
			TrickAction();
		}

		prevIsGrounded = isGrounded;

		if (isFinishTrick) {
			trickFinishFrameCount++;
		}
	}

	// 着地判定処理
	private void LandingDetection() {
		// 地面に着いているかの判定を行う
		if (IsGrounded()) {
			isGrounded = true;
			isFall = false;

			if (isStan) {
				trickCountX = 0;
				trickCountY = 0;
				currentSpacePutCount = 0;
				isHorizontalTrick = false;
				trickFinishFrameCount = 0;
				isFinishTrick = false;
				isTrickPlaying = false;
				animator.SetTrigger("TrickFailuer");
				return;
			}

			// 前フレームにトリックアクションをしていたら成功判定を行う
			if (!prevIsGrounded) {
				// 成功時は視聴者を増やす / 失敗したら減らす
				bool isSuccess = false;
				if (!isTrickPlaying) {
					isSuccess = true;
					Debug.Log("トリックアクション成功");
				} else {
					Debug.Log("トリックアクション失敗");
				}

				// トリックアクションをしてからxフレーム以内に着地をするとブーストをする
				if (trickFinishFrameCount < landingBoostFrame) {
					currentSpeed += landingBoostSpeed;
					Debug.Log("加速しました");
				}
				trickFinishFrameCount = 0;
				isFinishTrick = false;

				information.TrickViewer(isSuccess, trickCountX + defaultTrickAddCount, trickCountY + defaultTrickAddCount);
				isTrickPlaying = false;

				// 失敗したなら失敗アニメーションを再生する
				if (!isSuccess) {
					animator.SetTrigger("TrickFailuer");
				}

				// 姿勢のリセット
				ResetRotation();

				// キーを押した回数を元に戻す
				currentSpacePutCount = 0;

				// トリックアクションを行うフラグもfalseにする
				isHorizontalTrick = false;
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
	}

	// 移動する
	private void Move() {

		Vector3 velocity = rb.linearVelocity;
		Vector3 direction = isGrounded ? transform.forward : moveRotation * Vector3.forward;

		// 地面なら加速を行う
		if (isGrounded) {
			if (vertical == 1) {
				// 徐々に加速する
				currentSpeed += acceleration * accelerationRaito * Time.fixedDeltaTime;

				// 上限を超えたら徐々に減速するようにする
				if (currentSpeed > maxMoveSpeed) {
					currentSpeed = Mathf.MoveTowards(
						currentSpeed,
						maxMoveSpeed,
						deceleration * Time.fixedDeltaTime
					);
				}

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
				currentSpeed -= deceleration * Time.fixedDeltaTime;
				currentSpeed = Mathf.Max(currentSpeed, 0.0f);

				velocity.x = direction.x * currentSpeed;
				velocity.z = direction.z * currentSpeed;

				rb.linearVelocity = velocity;
			}
		} else {
			// 空中での移動速度
			Vector3 horizontalVelocity = direction * currentSpeed;

			// 壁に衝突している場合
			if (wallNormal != Vector3.zero) {
				// 壁に向かう速度成分を除去
				if (Vector3.Dot(horizontalVelocity, wallNormal) < 0.0f) {
					horizontalVelocity =
						Vector3.ProjectOnPlane(horizontalVelocity, wallNormal);
				}
			}

			velocity.x = horizontalVelocity.x;
			velocity.z = horizontalVelocity.z;

			// Y方向の速度は維持
			rb.linearVelocity = velocity;
		}

		moveDirection = direction;
	}

	// 落下処理
	private void Fall() {
		Vector3 velocity = rb.linearVelocity;

		// XZ方向の移動を停止
		velocity.x = 0.0f;
		velocity.z = 0.0f;

		// Y方向に落下速度を加算
		velocity.y -= fallSpeed * Time.fixedDeltaTime;

		rb.linearVelocity = velocity;
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

			if (isTrickPlaying) {
				return;
			}

			if (isFall) {
				return;
			}

			// 縦回転の入力
			if (Input.GetKeyDown(KeyCode.Space)) {
				currentSpacePutCount++;
			}

			// 横回転をするかどうかの入力
			if (CheckReverseInput()) {
				isHorizontalTrick = true;
			}

			// 落下するかどうかの入力
			if (Input.GetButtonDown("Fall")) {
				isFall = true;
			}
		}
	}

	// ハンドルが反対方向に入力されたかを取得する
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
		if (isGrounded || isTrickPlaying) {
			return;
		}

		if (isHorizontalTrick) {
			trickCountX++;
			isTrickPlaying = true;
			trickFinishFrameCount = 0;
			isFinishTrick = false;

			animator.SetTrigger("HorizontalTrick");
			isHorizontalTrick = false;
		}

		if (currentSpacePutCount >= spacePutCount) {
			trickCountY++;
			currentSpacePutCount = 0;
			trickFinishFrameCount = 0;
			isFinishTrick = false;
			animator.SetTrigger("VerticalTrick");
			isTrickPlaying = true;
		}
	}

	// トリックアクションをおこなった時に加速させる
	public void FinishTrick() {
		//currentSpeed += trickAcceleration;
		isTrickPlaying = false;
		isFinishTrick = true;
	}

	// 姿勢を元に戻す
	public void ResetRotation() {
		rotationController.enabled = true;
		rotationController.ResetTimer();
	}

	// playerをスタンさせる
	public void Stan(float _stanTime) {

		if (shieldObject.activeSelf) {
			shieldObject.SetActive(false);
			return;
		}

		if (isTrickPlaying) {
			slowMotion.StartSlowMotion();
			information.IncreaseViewership(sniperAvoidIncrease);

			Debug.Log("スタンを回避しました");
			return;
		}

		if (isStan) { return; }
		isStan = true;
		stanTime = _stanTime;
		stanTimer = 0;

		currentSpeed = 0;

		cameraShake.Shake(_stanTime, 0.3f);

		Debug.Log("スタンしました");
	}

	// ------------------------------------------------------------------------------------------------
	// accessor

	public void SetStanTime(float value) {
		stanTime = value;
	}

	public void SetAccelerationRaito(float value) {
		accelerationRaito = value;
	}

	public void SetDefaultTrickAddCount(int value) {
		defaultTrickAddCount = value;
	}
}
