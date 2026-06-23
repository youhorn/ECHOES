using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// Stage 5 〈시간의 틈〉. 흩어진 단서 오브젝트 3개를 순서대로 확인하면
    /// 단서 문장이 차례로 출력되고, 마지막에 할아버지의 실루엣이 나타나며
    /// 기억 조각 always와 다음 문 열쇠를 지급한다. (전투 없음, 분위기 중심)
    /// </summary>
    public class TimeGapManager : MonoBehaviour
    {
        [Header("진행")]
        [Tooltip("모아야 하는 단서 개수")]
        [SerializeField] private int total = 3;

        [Header("마지막 컷신")]
        [SerializeField] private string grandpaSpeaker = "할아버지";
        [TextArea] [SerializeField] private string grandpaLine = "이 아이가 아프지 않게 해다오.";
        [SerializeField] private string echoSpeaker = "에코";
        [TextArea] [SerializeField] private string echoFinalLine = "내가 잊은 건, 버림받은 기억이 아니었어.";
        [Tooltip("마지막 단서에서 켜지는 할아버지 실루엣")]
        [SerializeField] private GameObject grandpaSilhouette;

        [Header("보상")]
        [Tooltip("지급할 기억 조각 id (5 = always)")]
        [SerializeField] private int rewardMemoryId = 5;
        [SerializeField] private int rewardKeys = 1;
        [Tooltip("완료 시 활성화할 출구(선택). 비우면 열쇠로만 문을 연다")]
        [SerializeField] private GameObject exitToActivate;

        private int collected;
        private bool finished;

        public int Collected => collected;
        public bool IsFinished => finished;

        /// <summary>ClueObject가 수집될 때 호출.</summary>
        public void Collect(string clueLine)
        {
            if (finished) return;
            collected++;
            Debug.Log($"[TimeGapManager] 단서 {collected}/{total}");

            if (DialogueSystem.Instance == null)
            {
                Debug.LogError("[TimeGapManager] DialogueSystem이 씬에 없습니다.");
                return;
            }

            var clue = new DialogueLine { speaker = "", text = clueLine };

            if (collected < total)
            {
                DialogueSystem.Instance.Begin(new[] { clue }, null, null);
            }
            else
            {
                if (grandpaSilhouette != null) grandpaSilhouette.SetActive(true);
                var lines = new[]
                {
                    clue,
                    new DialogueLine { speaker = grandpaSpeaker, text = grandpaLine },
                    new DialogueLine { speaker = echoSpeaker, text = echoFinalLine }
                };
                DialogueSystem.Instance.Begin(lines, null, (_, __) => Finish());
            }
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;

            if (GameManager.Instance != null)
            {
                if (rewardMemoryId > 0) GameManager.Instance.CollectMemory(rewardMemoryId);
                if (rewardKeys > 0) GameManager.Instance.AddKey(rewardKeys);
            }
            if (exitToActivate != null) exitToActivate.SetActive(true);
            Debug.Log("[TimeGapManager] always 조각 + 열쇠 지급, 출구 개방");
        }
    }
}
