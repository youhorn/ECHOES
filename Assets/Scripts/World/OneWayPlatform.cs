using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 일방통행 발판. 플레이어가 발판보다 위에 있으면 단단히 막고(착지/서기 가능),
    /// 아래에 있거나 통과 중이면 충돌을 무시해 아래→위로 뚫고 올라갈 수 있게 한다.
    /// PlatformEffector2D가 Continuous 충돌과 잘 안 맞아, 위치 기반 IgnoreCollision으로 확실하게 처리.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        [Tooltip("플레이어 발이 발판 윗면보다 이만큼 위면 '위에 있음'으로 판정(흔들림 방지)")]
        [SerializeField] private float margin = 0.05f;

        private Collider2D platformCol;
        private Collider2D playerCol;
        private Rigidbody2D playerRb;

        private void Awake()
        {
            platformCol = GetComponent<Collider2D>();
        }

        private void FixedUpdate()
        {
            if (playerCol == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) { playerCol = p.GetComponent<Collider2D>(); playerRb = p.GetComponent<Rigidbody2D>(); }
                if (playerCol == null) return;
            }

            // 통과 허용은 "플레이어가 발판 윗면 아래에 있고 + 위로 올라가는 중"일 때만.
            // 낙하 중(또는 정지)에는 항상 단단히 막아 착지/서기가 되게 한다.
            bool belowTop = playerCol.bounds.min.y < platformCol.bounds.max.y - margin;
            bool movingUp = playerRb != null && playerRb.velocity.y > 0.1f;
            Physics2D.IgnoreCollision(playerCol, platformCol, belowTop && movingUp);
        }
    }
}
