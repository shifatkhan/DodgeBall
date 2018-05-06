﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MyController2D))]
[RequireComponent(typeof(MyPlayerInput))]
public class MyPlayer : MonoBehaviour
{
    // Identity for player 1 or player 2.
    private int playerID = 1;

    // Delta time in which the player must catch the ball.
    public readonly float CATCH_TIME = 0.5f;
    // Ball Touch is when the ball initially touches the player.
    // Ball caught depicts wether the player is holding the ball or not.
    public bool ballTouch = false;
    public bool ballCaught = false;
    public float ballHitPushBack = 4f;
    public GameObject ball = null;
    private Vector2 ballBounceDirection;
    
    public bool invincible = false;
    public float invulnerableTime = 2f;

    public float maxJumpHeight = 2.5f;
    public float minJumpHeight = 1f;
    public float timeToJumpApex = .32f;
    private float accelerationTimeAirborne = .2f;
    private float accelerationTimeGrounded = .1f;
    public float moveSpeed = 7f;

    public float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    public Vector3 velocity;
    private float velocityXSmoothing;
    public bool isDoubleJumping;

    public bool isDashing;
    public float dashDistance = 21f;
    private float timeToReachDashVelocity = .8f;

    public bool isWallSliding;
    public bool isWallJumping;
    public int wallSlideDirection; // sliding left or right
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpDistance;

    public Vector2 directionalInput;

    private MyController2D rc;
    private Animator animator;
    private SpriteRenderer render;

    // Use this for initialization
    void Start()
    {
        rc = GetComponent<MyController2D>();
        animator = GetComponent<Animator>();
        render = GetComponent<SpriteRenderer>();

        //Kinematic formula, solving for acceleration.
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
        wallJumpDistance = new Vector2(7, 15);
    }

    void FixedUpdate()
    {
        if (!ballTouch)
        {
            CalculateVelocity();
        }

        rc.Move(velocity * Time.deltaTime, directionalInput);
        flipSprite();

        checkState();
    }

