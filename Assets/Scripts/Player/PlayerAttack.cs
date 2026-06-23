using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 에코의 근접 공격(슬래시). 조작: Z 또는 J.
    /// 바라보는 방향 앞쪽에 사각 히트박스를 만들어 IDamageable 대상에게 데미지를 준다.
    /// 슬래시 이펙트 프리팹(fx_echo_slash)을 연동할 수 있다.
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [Header("공격")]
        [SerializeField] private int damage = 20;
        [Tooltip("연속 공격 사이 최소 간격(초)")]
        [SerializeField] private float attackCooldown = 0.35f;

        [Header("히트박스")]
        [Tooltip("플레이어 중심에서 공격 방향으로 떨어진 거리")]
        [SerializeField] private float hitboxOffset = 0.7f;
        [Tooltip("히트박스 크기")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.1f, 1.0f);
        [Tooltip("공격이 맞힐 대상 레이어 (적/보스/파괴 가능 오브젝트)")]
        [SerializeField] private LayerMask targetLayers;

        [Header("연출")]
        [Tooltip("슬래시 이펙트 프리팹 (선택)")]
        [SerializeField] private GameObject slashEffectPrefab;
        [Tooltip("이펙트 자동 제거 시간(초)")]
        [SerializeField] private float effectLifetime = 0.25f;

        private float cooldownTimer;
        private Animator animator;
        private PlayerController controller;
        private bool controlsEnabled = true;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;
            if (!controlsEnabled) return;

            if ((Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.J)) && cooldownTimer <= 0f)
                Attack();
        }

        private void Attack()
        {
            cooldownTimer = attackCooldown;
            if (animator != null) animator.SetTrigger("Attack");

            int dir = controller != null ? controller.Facing : 1;
            Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset * dir, 0f);

            SpawnEffect(center, dir);

            // 히트박스 안의 모든 피격 대상에 데미지 (중복 방지를 위해 대상별 1회)
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, hitboxSize, 0f, targetLayers);
            foreach (Collider2D hit in hits)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                    target.TakeDamage(damage, transform.position);
            }
        }

        private void SpawnEffect(Vector2 position, int dir)
        {
            if (slashEffectPrefab == null) return;
            GameObject fx = Instantiate(slashEffectPrefab, position, Quaternion.identity);
            // 방향에 맞춰 좌우 반전
            Vector3 s = fx.transform.localScale;
            s.x = Mathf.Abs(s.x) * dir;
            fx.transform.localScale = s;
            Destroy(fx, effectLifetime);
        }

        /// <summary>대화/컷신/사망 시 공격 입력 잠금.</summary>
        public void SetControlsEnabled(bool enabled) => controlsEnabled = enabled;

        private void OnDrawGizmosSelected()
        {
            int dir = Application.isPlaying && controller != null ? controller.Facing : 1;
            Vector2 center = (Vector2)transform.position + new Vector2(hitboxOffset * dir, 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, hitboxSize);
        }
    }
}
