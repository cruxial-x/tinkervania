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

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed;
    [SerializeField] private float dashTime;
    [SerializeField] private float dashCooldown;
    [SerializeField] GameObject dashEffect;
    PlayerStateList playerState;
    private Rigidbody2D player;
    private float xAxis, yAxis;
    Animator animator;
    private bool canDash = true;
    private float gravity;
    private bool dashed;

    [Header("Attack Settings")]
    [SerializeField] float damage;
    [SerializeField] Transform SideAttackTransform, UpAttackTransform, DownAttackTransform;
    [SerializeField] Vector2 SideAttackArea, UpAttackArea, DownAttackArea;
    [SerializeField] LayerMask AttackableLayer;
    [SerializeField] GameObject slashEffect;
    bool attack = false;
    float timeBetweenAttack, timeSinceAttack;

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
        gravity = player.gravityScale;
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpingState();
        if (playerState.dashing) return;
        Flip();
        Move();
        Jump();
        StartDash();
        Attack();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(SideAttackTransform.position, SideAttackArea);
        Gizmos.DrawWireCube(UpAttackTransform.position, UpAttackArea);
        Gizmos.DrawWireCube(DownAttackTransform.position, DownAttackArea);
    }

    void GetInputs()
    {
        xAxis = Input.GetAxis("Horizontal");
        yAxis = Input.GetAxis("Vertical");
        attack = Input.GetMouseButtonDown(0);
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

    void StartDash()
    {
        if (canDash && Input.GetButtonDown("Dash") && !dashed)
        {
            StartCoroutine(Dash());
            dashed = true;
        }

        if (dashed && Grounded())
        {
            dashed = false;
        }
    }

    IEnumerator Dash()
    {
        canDash = false;
        playerState.dashing = true;
        animator.SetTrigger("Dashing");
        player.gravityScale = 0;
        player.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        player.gravityScale = gravity;
        playerState.dashing = false;
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
    void Attack()
    {
        timeSinceAttack += Time.deltaTime;
        if(attack && timeSinceAttack >= timeBetweenAttack)
        {
            timeSinceAttack = 0;
            animator.SetTrigger("Attacking");

            if(yAxis == 0 || yAxis < 0 && Grounded())
            {
                Hit(SideAttackTransform, SideAttackArea);
                Instantiate(slashEffect, SideAttackTransform);
            }
            else if(yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea);
                SlshEffectAtAngle(slashEffect, 80, UpAttackTransform);
            }
            else if(yAxis < 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea);
                SlshEffectAtAngle(slashEffect, -90, DownAttackTransform);
            }
        }
    }
    private void Hit(Transform _attackTransform, Vector2 _attackArea)
    {
        Collider2D[] hit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, AttackableLayer);
        if(hit.Length > 0)
        {
            Debug.Log("Hit");
        }
        for(int i = 0; i < hit.Length; i++)
        {
            if (hit[i].GetComponent<Enemy>() != null)
            {
                hit[i].GetComponent<Enemy>().EnemyHit(1);
            }
        }
    }
    void SlshEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
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
