using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement Settings")]
    [SerializeField]private float walkSpeed = 1;

    [Header("Jump Settings")]
    [SerializeField]private float jumpForce = 30f;
    private int jumpBufferCounter = 0;
    [SerializeField] private int jumpBufferFrames;
    // Coyote time is the time after the player has left the ground that they can still jump
    private float coyoteTimeCounter = 0;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    PlayerStateList playerState;

    private Rigidbody2D player;
    private float xAxis;
    Animator animator;

    public static PlayerController Instance;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Rigidbody2D>();
        playerState = GetComponent<PlayerStateList>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpingState();
        Flip();
        Move();
        Jump();
    }

    void GetInputs()
    {
        xAxis = Input.GetAxis("Horizontal");
    }
    void Flip()
    {
        if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
        }
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
        }
    }

    private void Move()
    {
        player.velocity = new Vector2(xAxis * walkSpeed, player.velocity.y);
        animator.SetBool("Walking", xAxis != 0 && Grounded());
    }

    public bool Grounded()
    {
        if (Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround) 
        || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround) 
        || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    void Jump()
    {
        if(!playerState.jumping){
            // Jump when grounded
            if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
            {
                player.velocity = new Vector2(player.velocity.x, jumpForce);
                playerState.jumping = true;
            }
            else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
            {
                player.velocity = new Vector2(player.velocity.x, jumpForce);
                playerState.jumping = true;
                airJumpCounter++;
            }
        }

        // Half vertical velocity when jump button is released
        if (Input.GetButtonUp("Jump") && player.velocity.y > 0)
        {
            player.velocity = new Vector2(player.velocity.x, player.velocity.y * 0.5f);
            playerState.jumping = false;
        }

        animator.SetBool("Jumping", !Grounded());
    }
    
    void UpdateJumpingState()
    {
        if (Grounded())
        {
            playerState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if(Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }
    }
}
