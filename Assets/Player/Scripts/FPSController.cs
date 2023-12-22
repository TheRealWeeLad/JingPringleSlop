using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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
	public float MaxFOV = 90;

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

	[Header("Grapple")]
	[Tooltip("The rope that shoots out when you grapple.")]
	public GameObject RopePrefab;
	[Tooltip("Maximum distance that you can grapple from.")]
	public float MaxDistance = 30;
	[Tooltip("Grapple Strength")]
	public float PullStrength = 10;

	// Store grapple object
	GameObject _grapple;
	Vector3 _grapplePoint;

	// Most recent y position while grounded
	public static float LastGroundedY { get; private set; }

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private Vector3 _velocity; // Player's horizontal velocity
	private float _rotationVelocity;
	private float _verticalVelocity;
	private float _verticalPosLastFrame;
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
		if (UIManagement.paused) return;
		JumpAndGravity();
		GroundedCheck();
		Grapple();
		Move();
		ChangeFOV();
	}

	private void LateUpdate()
	{
		if (UIManagement.paused) return;
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

	void Grapple()
	{
		switch (_input.grappleState)
		{
			case GrappleState.BeginGrapple:
				// Shoot out Grapple
				if (Physics.Raycast(_mainCamera.transform.position, _mainCamera.transform.forward, out RaycastHit hit, MaxDistance, GroundLayers))
				{
					_grapplePoint = hit.point;

                    Vector3 offsetPos = _mainCamera.transform.position + _mainCamera.transform.right;
					Vector3 distanceToHit = _grapplePoint - offsetPos;
					Quaternion rotation = Quaternion.LookRotation(distanceToHit) * Quaternion.Euler(0, 0, 90);
                    _grapple = Instantiate(RopePrefab, offsetPos + distanceToHit / 2, rotation);
					_grapple.transform.localScale = new(0.05f, 0.05f, distanceToHit.magnitude / 6);

                    _input.grappleState = GrappleState.Grappling;
                }
				else
				{
					_input.grappleState = GrappleState.None;
				}
				break;
			case GrappleState.EndGrapple:
				// Retract Grapple
				Destroy(_grapple);

				_input.grappleState = GrappleState.None;
				break;
			case GrappleState.Grappling:
				// Pull into grapple
				Vector3 grappleDir = _grapplePoint - _mainCamera.transform.position;

				Vector3 acceleration = grappleDir.normalized * PullStrength;
				_velocity += new Vector3(acceleration.x, 0, acceleration.z) * Time.deltaTime;
				_verticalVelocity += acceleration.y * Time.deltaTime;

				// Move grapple object
				Vector3 grapplePos = _mainCamera.transform.position + _mainCamera.transform.right;
				Vector3 grappleObjDir = _grapplePoint - grapplePos;
                _grapple.transform.position = grapplePos + grappleObjDir / 2;
				_grapple.transform.rotation = Quaternion.LookRotation(grappleObjDir) * Quaternion.Euler(0, 0, 90);
                _grapple.transform.localScale = new(0.05f, 0.05f, grappleObjDir.magnitude / 6);

                break;
			default:
				break;
		}
	}

	private void Move()
	{
		if (_input.move == Vector2.zero && _input.grappleState.Equals(GrappleState.Grappling))
		{
			// Move and return
            _controller.Move((_velocity + new Vector3(0.0f, _verticalVelocity, 0.0f)) * Time.deltaTime);
			return;
        }

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            // move
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

		if (_input.grappleState.Equals(GrappleState.Grappling))
		{
			// ???? IDFK
		}
		else
		{
            // set target speed based on move speed
            int multiplier = 1;
            if (_input.sprint) multiplier = 2;
            Vector3 targetSpeed = MoveSpeed * multiplier * inputDirection.normalized;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = Vector3.zero;

            float speedOffset = 0.05f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            float parallelSpeed;
            if (targetSpeed == Vector3.zero) parallelSpeed = _velocity.magnitude;
            else parallelSpeed = Vector3.Dot(_velocity, targetSpeed) / targetSpeed.magnitude;

            if (parallelSpeed < targetSpeed.magnitude - speedOffset || parallelSpeed > targetSpeed.magnitude + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _velocity = Vector3.Lerp(_velocity, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
            }
            else
            {
                _velocity = targetSpeed;
            }
        }
		// move the player
		_controller.Move((_velocity + new Vector3(0.0f, _verticalVelocity, 0.0f)) * Time.deltaTime);
    }

    private void JumpAndGravity()
	{
		// Check if head is hitting something
        if (Physics.CheckSphere(_mainCamera.transform.position + transform.up * 0.3f, GroundedRadius * 0.8f, GroundLayers, QueryTriggerInteraction.Ignore)
			&& _verticalPosLastFrame < transform.position.y)
        {
            _verticalVelocity = 0;
        }

        if (_input.grappleState.Equals(GrappleState.Grappling)) return;

        if (Grounded)
		{
			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			// stop our velocity dropping infinitely when grounded
			if (_verticalVelocity < 0.0f)
			{
				_verticalVelocity = 0f;
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

		_verticalPosLastFrame = transform.position.y;
	}

	// Change the camera's fov based on speed
	void ChangeFOV()
	{
		// Use logistic curve to calculate fov
		float midpoint = MoveSpeed * PullStrength * 0.1f;
		Vector3 totalVel = _velocity + new Vector3(0, _verticalVelocity, 0);
		float fov = (MaxFOV - DefaultFOV) / (1 + Mathf.Exp(_steepness * (midpoint - totalVel.magnitude))) + DefaultFOV;
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
		// and head hitting collider
		Gizmos.DrawSphere(Camera.main.transform.position + transform.up * 0.3f, GroundedRadius * 0.8f);
	}
}