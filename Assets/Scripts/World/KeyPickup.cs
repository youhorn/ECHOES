using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 월드에 배치된 열쇠 아이템. 플레이어가 닿으면 GameManager 열쇠 수가 1 증가하고 사라진다.
    /// (NPC/퍼즐이 직접 GameManager.AddKey를 호출하는 경로와 별개로, 필드 습득용)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyPickup : MonoBehaviour
    {
        [SerializeField] private int amount = 1;
        [Tooltip("획득 시 재생할 이펙트 (선택)")]
        [SerializeField] private GameObject pickupEffect;

        private void Reset()
        {
            // 편의: 콜라이더를 트리거로 기본 설정
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (GameManager.Instance != null)
                GameManager.Instance.AddKey(amount);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
