﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RaycastController))]
public class Player : MonoBehaviour
{

    /* STATS */
    public int health;
    public bool invincible;
    public bool hurt;
    public float invulnerableTime = 2f;
    public bool ballCaught = false;

    /* MOVEMENT */
    public float gravity; //Amount of velocity y to fall
    public float jumpHeight; //Height of jump
    public float minJumpHeight;
    public float speed; //Horizontal movement speed
    public float wallSlidingSpeed;
    public int wallSlideDirection;

    public int facing = 1;
    //1 means facing right, -1 means left
    public bool isAirborne;
    public bool isWallSliding = false;
    public bool isWallJumping = false;
    public bool isDoubleJumping;
    public Vector2 velocity;
    public Vector2 directionalInput;

    private RaycastController rc;
    private SpriteRenderer render;
    private Animator animator;

    // Use this for initialization
    void Start()
    {
        rc = GetComponent<RaycastController>();
        render = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        health = 3;

        //Kinematic formula, solve for acceleration going down
        gravity = -(2 * 2.5f) / Mathf.Pow(0.36f, 2);

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        GetPlayerInput();

        velocity.x = directionalInput.x * speed;
        velocity.y += gravity * Time.deltaTime; //Gravity makes game obj fall at all 
        if (Mathf.Abs(velocity.x) < 0.1)
        {
            velocity.x = 0;
        }

        HandleWallSlide();

        //Move game object'
        Move(velocity * Time.deltaTime);

        //Checks current state of game obj and makes adjustment to velocity if necessary
        checkState();
    }

    /// <summary>
    /// This trigger will check for collision with traps. Not the level.
    /// If collided with traps, player's health reduces and becomes invulnerable
    /// for a short while.
    /// if item, then pick up
    /// </summary>
    /// <param name="other">Other.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        /*
		if (!invincible) {
			if (other.tag == "Trap") {
				ReceiveDamage ();
			}
		}*/
    }

    public void ReceiveDamage()
    {
        if (!invincible)
        {
            velocity.y = 0;
            animator.Play("player_hurt");
            //Receive damage
            health--;
            Debug.Log("HEALTH: " + health);

            //Makes slight pause and prevent player from moving when hit
            hurt = true;
            Invoke("resetHurt", 0.2f);

            //Become invulnerable for 2 seconds
            invincible = true;
            Invoke("resetInvincible", invulnerableTime);
        }
    }

    /// <summary>
    /// This method checks if the state of the player is airborne and colliding with a wall
    /// </summary>
    public void HandleWallSlide()
    {
        isWallSliding = false;
        //check if there's a wall and not grounded
        if ((rc.collision.right || rc.collision.left) && isAirborne && velocity.y < 0)
        {
            wallSlideDirection = rc.collision.right ? 1 : -1;
            if (directionalInput.x == wallSlideDirection)
            {
                isWallSliding = true;
                velocity.y = -wallSlidingSpeed;
            }
        }
    }

    /// <summary>
    /// Moves the player. Raycast only checks for level collision such as walls and floors
    /// </summary>
    /// <param name="moveAmount">Move amount.</param>
    public void Move(Vector2 moveAmount)
    {
        //Updates raycast position as game object moves.
        rc.UpdateRayOrigins();
        rc.collision.Reset();

        //direction player is facing; right = 1, left = -1
        setFacingDirection();

        //Check collisions - if found, moveAmount velocity will be reduced appropriately
        rc.checkCollisions(ref moveAmount);

        //Actually changing the velocity of game object
        transform.Translate(moveAmount);
    }

    /// <summary>
    /// This method checks the state of the game object after moving and makes any approriate changes 
    /// should it encounter states such as being airborne, grounded or interacting with incoming obstacle.
    /// Called after moving
    /// </summary>
    private void checkState()
    {
        //If grounded, reset airborne and double jump state
        if (rc.collision.below)
        {
            velocity.y = 0f;
            isAirborne = false;
            isDoubleJumping = false;
        }
        else if (!rc.collision.below)
        { //when airborne
            isAirborne = true;
        }
        //		animator.SetBool ("airborne", isAirborne); //Set sprite to airborne animation
        //Falling animation HERE

        //If hit ceiling, set velocity.y to 0 to prevent accumulating
        if (rc.collision.above)
        {
            velocity.y = 0f;
            Debug.Log("Hit ceiling");
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
    }

    /// <summary>
    /// Sets the facing direction of game object
    /// </summary>
    private void setFacingDirection()
    {
        //Keeps game object facing direction when not moving
        if (velocity.x != 0)
        {
            rc.collision.collDirection = (int)Mathf.Sign(velocity.x);
        }

        //Flips sprite
        bool dir = render.flipX ? (directionalInput.x > 0) : (directionalInput.x < 0);
        if (dir)
        {
            render.flipX = !render.flipX;
        }
    }

    /// <summary>
    /// Resets the invincble boolean. Used by OnTriggerEnter2D, to return player to vulnerable state 
    /// after slight moment of invincibility.
    /// </summary>
    private void resetInvincible()
    {
        invincible = false;
    }

    /// <summary>
    /// reset hurt boolean. Used in update(), to allow player to move again.
    /// </summary>
    private void resetHurt()
    {
        hurt = false;
    }

    private void resetWallJump()
    {
        isWallJumping = false;
    }

    /**
	 * USER INPUT
	 * 
	 */
    private void GetPlayerInput()
    {
        this.directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        //		animator.SetFloat ("velocityX", Mathf.Abs (directionalInput.x));

        //Jump
        if (Input.GetKeyDown("j"))
        {
            OnJumpDown();
        }

        if (Input.GetKeyUp("j"))
        {
            OnJumpUp();
        }

        //throw/catch
        if (Input.GetKeyDown("k"))
        {
            if (ballCaught)
            {
                OnThrowDown();
            }
            else
            {
                OnCatchDown();
            }
        }

    }

    private void OnThrowDown()
    {
        //you don't have the ball anymore
        ballCaught = false;
    }

    private void OnCatchDown()
    {
        //make some kind of timing mechanic so that catching is a bit challenging
        ballCaught = true;
    }
    private void OnJumpUp()
    {
        if (velocity.y > 8f)
        {
            velocity.y = 8f;
        }
    }

    private void OnJumpDown()
    {
        if (rc.collision.below)
        {
            isAirborne = true;
            velocity.y = jumpHeight;
        }

        if (isWallSliding)
        {
            isWallJumping = true;
            velocity.x = -wallSlideDirection * 7;
            velocity.y = 10;
            print(velocity.x);
            Invoke("resetWallJump", 0.2f);
        }
        //Double jump
        else if (!isDoubleJumping && !rc.collision.below)
        {
            velocity.y = jumpHeight * .8f;
            isDoubleJumping = true;
        }
    }
}
