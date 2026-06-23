using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Stage 1 영혼(빛) 오브젝트 수집 카운터.
    /// 씬에 하나만 두며, CollectibleSoulLight가 수집될 때마다 Add가 호출된다.
    /// 기억지기 NPC는 IsComplete가 true가 될 때까지 보상을 지급하지 않는다.
    /// </summary>
    public class SoulLightCounter : MonoBehaviour
    {
        public static SoulLightCounter Instance { get; private set; }

        [Tooltip("모아야 하는 영혼 오브젝트 개수")]
        [SerializeField] private int required = 3;

        public int Collected { get; private set; }
        public int Required => required;
        public int Remaining => Mathf.Max(0, required - Collected);
        public bool IsComplete => Collected >= required;

        /// <summary>(collected, required) 변경 통지.</summary>
        public System.Action<int, int> OnChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Add(int n = 1)
        {
            Collected = Mathf.Min(required, Collected + n);
            Debug.Log($"[SoulLightCounter] 영혼 {Collected}/{required} 수집");
            OnChanged?.Invoke(Collected, required);
        }
    }
}
