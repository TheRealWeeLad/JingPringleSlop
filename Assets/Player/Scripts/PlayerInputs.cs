using UnityEngine;
using UnityEngine.InputSystem;

public enum GrappleState { None, BeginGrapple, Grappling, EndGrapple };

public class PlayerInputs : MonoBehaviour
{
	[Header("Character Input Values")]
	public Vector2 move;
	public Vector2 look;
	public bool sprint;
	public bool jump;
	public GrappleState grappleState = GrappleState.None;

	[Header("Movement Settings")]
	public bool analogMovement;

	[Header("Mouse Cursor Settings")]
	public bool cursorLocked = true;
	public bool cursorInputForLook = true;

	[Header("Pause Menu Object")]
	public GameObject pauseMenuObj;
	[Header("Crosshair Menu Object")]
	public GameObject crossMenuObj;
	[Header("Crosshair Object")]
	public GameObject crosshairObj;

	[Header("Player Input Object")]
	public PlayerInput playerInp;

	[Header("DEBUG")]
	public GameObject enemyPrefab;

	private void Awake()
	{
		RectTransform[] menuObjs = Resources.FindObjectsOfTypeAll<RectTransform>();
		for (int i = 0; i < menuObjs.Length; i++)
		{
			string name = menuObjs[i].name;
			if (name.Equals("Escape Menu")) pauseMenuObj = menuObjs[i].gameObject;
			else if (name.Equals("Crosshair Settings")) crossMenuObj = menuObjs[i].gameObject;
			else if (name.Equals("Crosshair")) crosshairObj = menuObjs[i].gameObject;
		}
		playerInp = GameObject.Find("Player").GetComponent<PlayerInput>();

        // If current game is GlimboKiller, deactivate input to choose abilities
        if (GameManager.CurrentGame.Equals("GlimboKiller")) DeactivateInput();
		// If in editor
		if (Application.isEditor && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("GlimboKiller")) DeactivateInput();
    }

    public void DeactivateInput()
	{
		playerInp.DeactivateInput();
	}
	public void ActivateInput()
	{
		playerInp.ActivateInput();
	}

    public void OnMove(InputAction.CallbackContext context)
	{
		MoveInput(context.ReadValue<Vector2>());
	}

	public void OnSprint(InputAction.CallbackContext context)
	{
		SprintInput(context.ReadValue<float>() == 1);
	}

	public void OnLook(InputAction.CallbackContext context)
	{
		if (cursorInputForLook)
		{
			LookInput(context.ReadValue<Vector2>());
		}
    }

	public void OnGrapple(InputAction.CallbackContext context)
	{
		bool nowGrappling = context.ReadValue<float>() == 1;
		switch (grappleState)
		{
			case GrappleState.None:
				if (nowGrappling) grappleState = GrappleState.BeginGrapple;
				break;
			case GrappleState.Grappling:
				if (!nowGrappling) grappleState = GrappleState.EndGrapple;
				break;
		}
	}

	public void OnJump(InputAction.CallbackContext context)
	{
		bool pressed = context.ReadValue<float>() == 1;
		JumpInput(pressed);
	}

    public void OnPause(InputAction.CallbackContext context)
    {
		bool active = pauseMenuObj.activeSelf;

		if (!crossMenuObj.activeSelf)
        {
            crosshairObj.SetActive(active);
        }
		else
		{
			// Force crosshair to update
			crosshairObj.SetActive(false);
			crosshairObj.SetActive(true);
		}

        pauseMenuObj.SetActive(!active);
		SetCursorState(active);
		UIManagement.TogglePause();
    }

    private void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	}

	private void SprintInput(bool sprinting)
	{
		sprint = sprinting;
	}

	private void LookInput(Vector2 newLookDirection)
	{
		look = newLookDirection;
	}

	private void JumpInput(bool newJumpState)
	{
		jump = newJumpState;
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		SetCursorState(cursorLocked);
	}

	private void SetCursorState(bool newState)
	{
		Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
	}

    // DEBUG
    public void OnDebugSpawn(InputAction.CallbackContext context)
    {
        EnemyManager.SpawnRandom(Vector3.up, Quaternion.identity);
    }
    public void OnRaiseMoney(InputAction.CallbackContext context)
	{
		PlayerStatManager.PlayerStats.Money++;
		PlayerStatUIManager.Instance.UpdateInfo();
	}
    public void OnLowerMoney(InputAction.CallbackContext context)
    {
        PlayerStatManager.PlayerStats.Money--;
        PlayerStatUIManager.Instance.UpdateInfo();
    }
}