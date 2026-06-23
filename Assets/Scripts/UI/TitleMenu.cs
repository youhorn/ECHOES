using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echoes
{
    /// <summary>
    /// 타이틀 화면 메뉴. [게임 시작] / [종료].
    /// 버튼 OnClick에 StartGame()/QuitGame()을 연결하거나, 키보드(Enter/Z 시작)로도 동작한다.
    /// 게임 시작 시 GameManager가 진행 데이터를 초기화하고 Stage 1을 로드한다.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [Tooltip("GameManager가 없을 때 직접 로드할 첫 스테이지 씬 이름(폴백)")]
        [SerializeField] private string firstStageScene = "Stage1";
        [Tooltip("키보드 Enter/Z로도 게임 시작 허용")]
        [SerializeField] private bool allowKeyboardStart = true;

        private void Update()
        {
            if (allowKeyboardStart &&
                (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space)))
                StartGame();
        }

        /// <summary>[게임 시작] 버튼.</summary>
        public void StartGame()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewGame();
            else
                SceneManager.LoadScene(firstStageScene); // GameManager 미배치 시 폴백
        }

        /// <summary>[종료] 버튼.</summary>
        public void QuitGame()
        {
            Debug.Log("[TitleMenu] 게임 종료");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