    private void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed;
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (rc.collisions.below ? accelerationTimeGrounded : accelerationTimeAirborne));
        velocity.y += gravity * Time.deltaTime;

        if (Mathf.Abs(velocity.x) < 0.1)
        {
            velocity.x = 0;
        }
    }

    private void checkState()
    {
        //Prevent velocity from accumulating when standing on platform or hitting a ceiling
        if (rc.collisions.above || rc.collisions.below)
        {
            velocity.y = 0f;
        }

        //When airborne and player collides horizontally with/without a dash, reset velocity to 0; prevents bouncing
        if ((rc.collisions.left || rc.collisions.right) && !rc.collisions.below && !isDashing)
        {
            velocity.x = (rc.collisions.faceDir == 1) ? 0 : -0.001f;
        }

        //Apparantly, Color isn't something you can modify like transform.position
        //Reduce transparency by half when hurt.
        Color c = render.color;
        if (invincible)
        {
            c.a = 0.5f;
        }
        else
        {
            c.a = 1f;
        }
        render.color = c;

        //Check if should wall slide
        CheckWallSlide();

        CheckGrounded();
    }

    private void CheckWallSlide()
    {
        wallSlideDirection = (rc.collisions.left) ? -1 : 1;
        isWallSliding = false;
        if ((rc.collisions.left || rc.collisions.right) && !rc.collisions.below && velocity.y < 0 && directionalInput.x == wallSlideDirection)
        {
            isWallSliding = true;
            isDoubleJumping = false;
            //slows down descent when sliding on wall
            if (velocity.y < -wallSlideSpeed)
            {
                velocity.y = -wallSlideSpeed;
                Debug.Log("Wall Slide");
            }
        }
    }

    private void CheckGrounded()
    {
        //When landing, reset double jump
        if (rc.collisions.below)
        {
            //animator.SetBool("grounded", true);
            isDoubleJumping = false;
        }
        else
        {
            //animator.SetBool("grounded", false);
            //If in any situation where player isn't grounded, then he is not standing in a platform;
            rc.collisions.standingOnPlatform = false;
        }
    }

    private void flipSprite()
    {
        bool flipSprite = render.flipX ? (directionalInput.x > 0) : (directionalInput.x < 0);
        if (flipSprite)
        {
            render.flipX = !render.flipX;
        }
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input;
    }

    public void OnJumpInputDown()
    {

        if (isWallSliding)
        {
            velocity.x = -wallSlideDirection * wallJumpDistance.x;
            velocity.y = wallJumpDistance.y;
            isDoubleJumping = false;
            isWallJumping = true;
            Invoke("resetWallJumping", 0.2f);
            Debug.Log("Wall Jump");
        }

        //Going down through platforms
        if (rc.collisions.standingOnPlatform && directionalInput.y == -1)
        {
            rc.collisions.fallingThroughPlatform = true;
            Debug.Log("Going down platform");
        }
        else if (rc.collisions.below)
        {
            Debug.Log("Jump");
            velocity.y = maxJumpVelocity;
            isDoubleJumping = false;
        }

        //Double jump
        if (!isDoubleJumping && !rc.collisions.below && !isWallSliding && !rc.collisions.standingOnPlatform)
        {
            velocity.y = maxJumpVelocity * 0.75f;
            isDoubleJumping = true;
            Debug.Log("Double jump");
        }
    }

    public void OnJumpInputUp()
    {
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity;
            Debug.Log("Short Jump");
        }
    }

    public void OnDashInputDown()
    {
        if (!isDashing && rc.collisions.below)
        {
            velocity.x = (rc.collisions.faceDir * dashDistance) / timeToReachDashVelocity;
            isDashing = true;
            Debug.Log("Dash");
        }
    }

    private void resetWallJumping()
    {
        isWallJumping = false;
    }

    public int GetPlayerID()
    {
        return this.playerID;
    }

    public void SetPlayerID(int playerID)
    {
        if (playerID < 1 || playerID > 2)
        {
            throw new System.Exception("Player's ID must be within 1 or 2.");
        }
        this.playerID = playerID;
    }

    /// <summary>
	/// Resets the invincble boolean. Used by DetermineIfBallNotCaught, to return player to vulnerable state 
	/// after slight moment of invincibility.
	/// </summary>
	private void resetInvincible()
    {
        this.invincible = false;
    }

    private void DetermineIfBallNotCaught()
    {
        if (!ballCaught && ballTouch)
        {
            // Player got hit (didn't catch the ball).
            Debug.Log("Player " + this.playerID + " got HIT!");

            // Bounce the ball off the player appropriately
            this.ballBounceDirection = this.ball.GetComponent<BallController>().throwDirection;
            this.ballBounceDirection = ballBounceDirection.normalized * this.ball.GetComponent<BallController>().throwForce;
            this.ballBounceDirection.x *= -1;
            this.ball.GetComponent<Rigidbody2D>().AddForce(ballBounceDirection);

            switch (playerID)
            {
                case 1:
                    velocity.x = -ballHitPushBack;
                    break;
                case 2:
                    velocity.x = ballHitPushBack;
                    break;
                default:
                    Debug.LogError("Hit player with unknown playerID!");
                    break;
            }

            this.ball = null;

            //Become invulnerable for 2 seconds
            invincible = true;
            Invoke("resetInvincible", invulnerableTime);
        }

        this.ballTouch = false;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Ball" 
            && collision.gameObject.GetComponent<BallController>().throwerId != this.playerID
            && collision.gameObject.GetComponent<BallController>().throwerId != -1
            && !invincible)
        {
            this.ball = collision.gameObject;

            // Stop movement of the ball.
            this.ball.GetComponent<Rigidbody2D>().velocity = Vector2.zero;

            // Stop movement of the player.
            this.velocity = Vector2.zero;
            SetDirectionalInput(Vector2.zero);

            this.ballTouch = true;
            Invoke("DetermineIfBallNotCaught", CATCH_TIME);
        }
    }

}
