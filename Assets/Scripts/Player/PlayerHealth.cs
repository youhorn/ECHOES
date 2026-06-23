using System;
using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 에코의 HP 관리 · 피격 처리 · 무적 프레임 · 넉백 · 사망/리스폰.
    /// IDamageable을 구현하여 적/함정/보스 투사체가 공통 방식으로 데미지를 가한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [Header("HP")]
        [SerializeField] private int maxHP = 100;

        [Header("피격")]
        [Tooltip("피격 후 무적 시간(초)")]
        [SerializeField] private float invincibleTime = 1f;
        [Tooltip("피격 시 넉백 세기")]
        [SerializeField] private float knockbackForce = 8f;
        [Tooltip("무적 동안 스프라이트 깜빡임 간격(초)")]
        [SerializeField] private float flashInterval = 0.1f;

        [Header("리스폰")]
        [Tooltip("사망 후 리스폰까지 대기 시간(초)")]
        [SerializeField] private float respawnDelay = 1.2f;
        [Tooltip("리스폰 위치 (비우면 시작 위치 사용)")]
        [SerializeField] private Transform respawnPoint;

        // --- 이벤트 (UI · GameManager가 구독) ---
        /// <summary>(현재 HP, 최대 HP)</summary>
        public event Action<int, int> OnHealthChanged;
        public event Action OnDeath;
        public event Action OnRespawn;

        // --- 상태 ---
        private int currentHP;
        private bool isInvincible;
        private bool isDead;

        // --- 컴포넌트 ---
        private Rigidbody2D rb;
        private Collider2D col;
        private Animator animator;
        private SpriteRenderer sprite;
        private PlayerController controller;
        private PlayerAttack attack;
        private Vector3 startPosition;

        public bool IsAlive => !isDead;
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            animator = GetComponentInChildren<Animator>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            controller = GetComponent<PlayerController>();
            attack = GetComponent<PlayerAttack>();
            startPosition = transform.position;
            currentHP = maxHP;
        }

        private void Start()
        {
            // UI 초기 동기화
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        // ---------------------------------------------------------------

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (isDead || isInvincible || amount <= 0) return;

            currentHP = Mathf.Max(0, currentHP - amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);

            if (currentHP <= 0)
            {
                Die();
                return;
            }

            ApplyKnockback(sourcePosition);
            if (animator != null) animator.SetTrigger("Hurt");
            StartCoroutine(InvincibilityRoutine());
        }

        /// <summary>회복 (기억 조각/체크포인트 등에서 사용).</summary>
        public void Heal(int amount)
        {
            if (isDead || amount <= 0) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }

        private void ApplyKnockback(Vector2 sourcePosition)
        {
            Vector2 dir = ((Vector2)transform.position - sourcePosition).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up; // 같은 위치면 위로
            dir.y = Mathf.Max(dir.y, 0.4f); // 살짝 떠오르게
            rb.velocity = Vector2.zero;
            rb.AddForce(dir.normalized * knockbackForce, ForceMode2D.Impulse);
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            float t = 0f;
            bool visible = true;
            while (t < invincibleTime)
            {
                if (sprite != null)
                {
                    visible = !visible;
                    sprite.enabled = visible;
                }
                yield return new WaitForSeconds(flashInterval);
                t += flashInterval;
            }
            if (sprite != null) sprite.enabled = true;
            isInvincible = false;
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;
            isInvincible = true;
            rb.velocity = Vector2.zero;

            if (controller != null) controller.SetControlsEnabled(false);
            if (attack != null) attack.SetControlsEnabled(false);
            if (animator != null) animator.SetTrigger("Death");

            OnDeath?.Invoke();
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        /// <summary>체크포인트(또는 시작 위치)에서 부활. GameManager가 직접 호출할 수도 있다.</summary>
        public void Respawn()
        {
            StopAllCoroutines();
            transform.position = respawnPoint != null ? respawnPoint.position : startPosition;
            rb.velocity = Vector2.zero;
            currentHP = maxHP;
            isDead = false;
            isInvincible = false;

            if (sprite != null) sprite.enabled = true;
            if (controller != null) controller.SetControlsEnabled(true);
            if (attack != null) attack.SetControlsEnabled(true);

            OnHealthChanged?.Invoke(currentHP, maxHP);
            OnRespawn?.Invoke();
        }

        /// <summary>체크포인트 갱신 (세이브 포인트 등에서 사용).</summary>
        public void SetRespawnPoint(Transform point) => respawnPoint = point;
    }
}
