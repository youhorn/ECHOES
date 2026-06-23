using System;
using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Stage 4 보스 — 어둠의 화신 블랭크.
    /// 상태 기반 AI로 패턴을 순환한다.
    ///  · 페이즈 1 (HP 100~50%): 좌우 돌진 / 원거리 투사체 (교대)
    ///  · 페이즈 2 (HP 50% 미만): 패턴 속도↑ / 투사체 수↑ / 순간이동 추가
    /// IDamageable을 구현해 플레이어 공격으로 피해를 입으며, 처치 시 기억 문장 ④⑤를 지급한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class BossController : MonoBehaviour, IDamageable
    {
        [Header("체력")]
        [SerializeField] private int maxHP = 100;
        [Range(0.1f, 0.9f)]
        [SerializeField] private float phase2Threshold = 0.5f;

        [Header("이동 범위 (월드 X 좌표)")]
        [SerializeField] private float leftX = -7f;
        [SerializeField] private float rightX = 7f;
        [Tooltip("돌진 속도")]
        [SerializeField] private float dashSpeed = 12f;
        [Tooltip("idle 시 부유 이동 속도")]
        [SerializeField] private float driftSpeed = 2.5f;

        [Header("투사체")]
        [SerializeField] private BossProjectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 8f;
        [SerializeField] private int projectileDamage = 12;
        [Tooltip("페이즈 2에서 한 번에 쏘는 투사체 수 (부채꼴)")]
        [SerializeField] private int phase2ProjectileCount = 3;
        [SerializeField] private float spreadAngle = 20f;

        [Header("타이밍")]
        [Tooltip("패턴 사이 대기(페이즈1)")]
        [SerializeField] private float patternInterval = 1.4f;
        [Tooltip("페이즈2 시간 배수(작을수록 빠름)")]
        [SerializeField] private float phase2SpeedMul = 0.6f;

        [Header("접촉 데미지")]
        [SerializeField] private int contactDamage = 15;
        [SerializeField] private float contactCooldown = 0.8f;

        [Header("처치 보상")]
        [Tooltip("처치 시 지급할 기억 문장 id (보고서: ④⑤)")]
        [SerializeField] private int[] rewardMemoryIds = { 4, 5 };
        [Tooltip("처치 후 다음 스테이지로 이동까지 대기(초)")]
        [SerializeField] private float deathToNextDelay = 3f;
        [Tooltip("켜면 GameManager.LoadNextStage, 끄면 아래 씬 이름")]
        [SerializeField] private bool goToNextStage = true;
        [SerializeField] private string nextSceneOverride = "Stage5";

        [Header("활성화")]
        [Tooltip("켜면 씬 시작과 동시에 전투 시작. 끄면 Activate() 호출 전까지 가만히 있음(대화/연출 후 시작용)")]
        [SerializeField] private bool startActive = true;

        // --- 이벤트 ---
        public event Action<int, int> OnBossHealthChanged; // (현재, 최대) — 보스 HP바 UI용
        public event Action OnPhase2;
        public event Action OnBossDefeated;

        // --- 상태 ---
        private int currentHP;
        private bool isPhase2;
        private bool isDead;
        private bool activated;
        private float lastContactTime = -999f;
        private float hoverY;

        private Rigidbody2D rb;
        private Animator animator;
        private SpriteRenderer sprite;
        private Transform player;

        public bool IsAlive => !isDead;
        public bool IsPhase2 => isPhase2;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponentInChildren<Animator>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            currentHP = maxHP;
            hoverY = transform.position.y;
            rb.gravityScale = 0f;        // 부유형 보스
            rb.freezeRotation = true;
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            OnBossHealthChanged?.Invoke(currentHP, maxHP);
            if (startActive) Activate();
        }

        /// <summary>전투 시작. 대화/연출이 끝난 뒤 외부(NPC 이벤트 등)에서 호출해 보스를 깨운다.</summary>
        public void Activate()
        {
            if (activated || isDead) return;
            activated = true;
            StartCoroutine(BehaviorLoop());
        }

        // =============================================================
        // AI 패턴 루프
        // =============================================================

        private IEnumerator BehaviorLoop()
        {
            int patternToggle = 0;
            while (!isDead)
            {
                if (animator != null) animator.SetTrigger("Idle");

                float wait = patternInterval * (isPhase2 ? phase2SpeedMul : 1f);
                float t = 0f;
                while (t < wait) { Drift(); t += Time.deltaTime; yield return null; }

                if (isDead) yield break;

                // 패턴 선택
                if (!isPhase2)
                {
                    // 페이즈1: 돌진 ↔ 투사체 교대
                    if (patternToggle % 2 == 0) yield return DashAttack();
                    else yield return ShootAttack();
                }
                else
                {
                    // 페이즈2: 돌진 → 투사체 → 순간이동 순환
                    int pick = patternToggle % 3;
                    if (pick == 0) yield return DashAttack();
                    else if (pick == 1) yield return ShootAttack();
                    else yield return TeleportMove();
                }
                patternToggle++;
            }
        }

        /// <summary>idle 중 플레이어 쪽으로 천천히 부유.</summary>
        private void Drift()
        {
            if (player == null) return;
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            float nx = Mathf.Clamp(transform.position.x + dir * driftSpeed * Time.deltaTime, leftX, rightX);
            transform.position = new Vector3(nx, hoverY, transform.position.z);
            FaceTo(player.position.x);
        }

        // ---- 돌진 ----
        private IEnumerator DashAttack()
        {
            if (animator != null) animator.SetTrigger("Dash");
            if (player == null) yield break;

            float dir = Mathf.Sign(player.position.x - transform.position.x);
            if (dir == 0) dir = 1;
            FaceTo(transform.position.x + dir);

            float speed = dashSpeed * (isPhase2 ? 1.4f : 1f);
            float targetX = Mathf.Clamp(transform.position.x + dir * 14f, leftX, rightX);

            while (Mathf.Abs(transform.position.x - targetX) > 0.1f && !isDead)
            {
                float nx = Mathf.MoveTowards(transform.position.x, targetX, speed * Time.deltaTime);
                transform.position = new Vector3(nx, hoverY, transform.position.z);
                yield return null;
            }
        }

        // ---- 투사체 발사 ----
        private IEnumerator ShootAttack()
        {
            if (animator != null) animator.SetTrigger("Shoot");
            if (player == null || projectilePrefab == null) yield break;

            yield return new WaitForSeconds(0.2f); // 발사 모션 선딜

            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            Vector2 baseDir = ((Vector2)player.position - origin).normalized;

            int count = isPhase2 ? Mathf.Max(1, phase2ProjectileCount) : 1;
            if (count == 1)
            {
                Fire(origin, baseDir);
            }
            else
            {
                // 부채꼴로 분산 발사
                float start = -spreadAngle * 0.5f;
                float step = spreadAngle / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    float ang = start + step * i;
                    Vector2 d = Rotate(baseDir, ang);
                    Fire(origin, d);
                }
            }
            yield return new WaitForSeconds(0.3f);
        }

        private void Fire(Vector2 origin, Vector2 dir)
        {
            BossProjectile proj = Instantiate(projectilePrefab, origin, Quaternion.identity);
            proj.Init(dir, projectileSpeed, projectileDamage);
        }

        // ---- 순간이동 (페이즈2) ----
        private IEnumerator TeleportMove()
        {
            if (animator != null) animator.SetTrigger("Teleport");

            // 사라짐
            if (sprite != null) sprite.enabled = false;
            yield return new WaitForSeconds(0.4f);

            // 플레이어 근처(좌우 한쪽)로 재등장
            if (player != null)
            {
                float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                float nx = Mathf.Clamp(player.position.x + side * 3f, leftX, rightX);
                transform.position = new Vector3(nx, hoverY, transform.position.z);
                FaceTo(player.position.x);
            }

            if (sprite != null) sprite.enabled = true;
            yield return new WaitForSeconds(0.2f);
        }

        // =============================================================
        // 피격 / 페이즈 / 사망
        // =============================================================

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (isDead || amount <= 0) return;

            currentHP = Mathf.Max(0, currentHP - amount);
            OnBossHealthChanged?.Invoke(currentHP, maxHP);
            StartCoroutine(HitFlash());

            if (!isPhase2 && currentHP <= maxHP * phase2Threshold)
                EnterPhase2();

            if (currentHP <= 0)
                Die();
        }

        private void EnterPhase2()
        {
            isPhase2 = true;
            if (animator != null) animator.SetBool("Phase2", true);
            OnPhase2?.Invoke();
            Debug.Log("[BossController] 페이즈 2 돌입");
        }

        private IEnumerator HitFlash()
        {
            if (sprite == null) yield break;
            Color orig = sprite.color;
            sprite.color = Color.white;
            yield return new WaitForSeconds(0.06f);
            if (sprite != null) sprite.color = orig;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            StopAllCoroutines();

            if (animator != null) animator.SetTrigger("Death");
            Debug.Log("[BossController] 보스 처치 — 기억 문장 ④⑤ 지급");

            // 보상: 기억 문장 ④⑤ 동시 획득
            if (GameManager.Instance != null && rewardMemoryIds != null)
                foreach (int id in rewardMemoryIds)
                    GameManager.Instance.CollectMemory(id);

            OnBossDefeated?.Invoke();
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            yield return new WaitForSeconds(deathToNextDelay);
            if (GameManager.Instance == null) yield break;

            if (goToNextStage) GameManager.Instance.LoadNextStage();
            else if (!string.IsNullOrEmpty(nextSceneOverride)) GameManager.Instance.LoadSceneByName(nextSceneOverride);
        }

        // =============================================================
        // 접촉 데미지 (몸통/돌진)
        // =============================================================

        private void OnCollisionEnter2D(Collision2D c) => TryContact(c.collider);
        private void OnCollisionStay2D(Collision2D c) => TryContact(c.collider);
        private void OnTriggerEnter2D(Collider2D other) => TryContact(other);
        private void OnTriggerStay2D(Collider2D other) => TryContact(other);

        private void TryContact(Collider2D other)
        {
            if (isDead || !other.CompareTag("Player")) return;
            if (Time.time - lastContactTime < contactCooldown) return;

            var target = other.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) return;

            target.TakeDamage(contactDamage, transform.position);
            lastContactTime = Time.time;
        }

        // =============================================================
        // 유틸
        // =============================================================

        private void FaceTo(float targetX)
        {
            if (sprite == null) return;
            float dir = Mathf.Sign(targetX - transform.position.x);
            if (dir != 0) sprite.flipX = dir < 0;
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            float y = Application.isPlaying ? hoverY : transform.position.y;
            Gizmos.DrawLine(new Vector3(leftX, y, 0), new Vector3(rightX, y, 0));
        }
    }
}
