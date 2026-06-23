using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Stage 5 시간의 틈에 흩어진 단서 오브젝트. 플레이어가 닿으면
    /// 자신의 단서 문장을 TimeGapManager에 전달하고 사라진다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ClueObject : MonoBehaviour
    {
        [TextArea] [SerializeField] private string clueLine = "";
        [SerializeField] private TimeGapManager manager;
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
            if (manager == null) manager = FindObjectOfType<TimeGapManager>();
        }

        private void Update()
        {
            if (bobAmplitude > 0f)
                transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (manager != null) manager.Collect(clueLine);
            Destroy(gameObject);
        }
    }
}
