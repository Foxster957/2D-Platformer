using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	[Header("Game Manager")]
	public GameManager gameManager;
	[Space]
	
    [Header("Contact Filters")]
    public ContactFilter2D ground;
    public ContactFilter2D leftWall;
    public ContactFilter2D rightWall;
    [Space]

    [Header("Movement")]

    [Tooltip("The top speed at which the player character moves (duh)")]
	[Min(0)]
    public float moveSpeed;

    [Tooltip("Some multiplier that affects acceleration force")]
	[Min(0)]
    public float acceleration;

    [Tooltip("Some multiplier that affects decceleration force")]
	[Min(0)]
    public float decceleration;
	[Space]

	[Tooltip("Some multiplier that affects acceleration force while in air")]
	[Min(0)]
    public float airAcceleration;

    [Tooltip("Some multiplier that affects decceleration force while in air")]
	[Min(0)]
    public float airDecceleration;

    [Tooltip("Power for exponential acceleration")]
	[Min(0)]
    public float velPower;

    [Tooltip("The amount of friction (Revolutionary, I know!)")]
	[Min(0)]
    public float frictionAmount;
    [Space]

    [Header("Jump")]

	[Tooltip("The force of the player character's jump")]
	[Min(0)]
    public float jumpForce;

	[Tooltip("How much to reduce y velocity when jump is released, and the player character id still moving up")]
	[Range(0f, 1f)]
	public float jumpCutMultiplier;
	[Space]

	[Tooltip("If the amount of time passed between leaving the ground and jump input is less than this, a jump will be performed")]
    [Min(0)]
	public float jumpCoyoteTime;

	[Tooltip("If the amount of time passed between jump input and landing on the ground is less than this, a jump will be performed")]
    [Min(0)]
	public float jumpBufferTime;
	[Space]

	[Tooltip("The horizontal force applied when wall-jumping")]
	[Min(0)]
    public float wallJumpForce;

	[Tooltip("A small amount of time after wall jumping when the player is unable to control the character's horizontal movement")]
	[Min(0)]
	public float wallJumpTimeout;
	[Space]

	[Header("Physics modifiers")]

	[Tooltip("The default value to use for the player's Rigidbody2D.gravityScale")]
	[Min(0)]
	public float gravityScale;

	[Tooltip("A multiplier to temporarily increase the player's Rigidbody2D.gravityScale while falling")]
	[Min(0)]
	public float fallGravityMultiplier;

	[Tooltip("A maximum value for the player character's fall speed")]
	[Min(0)]
	public float maxFallSpeed;

	[Tooltip("A maximum value for the player character's fall speed when touching a wall")]
	[Min(0)]
	public float maxSlideSpeed;

    private Rigidbody2D rb;
    private float moveX;
    private float moveY;
    private float lastGroundedTime;
    private float lastJumpTime;
	private float lastWallJumpTime;
    private bool isJumping;
    private bool jumpInputReleased;
	private string lastGroundedType;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        new Vector2();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
		Vector2 value = context.ReadValue<Vector2>();
        moveX = value.x;
        moveY = value.y;	// unused
    }

    public void OnJump(InputAction.CallbackContext context)
    {
		if(context.started)
		{
			// Resets lastJumpTime to some buffer > 0
        	lastJumpTime = jumpBufferTime;
		}
        else if(context.canceled)
		{
			if(rb.velocity.y > 0 && isJumping)
			{
				// Reduces y velocity by some multiplier between 0 and 1
				rb.AddForce(Vector2.down * rb.velocity.y * (1 - jumpCutMultiplier), ForceMode2D.Impulse);
			}

			jumpInputReleased = true;
			lastJumpTime = 0;
		}
    }

    void Jump()
    {
		switch(lastGroundedType)
		{
			case "ground":
				rb.AddForce(jumpForce * Vector2.up, ForceMode2D.Impulse);
				break;

			case "leftWall":
				rb.AddForce(new Vector2(wallJumpForce, jumpForce), ForceMode2D.Impulse);
				lastWallJumpTime = wallJumpTimeout;
				break;

			case "rightWall":
				rb.AddForce(new Vector2(-wallJumpForce, jumpForce), ForceMode2D.Impulse);
				lastWallJumpTime = wallJumpTimeout;
				break;
		}

        // The player was last grounded now
        lastGroundedTime = 0;
        // The player last jumped now
        lastJumpTime = 0;
        isJumping = true;
        jumpInputReleased = false;
    }

	void OnCollisionEnter2D(Collision2D collision)
	{
		if(collision.gameObject.CompareTag("Spikes"))
		{
			gameManager.PlayerDeath();
		}
	}

    void Update()
    {
        #region Timer
        // lastGroundedTime and lastJumpTime are constantly decreasing
        lastGroundedTime -= Time.deltaTime;
        lastJumpTime -= Time.deltaTime;
		lastWallJumpTime -= Time.deltaTime;
        #endregion
    }

    void FixedUpdate()
    {
		#region Ground Check
        if((rb.IsTouching(ground) || rb.IsTouching(leftWall) || rb.IsTouching(rightWall)) && jumpInputReleased)
        {
            // Resets lastGroundedTime to some buffer > 0
            lastGroundedTime = jumpCoyoteTime;
            isJumping = false;
			lastWallJumpTime = 0;
			
			switch(1)
			{
				case 1 when rb.IsTouching(ground):
					lastGroundedType = "ground";
					break;
				case 1 when rb.IsTouching(leftWall):
					lastGroundedType = "leftWall";
					break;
				case 1 when rb.IsTouching(rightWall):
					lastGroundedType = "rightWall";
					break;
			}
        }
        #endregion

        #region Jump
        // If there is still buffer time left in lastGroundedTime and lastJumpTime (and player isn't jumping)
        if(lastGroundedTime > 0 && lastJumpTime > 0 && !isJumping)
        {
            Jump();
        }
        #endregion

		#region Fall Gravity
		if(rb.velocity.y < 0)
		{
			rb.gravityScale = gravityScale * fallGravityMultiplier;
			jumpInputReleased = true;

			if(rb.IsTouching(leftWall) || rb.IsTouching(rightWall))
			{
				rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxSlideSpeed));
			}
			else
			{
				rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -maxFallSpeed));
			}
		}
		else
		{
			rb.gravityScale = gravityScale;
		}
		#endregion

        #region Run
		if(lastWallJumpTime <= 0)
		{
			float targetSpeed = moveX * moveSpeed;
        	float speedDif = targetSpeed - rb.velocity.x;

        	// Multiplier for tweakable acceleration and decceleration
			float accelRate;
			if(rb.IsTouching(ground))
			{
				accelRate = (Mathf.Abs(targetSpeed) > 0.1f) ? acceleration : decceleration;
			}
			else
			{
				accelRate = (Mathf.Abs(targetSpeed) > 0.1f) ? airAcceleration : airDecceleration;
			}

        	// Multiply speedDif with accelRate, then raise to velPower for exponential acceleration
        	// Then multiply by the sign of speedDif, to restore direction
        	float movement = Mathf.Pow(Mathf.Abs(speedDif) * accelRate, velPower) * Mathf.Sign(speedDif);

        	// Multiply movement by Vector2(1, 0) to only affect x axis
        	rb.AddForce(movement * Vector2.right);
		}
        #endregion

        #region Friction
        if(rb.IsTouching(ground) && Mathf.Abs(moveX) < 0.1f)
        {
            float amount = Mathf.Min(Mathf.Abs(rb.velocity.x), Mathf.Abs(frictionAmount));
            amount *= Mathf.Sign(rb.velocity.x);
            rb.AddForce(-amount * Vector2.right, ForceMode2D.Impulse);
        }
        #endregion
    }
}
