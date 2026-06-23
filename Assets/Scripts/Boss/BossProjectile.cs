using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 보스가 발사하는 투사체(fx_blank_projectile). 직선 이동하며 플레이어에게 데미지를 준다.
    /// 플레이어/벽에 닿거나 수명이 다하면 사라진다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private int damage = 12;
        [SerializeField] private float lifetime = 4f;
        [Tooltip("벽으로 인식해 소멸할 레이어")]
        [SerializeField] private LayerMask wallLayers;
        [Tooltip("충돌 시 이펙트 (fx_blank_proj_hit, 선택)")]
        [SerializeField] private GameObject hitEffect;

        private Vector2 direction = Vector2.left;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        /// <summary>발사 시 방향/속도/데미지를 설정.</summary>
        public void Init(Vector2 dir, float spd, int dmg)
        {
            direction = dir.normalized;
            speed = spd;
            damage = dmg;

            // 진행 방향으로 회전
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Start() => Destroy(gameObject, lifetime);

        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var target = other.GetComponentInParent<IDamageable>();
                if (target != null && target.IsAlive)
                    target.TakeDamage(damage, transform.position);
                Hit();
                return;
            }

            // 벽에 닿으면 소멸
            if ((wallLayers.value & (1 << other.gameObject.layer)) != 0)
                Hit();
        }

        private void Hit()
        {
            if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
