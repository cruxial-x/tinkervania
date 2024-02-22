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
    // Coyote time is the time after the playerRb has left the ground that they can still jump
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
    [HideInInspector] public PlayerStateList playerState;
    private Rigidbody2D playerRb;
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

    [Header("Recoil")]
    [SerializeField] int recoilXSteps = 5;
    [SerializeField] int recoilYSteps = 5;
    [SerializeField] float recoilXSpeed = 100;
    [SerializeField] float recoilYSpeed = 100;
    int stepsXRecoiled, stepsYRecoiled;

    public static PlayerController Instance;

    [Header("Health Settings")]
    public int health;
    public int maxHealth;

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
        playerRb = GetComponent<Rigidbody2D>();
        playerState = GetComponent<PlayerStateList>();
        animator = GetComponent<Animator>();
        gravity = playerRb.gravityScale;
        health = maxHealth;
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
    private void FixedUpdate()
    {
        if (playerState.dashing) return;
        Recoil();
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
        attack = Input.GetButtonDown("Attack");
    }
    void Flip()
    {
        if (xAxis > 0)
        {
            transform.localScale = new Vector2(1, transform.localScale.y);
            playerState.lookingRight = true;
        }
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-1, transform.localScale.y);
            playerState.lookingRight = false;
        }
    }

    private void Move()
    {
        playerRb.velocity = new Vector2(xAxis * walkSpeed, playerRb.velocity.y);
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
        playerRb.gravityScale = 0;
        playerRb.velocity = new Vector2(transform.localScale.x * dashSpeed, 0);
        if (Grounded()) Instantiate(dashEffect, transform);
        yield return new WaitForSeconds(dashTime);
        playerRb.gravityScale = gravity;
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
                Hit(SideAttackTransform, SideAttackArea, ref playerState.recoilingX, recoilXSpeed);
                Instantiate(slashEffect, SideAttackTransform);
            }
            else if(yAxis > 0)
            {
                Hit(UpAttackTransform, UpAttackArea, ref playerState.recoilingY, recoilYSpeed);
                SlshEffectAtAngle(slashEffect, 80, UpAttackTransform);
            }
            else if(yAxis < 0 && !Grounded())
            {
                Hit(DownAttackTransform, DownAttackArea, ref playerState.recoilingY, recoilYSpeed);
                SlshEffectAtAngle(slashEffect, -90, DownAttackTransform);
            }
        }
    }
    private void Hit(Transform _attackTransform, Vector2 _attackArea, ref bool _recoilDir, float _recoilStrength)
    {
        Collider2D[] hit = Physics2D.OverlapBoxAll(_attackTransform.position, _attackArea, 0, AttackableLayer);
        if(hit.Length > 0)
        {
            _recoilDir = true;
        }
        for(int i = 0; i < hit.Length; i++)
        {
            if (hit[i].GetComponent<Enemy>() != null)
            {
                hit[i].GetComponent<Enemy>().EnemyHit(damage, (transform.position - hit[i].transform.position).normalized, _recoilStrength);
            }
        }
    }
    void SlshEffectAtAngle(GameObject _slashEffect, int _effectAngle, Transform _attackTransform)
    {
        _slashEffect = Instantiate(_slashEffect, _attackTransform);
        _slashEffect.transform.eulerAngles = new Vector3(0, 0, _effectAngle);
        _slashEffect.transform.localScale = new Vector2(transform.localScale.x, transform.localScale.y);
    }
    void Recoil()
    {
        if(playerState.recoilingX)
        {
            if(playerState.lookingRight)
            {
                playerRb.velocity = new Vector2(-recoilXSpeed, 0);
            }
            else
            {
                playerRb.velocity = new Vector2(recoilXSpeed, 0);
            }
        }
        if(playerState.recoilingY)
        {
            playerRb.gravityScale = 0;
            if(yAxis < 0)
            {
                playerRb.velocity = new Vector2(playerRb.velocity.x, recoilYSpeed);
            }
            else
            {
                playerRb.velocity = new Vector2(playerRb.velocity.x, -recoilYSpeed);
            }
            airJumpCounter = 0;
        }
        else
        {
            playerRb.gravityScale = gravity;
        }

        //stop recoil
        if(playerState.recoilingX && stepsXRecoiled < recoilXSteps)
        {
            stepsXRecoiled++;
        }
        else
        {
            StopRecoilX();
        }
        if(playerState.recoilingY && stepsYRecoiled < recoilYSteps)
        {
            stepsYRecoiled++;
        }
        else
        {
            StopRecoilY();
        }
        if(Grounded())
        {
            StopRecoilY();
        }
    }
    void StopRecoilX()
    {
        stepsXRecoiled = 0;
        playerState.recoilingX = false;
    }
    void StopRecoilY()
    {
        stepsYRecoiled = 0;
        playerState.recoilingY = false;
    }
    public void TakeDamage(float _damage)
    {
        health -= Mathf.RoundToInt(_damage);
        StartCoroutine(StopTakingDamage());
    }
    IEnumerator StopTakingDamage()
    {
        playerState.invincible = true;
        animator.SetTrigger("takeDamage");
        ClampHealth();
        yield return new WaitForSeconds(1f);
        playerState.invincible = false;
    }
    void ClampHealth()
    {
        health = Mathf.Clamp(health, 0, maxHealth);
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
                playerRb.velocity = new Vector2(playerRb.velocity.x, jumpForce);
                playerState.jumping = true;
            }
            else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
            {
                playerRb.velocity = new Vector2(playerRb.velocity.x, jumpForce);
                playerState.jumping = true;
                airJumpCounter++;
            }
        }

        // Half vertical velocity when jump button is released
        if (Input.GetButtonUp("Jump") && playerRb.velocity.y > 0)
        {
            playerRb.velocity = new Vector2(playerRb.velocity.x, playerRb.velocity.y * 0.5f);
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
