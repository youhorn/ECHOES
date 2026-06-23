using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Stage 1 발판 위에 떠 있는 영혼(빛) 오브젝트. 플레이어가 닿으면
    /// SoulLightCounter에 1을 더하고 사라진다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CollectibleSoulLight : MonoBehaviour
    {
        [Tooltip("획득 시 재생할 이펙트 (선택)")]
        [SerializeField] private GameObject pickupEffect;
        [Tooltip("위아래로 떠다니는 연출 진폭(0이면 정지)")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobSpeed = 2f;

        private Vector3 basePos;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Start()
        {
            basePos = transform.position;
        }

        private void Update()
        {
            if (bobAmplitude > 0f)
                transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            if (SoulLightCounter.Instance != null)
                SoulLightCounter.Instance.Add(1);

            if (pickupEffect != null)
                Instantiate(pickupEffect, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
