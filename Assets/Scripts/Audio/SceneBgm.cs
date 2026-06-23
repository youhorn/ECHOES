using UnityEngine;

namespace Echoes
{
    /// <summary>
    /// 씬 배경음악 재생기. AudioSource에 클립을 물려 두고 씬 진입 시 루프 재생한다.
    /// 타이틀(opening) · 튜토리얼(stage0) · 시계탑(ClockTower)처럼 머무는 동안만
    /// 반복 재생하면 되는 스테이지에 붙인다. 보스전(boss)처럼 특정 시점에 켜고 끄는
    /// 경우엔 playOnStart=false로 두고 PlayBgm()/StopBgm()을 이벤트로 호출한다.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SceneBgm : MonoBehaviour
    {
        [Tooltip("씬 시작과 동시에 재생할지")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float fadeInTime = 0.5f;

        private AudioSource src;
        private float targetVolume = 1f;

        private void Awake()
        {
            src = GetComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            targetVolume = src.volume;
        }

        private void Start()
        {
            if (playOnStart) PlayBgm();
        }

        /// <summary>BGM 재생(이벤트 연결용). 이미 재생 중이면 무시.</summary>
        public void PlayBgm()
        {
            if (src == null || src.clip == null) return;
            if (src.isPlaying) return;
            if (fadeInTime > 0f)
            {
                src.volume = 0f;
                src.Play();
                StartCoroutine(FadeTo(targetVolume, fadeInTime));
            }
            else { src.volume = targetVolume; src.Play(); }
        }

        /// <summary>BGM 정지(이벤트 연결용). 보스 처치 등.</summary>
        public void StopBgm()
        {
            if (src == null) return;
            StartCoroutine(FadeOutStop(0.6f));
        }

        private System.Collections.IEnumerator FadeTo(float to, float dur)
        {
            float from = src.volume, t = 0f;
            while (t < dur) { t += Time.deltaTime; src.volume = Mathf.Lerp(from, to, t / dur); yield return null; }
            src.volume = to;
        }

        private System.Collections.IEnumerator FadeOutStop(float dur)
        {
            float from = src.volume, t = 0f;
            while (t < dur) { t += Time.deltaTime; src.volume = Mathf.Lerp(from, 0f, t / dur); yield return null; }
            src.Stop();
            src.volume = targetVolume;
        }
    }
}
