using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 에코(플레이어)의 이동 · 점프 · 낙하 처리.
    /// Rigidbody2D 기반. 조작: A/D(또는 ←/→) 이동, Space 점프.
    /// 좋은 플랫포머 손맛을 위해 코요테 타임 · 점프 버퍼 · 가변 점프 높이를 포함한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("이동")]
        [Tooltip("좌우 이동 속도 (units/sec)")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("점프")]
        [Tooltip("점프 시 초기 상승 속도")]
        [SerializeField] private float jumpForce = 13f;
        [Tooltip("공중에서 추가로 가능한 점프 횟수 (1 = 더블 점프)")]
        [SerializeField] private int maxAirJumps = 1;
        [Tooltip("상승 중 점프 키를 떼면 속도가 이 비율로 깎여 점프 높이가 짧아진다")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        [Tooltip("하강을 더 묵직하게 만드는 추가 중력 배수")]
        [SerializeField] private float fallGravityMultiplier = 2.2f;
        [Tooltip("상승 중 점프 키를 누르고 있을 때의 중력 배수")]
        [SerializeField] private float lowJumpMultiplier = 1.6f;

        [Header("접지 판정")]
        [Tooltip("바닥으로 인식할 레이어")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("발밑 접지 검사 박스의 두께")]
        [SerializeField] private float groundCheckThickness = 0.12f;

        [Header("손맛 보정")]
        [Tooltip("바닥을 벗어난 뒤에도 점프를 허용하는 유예 시간(초)")]
        [SerializeField] private float coyoteTime = 0.1f;
        [Tooltip("착지 직전 누른 점프 입력을 기억하는 시간(초)")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        // --- 컴포넌트 캐시 ---
        private Rigidbody2D rb;
        private Collider2D col;
        private Animator animator;
        private SpriteRenderer sprite;

        // --- 상태 ---
        private float moveInput;
        private bool isGrounded;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private int airJumpsRemaining;
        private float defaultGravityScale;
        private int facing = 1;          // 1 = 오른쪽, -1 = 왼쪽
        private bool controlsEnabled = true;

        /// <summary>현재 바라보는 방향 (1: 오른쪽, -1: 왼쪽). 공격 방향 등에 사용.</summary>
        public int Facing => facing;
        /// <summary>바닥에 닿아 있는지.</summary>
        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            animator = GetComponentInChildren<Animator>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            defaultGravityScale = rb.gravityScale;

            // 픽셀 플랫포머에 적합한 기본 설정 (인스펙터에서 누락돼도 안전하게 동작)
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        private void Update()
        {
            ReadInput();
            UpdateTimers();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            CheckGrounded();
            ApplyMovement();
            ApplyJump();
            ApplyBetterGravity();
        }

        // ---------------------------------------------------------------

        private void ReadInput()
        {
            if (!controlsEnabled)
            {
                moveInput = 0f;
                return;
            }

            moveInput = Input.GetAxisRaw("Horizontal"); // A/D, ←/→

            // 점프 버퍼: 눌린 순간을 기억
            if (Input.GetButtonDown("Jump")) // 기본값 Space
                jumpBufferCounter = jumpBufferTime;

            // 가변 점프: 상승 중 키를 떼면 점프를 끊는다
            if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }

        private void UpdateTimers()
        {
            coyoteCounter -= Time.deltaTime;
            jumpBufferCounter -= Time.deltaTime;
        }

        private void CheckGrounded()
        {
            // 콜라이더 발밑에 얇은 박스를 겹쳐 바닥을 검사 (별도 자식 오브젝트 불필요)
            Bounds b = col.bounds;
            Vector2 boxCenter = new Vector2(b.center.x, b.min.y - groundCheckThickness * 0.5f);
            Vector2 boxSize = new Vector2(b.size.x * 0.95f, groundCheckThickness);
            isGrounded = Physics2D.OverlapBox(boxCenter, boxSize, 0f, groundLayer) != null;

            if (isGrounded)
            {
                coyoteCounter = coyoteTime;
                airJumpsRemaining = maxAirJumps;   // 착지 시 공중 점프 횟수 충전
            }
        }

        private void ApplyMovement()
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

            // 방향 전환 (자식 오브젝트도 함께 뒤집히도록 스케일로 플립)
            if (moveInput > 0.01f && facing != 1) Flip(1);
            else if (moveInput < -0.01f && facing != -1) Flip(-1);
        }

        private void ApplyJump()
        {
            if (jumpBufferCounter <= 0f) return;

            bool canGroundJump = coyoteCounter > 0f;            // 접지(코요테 포함)
            bool canAirJump = !canGroundJump && airJumpsRemaining > 0; // 공중 점프
            if (!canGroundJump && !canAirJump) return;

            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            if (canAirJump) airJumpsRemaining--;
            if (animator != null) animator.SetTrigger("Jump");
        }

        private void ApplyBetterGravity()
        {
            if (rb.velocity.y < 0f)
                rb.gravityScale = defaultGravityScale * fallGravityMultiplier;   // 하강은 묵직하게
            else if (rb.velocity.y > 0f && !Input.GetButton("Jump"))
                rb.gravityScale = defaultGravityScale * lowJumpMultiplier;        // 짧은 점프
            else
                rb.gravityScale = defaultGravityScale;
        }

        private void Flip(int dir)
        {
            facing = dir;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            transform.localScale = s;
        }

        private void UpdateAnimator()
        {
            if (animator == null) return;
            animator.SetFloat("Speed", Mathf.Abs(moveInput));
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetFloat("VerticalVelocity", rb.velocity.y);
        }

        // --- 외부 제어 (대화 · 컷신 · 사망 시 입력 잠금) ---

        /// <summary>대화/컷신/사망 등에서 플레이어 입력을 켜고 끈다.</summary>
        public void SetControlsEnabled(bool enabled)
        {
            controlsEnabled = enabled;
            if (!enabled)
            {
                moveInput = 0f;
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 에디터에서 접지 검사 박스를 시각화
            Collider2D c = GetComponent<Collider2D>();
            if (c == null) return;
            Bounds b = c.bounds;
            Vector2 center = new Vector2(b.center.x, b.min.y - groundCheckThickness * 0.5f);
            Vector2 size = new Vector2(b.size.x * 0.95f, groundCheckThickness);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
