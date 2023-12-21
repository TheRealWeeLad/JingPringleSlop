using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Unity.VisualScripting;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]

public class FPSController : MonoBehaviour
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 8.0f;
	[Tooltip("Rotation speed of the character")]
	public static float RotationSpeed = 20.0f;
    [Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;
	[Tooltip("Default Field of Vision")]
	public float DefaultFOV = 60;
	[Tooltip("Maximum Field of Vision")]
	public float MaxFOV = 100;

	[Space(10)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;
	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15.0f;

	[Space(10)]
	[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
	public float JumpTimeout = 0f;
	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	public bool Grounded = true;
	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = 0.14f;
	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.5f;
	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Cinemachine")]
	[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
	public GameObject CinemachineCameraTarget;
	[Tooltip("How far in degrees can you move the camera up")]
	public float TopClamp = 90.0f;
	[Tooltip("How far in degrees can you move the camera down")]
	public float BottomClamp = -90.0f;

	// Most recent y position while grounded
	public static float LastGroundedY { get; private set; }

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private Vector3 _velocity;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private const float _terminalVelocity = 53.0f;

	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	// Logistic Function parameters
	const float _steepness = 0.2f;

	private PlayerInput _playerInput;
	private CharacterController _controller;
	private CinemachineVirtualCamera _mainCamera;
	private PlayerInputs _input;

	private const float _threshold = 0.01f;

	private bool IsCurrentDeviceMouse => _playerInput.currentControlScheme == "KeyboardMouse";

	private void Awake()
	{
		// get a reference to our main camera
		if (_mainCamera == null)
		{
			_mainCamera = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CinemachineVirtualCamera>();
		}
		CinemachineCameraTarget = GameObject.FindGameObjectWithTag("PlayerCamera");

		Cursor.lockState = CursorLockMode.Locked;
	}

	private void Start()
	{
		_controller = GetComponent<CharacterController>();
		_playerInput = GetComponent<PlayerInput>();
		_input = GetComponent<PlayerInputs>();

		// reset our timeouts on start
		_jumpTimeoutDelta = JumpTimeout;
		_fallTimeoutDelta = FallTimeout;
	}

	private void Update()
	{
		if (UIManagement.paused && GameManager.CurrentGame.Equals("GlimboKiller")) return;
		JumpAndGravity();
		GroundedCheck();
		Move();
		ChangeFOV();
	}

	private void LateUpdate()
	{
		if (UIManagement.paused && GameManager.CurrentGame.Equals("GlimboKiller")) return;
		CameraRotation();
	}

	private void GroundedCheck()
	{
		// set sphere position, with offset
		Vector3 spherePosition = new(transform.position.x, transform.position.y - 0.5f - GroundedOffset, transform.position.z);
		Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
	}

	private void CameraRotation()
	{
		// if there is an input
		if (_input.look.sqrMagnitude >= _threshold)
		{
			//Don't multiply mouse input by Time.deltaTime
			float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

			_cinemachineTargetPitch -= _input.look.y * RotationSpeed * deltaTimeMultiplier;
			_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

			// clamp our pitch rotation
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// rotate the player left and right
			transform.Rotate(Vector3.up * _rotationVelocity);

			// Update Cinemachine camera target pitch
			CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, transform.eulerAngles.y, 0.0f);
		}
	}

	private void Move()
	{
        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            // move
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

		// set target speed based on move speed
		int multiplier = 1;
		if (_input.sprint) multiplier = 2;
		Vector3 targetSpeed = MoveSpeed * multiplier * inputDirection.normalized;

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

		// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is no input, set the target speed to 0
		if (_input.move == Vector2.zero) targetSpeed = Vector3.zero;

		// a reference to the players current horizontal velocity
		Vector3 currentHorizontalSpeed = new (_controller.velocity.x, 0.0f, _controller.velocity.z);

		float speedOffset = 0.05f;
		float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

		// accelerate or decelerate to target speed
		float parallelSpeed;
		if (targetSpeed == Vector3.zero) parallelSpeed = currentHorizontalSpeed.magnitude;
		else parallelSpeed = Vector3.Dot(currentHorizontalSpeed, targetSpeed) / targetSpeed.magnitude;
		
		if (parallelSpeed < targetSpeed.magnitude - speedOffset || parallelSpeed > targetSpeed.magnitude + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_velocity = Vector3.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
		}
		else
		{
			_velocity = targetSpeed;
		}

		// move the player
		_controller.Move(_velocity * Time.deltaTime + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
	}

	private void JumpAndGravity()
	{
		if (Grounded)
		{
			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			// stop our velocity dropping infinitely when grounded
			if (_verticalVelocity < 0.0f)
			{
				_verticalVelocity = -2f;
			}

			// Jump
			if (_input.jump && _jumpTimeoutDelta <= 0.0f)
			{
				// sqrt(-2gh) = velocity needed to reach height
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			}

			// jump timeout
			if (_jumpTimeoutDelta >= 0.0f)
			{
				_jumpTimeoutDelta -= Time.deltaTime;
			}

			LastGroundedY = transform.position.y;
		}
		else
		{
			// reset the jump timeout timer
			_jumpTimeoutDelta = JumpTimeout;

			// fall timeout
			if (_fallTimeoutDelta >= 0.0f)
			{
				_fallTimeoutDelta -= Time.deltaTime;
			}

			// if we are not grounded, do not jump
			_input.jump = false;
		}

		// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
		if (_verticalVelocity < _terminalVelocity)
		{
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}

	// Change the camera's fov based on speed
	void ChangeFOV()
	{
		// Use logistic curve to calculate fov
		float midpoint = MoveSpeed * 2;
		float fov = (MaxFOV - DefaultFOV) / (1 + Mathf.Exp(_steepness * (midpoint - _velocity.magnitude))) + DefaultFOV;
		_mainCamera.m_Lens.FieldOfView = fov;
	}

	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

	private void OnDrawGizmosSelected()
	{
		Color transparentGreen = new (0.0f, 1.0f, 0.0f, 0.35f);
		Color transparentRed = new (1.0f, 0.0f, 0.0f, 0.35f);

		if (Grounded) Gizmos.color = transparentGreen;
		else Gizmos.color = transparentRed;

		// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
	}
}