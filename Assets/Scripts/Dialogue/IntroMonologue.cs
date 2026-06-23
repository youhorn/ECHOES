using System.Collections;
using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 씬 시작과 동시에 에코(또는 지정 화자)의 독백을 출력한다.
    /// 상호작용 없이, 스테이지에 들어오자마자 대사가 흐르도록 한다.
    /// (Stage1 도입, ClockTower/Calculator 도입 독백 등)
    /// </summary>
    public class IntroMonologue : MonoBehaviour
    {
        [SerializeField] private string speakerName = "에코";
        [SerializeField] private DialogueLine[] lines;
        [Tooltip("씬 시작 후 독백 시작까지 대기(초)")]
        [SerializeField] private float startDelay = 0.4f;
        [Tooltip("독백 동안 플레이어 입력을 잠글지 (DialogueSystem이 자동 처리하므로 보통 불필요)")]
        [SerializeField] private bool onlyOnce = true;

        private static readonly System.Collections.Generic.HashSet<string> playedScenes
            = new System.Collections.Generic.HashSet<string>();

        private IEnumerator Start()
        {
            if (lines == null || lines.Length == 0) yield break;

            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (onlyOnce && playedScenes.Contains(scene)) yield break;
            if (onlyOnce) playedScenes.Add(scene);

            yield return new WaitForSeconds(startDelay);

            // DialogueSystem 준비 대기
            float t = 0f;
            while (DialogueSystem.Instance == null && t < 3f) { t += Time.deltaTime; yield return null; }
            if (DialogueSystem.Instance == null) yield break;

            foreach (var l in lines)
                if (string.IsNullOrEmpty(l.speaker)) l.speaker = speakerName;

            DialogueSystem.Instance.Begin(lines, null, null);
        }
    }
}
