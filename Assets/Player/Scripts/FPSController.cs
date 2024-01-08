using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

using ExtensionMethods;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]

public class FPSController : MonoBehaviour
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float MoveSpeed = 8.0f;
	[Tooltip("Character's side-to-side move speed in new movement system")]
	public float SideMoveSpeed = 3.0f;
	[Tooltip("Rotation speed of the character")]
	public static float RotationSpeed = 20.0f;
    [Tooltip("Acceleration and deceleration")]
	public float OldSpeedChangeRate = 7.0f;
	[Tooltip("New Acceleration and deceleration")]
	public float NewSpeedChangeRate = 30.0f;
	[Tooltip("How fast player decelerates on ground")]
	public float FrictionStrength = 50.0f;
	[Tooltip("Maneuverability in the air")]
	public float MidairControl = 0.3f;
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

	// grapple
	GameObject _grapple;
	Vector3 _grapplePoint;
	float _maxGrappleDist = Mathf.Infinity;

	// Most recent y position while grounded
	public static float LastGroundedY { get; private set; }

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private Vector3 _velocity;
	private float _sideVelocity;
	private float _speedChangeRate;
	private float _rotationVelocity;
	private float _velocityLastFrame;
	private float _verticalPosLastFrame;

	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	// FOV
	const float _steepness = 0.2f;
	bool _smoothing;
	bool _smoothingLastFrame;
	float _smoothingTime;
	float _smoothingMaxTime;

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

		// Update velocity last frame
		_velocityLastFrame = _velocity.magnitude;

		// Update speed in player stats
		if (PlayerStatUIManager.Instance) // Make sure HUD elements are loaded
		{
            PlayerStatManager.PlayerStats.Speed = _velocity.magnitude;
            PlayerStatUIManager.Instance.UpdateInfo();
        }
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

					// Lock grapple length to initial length
					_maxGrappleDist = distanceToHit.magnitude;

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
				// Pull into grapple, keeping grapple length under maximum
				Vector3 grappleVec = _grapplePoint - _mainCamera.transform.position;
				Vector3 grappleDir = grappleVec.normalized;
				Vector3 acceleration = grappleDir * PullStrength;
				// Remove parallel component of velocity to maintain proper grapple length
				if (grappleVec.magnitude >= _maxGrappleDist)
				{
					float parComp = Vector3.Dot(grappleDir, _velocity);
					if (parComp < 0) _velocity -= parComp * grappleDir;

					// Smoothly update FOV to compensate for instant change in velocity
					_smoothing = true;
                }

				_velocity += acceleration * Time.deltaTime;

				// Move grapple object
				Vector3 grapplePos = _mainCamera.transform.position + _mainCamera.transform.right;
				Vector3 grappleObjDir = _grapplePoint - grapplePos;
                _grapple.transform.position = grapplePos + grappleObjDir / 2;
				_grapple.transform.rotation = Quaternion.LookRotation(grappleObjDir) * Quaternion.Euler(0, 0, 90); // Original mesh offset by -90deg along z-axis
                _grapple.transform.localScale = new(0.05f, 0.05f, grappleObjDir.magnitude / 6); // Original mesh is 6m long

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
            _controller.Move(_velocity * Time.deltaTime);
			return;
        }

		// Reduce maneuverability in the air
		if (Grounded && _input.grappleState.Equals(GrappleState.None)) _speedChangeRate = NewSpeedChangeRate;
		else _speedChangeRate = NewSpeedChangeRate * MidairControl;

        // normalise input direction
        Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_input.move != Vector2.zero)
        {
            // move
            inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;
        }

        /* REWORKED HORIZONTAL MOVEMENT */
        Vector3 acceleration = Vector3.zero;
		Vector3 horizontalVel = _velocity.PlanarComponent(transform.up);

		// Forward/Backward movement
		ushort multiplier = 1;
		if (_input.sprint) multiplier = 2;
		float forwardAccel = multiplier * SpeedChangeRate(horizontalVel.magnitude, _speedChangeRate);
		acceleration += forwardAccel * _input.move.y * transform.forward;

		// Velocity should rotate towards player's view direction
		Vector3 desiredVelocity = Vector3.Dot(horizontalVel, transform.forward) * transform.forward;
		_velocity = Vector3.Lerp(_velocity, desiredVelocity + _velocity.y * transform.up, Time.deltaTime * _speedChangeRate);

		// Apply Friction if on ground and not moving parallel to velocity
		if (Grounded && Vector3.Dot(inputDirection, _velocity) <= 0) acceleration -= FrictionStrength * _velocity.PlanarComponent(transform.up).normalized;

		// Side to side movement
		float targetSideSpeed = SideMoveSpeed;
		if (Mathf.Approximately(_input.move.x, 0)) targetSideSpeed = 0;
		float speedOffset = 0.05f;
        if (_sideVelocity < targetSideSpeed - speedOffset || _sideVelocity > targetSideSpeed + speedOffset)
        {
            // Interpolate side velocity towards SideMoveSpeed
            _sideVelocity = Mathf.Lerp(_sideVelocity, targetSideSpeed * _input.move.x, Time.deltaTime * _speedChangeRate);
        }
        else _sideVelocity = targetSideSpeed;

        // Update velocity
        _velocity += acceleration * Time.deltaTime;

		/* OLD HORIZONTAL MOVEMENT
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

		// If moving forward at greater than target speed, only change direction
		if (parallelSpeed > targetSpeed.magnitude + speedOffset && targetSpeed != Vector3.zero)
		{
			targetSpeed *= parallelSpeed / targetSpeed.magnitude;
		}
        if (parallelSpeed < targetSpeed.magnitude - speedOffset || parallelSpeed > targetSpeed.magnitude + speedOffset)
        {
			// Only interpolate velocity parallel to xz-plane
            _velocity = Vector3.Lerp(_velocity, targetSpeed * inputMagnitude + _velocity.y * Vector3.up, Time.deltaTime * _speedChangeRate);
        }
        else _velocity = targetSpeed;
		*/

		// move the player
		_controller.Move((_velocity + _sideVelocity * transform.right) * Time.deltaTime);
    }
	float SpeedChangeRate(float speed, float maxRate) // magic values found through experimentation (desmos :D)
	{
		float a = maxRate;
		float b = 2.5f * a;
		// Equations found from -dx^2 + a = b/x
		float d = 4 * Mathf.Pow(a, 3) / (27 * b * b); // 4a^3/27b^2
		float c = Mathf.Pow(b / (2 * d), 1 / 3); // (b/2d)^1/3

		if (speed < c) return -d * speed * speed + a; // Negative Quadratic
		else return b / speed; // Inverse
	}

    private void JumpAndGravity()
	{
		// Check if head is hitting something
        if (Physics.CheckSphere(_mainCamera.transform.position + transform.up * 0.3f, GroundedRadius * 0.8f, GroundLayers, QueryTriggerInteraction.Ignore)
			&& _verticalPosLastFrame < transform.position.y)
        {
            _velocity.y = 0;

			// Smoothly update FOV to compensate for instant change in velocity
			_smoothing = true;
        }

        // Disables gravity when grappling
        // if (_input.grappleState.Equals(GrappleState.Grappling)) return;

        if (Grounded)
		{
			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			// stop our velocity dropping infinitely when grounded
			if (_velocity.y < 0.0f)
			{
				_velocity.y = 0f;
                // Smoothly update FOV to compensate for instant change in velocity
                _smoothing = true;
			}

			// Jump
			if (_input.jump && _jumpTimeoutDelta <= 0.0f)
			{
				// sqrt(-2gh) = velocity needed to reach height
				_velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
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

		// apply gravity over time
		_velocity.y += Gravity * Time.deltaTime;

		_verticalPosLastFrame = transform.position.y;
	}

	// Change the camera's fov based on speed
	void ChangeFOV()
	{
		// Use logistic curve to calculate fov
		float midpoint = MoveSpeed * PullStrength * 0.05f;
		float fov = (MaxFOV - DefaultFOV) / (1 + Mathf.Exp(_steepness * (midpoint - _velocity.magnitude))) + DefaultFOV;
		if (_smoothing)
        {
            if (!_smoothingLastFrame) // Set up values
			{
                _smoothingTime = 0;
                // Calculate time needed for smooth change using arctan function
                float speedDiff = Mathf.Abs(_velocity.magnitude - _velocityLastFrame);
                _smoothingMaxTime = 2 / Mathf.PI * Mathf.Atan(0.5f * speedDiff); // Capped at 1 second
            }

            if (_smoothingTime < _smoothingMaxTime)
            {
                fov = Mathf.Lerp(_mainCamera.m_Lens.FieldOfView, fov, _smoothingTime / _smoothingMaxTime);
                _smoothingTime += Time.deltaTime;
            }
            else _smoothing = false;
        }

		// Set FOV
        _mainCamera.m_Lens.FieldOfView = fov;

        _smoothingLastFrame = _smoothing;
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