#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using Echoes;

namespace Echoes.EditorTools
{
    /// <summary>
    /// 「지강이에게」 신규 퍼즐 스테이지(멈춘 시계탑 / 낡은 계산기) 빌더.
    /// Stage1.unity를 템플릿으로 복제 → NPC를 NumberLockPuzzle로 교체 → 프롭/입력표시 추가.
    /// </summary>
    public static class EchoesSceneBuilder
    {
        struct Cfg
        {
            public string speaker, spritePath, code, hint, diaryPath;
            public int digits, memId;
            public Vector3 propPos, propScale, inputPos, diaryPos;
            public float inputFont;
            public DialogueLine[] prompt, cutscene;
        }

        [MenuItem("Echoes/Build New Puzzle Scenes")]
        public static void BuildAll()
        {
            BuildClockTower();
            BuildCalculator();
            Debug.Log("[SceneBuilder] ClockTower + Calculator 완료");
        }

        static DialogueLine L(string s, string t) => new DialogueLine { speaker = s, text = t };

        public static void BuildClockTower()
        {
            var c = new Cfg
            {
                speaker = "시계지기",
                spritePath = "Assets/Art/Objects/clock_tower.png",
                propPos = new Vector3(-2.2f, -0.2f, 1f),
                propScale = Vector3.one,
                inputPos = new Vector3(-2.2f, 1.6f, 0f),
                inputFont = 20f,
                digits = 1, code = "7", memId = 3,
                hint = "어둠 속에서 숫자는 읽히지 않는다. 다만, 몇 조각의 빛으로 자신을 드러낼 뿐이지.",
                prompt = new[]
                {
                    L("시계지기", "이 숫자들은 보통 숫자가 아니다. 아래 규칙을 보고 마지막 답을 말해라."),
                    L("시계지기", "1 = 2    2 = 5    3 = 5\n4 = 4    7 = 3    8 = ?"),
                    L("시계지기", "[E로 입력 시작 · 숫자키 입력 · Enter 제출 · H 힌트]"),
                },
                cutscene = new[]
                {
                    L("", "멈춰 있던 시계탑이 천천히 움직이기 시작한다.  째깍…  째깍…"),
                    L("기억", "어린 에코가 시계탑 아래에 서 있다."),
                    L("기억", "누군가의 손이 작은 사탕을 건넨다. 얼굴은 보이지 않는다. 다만 낮고 따뜻한 목소리만 들린다."),
                    L("???", "에코, 오늘 하루는 어땠니?"),
                    L("", "기억 조각 ‘I’ 를 손에 넣었다."),
                },
            };
            Build("ClockTower", c, false);
        }

        public static void BuildCalculator()
        {
            var c = new Cfg
            {
                speaker = "낡은 계산기",
                spritePath = "Assets/Art/Objects/calculator.png",
                propPos = new Vector3(-2.4f, -0.4f, 1f),
                propScale = Vector3.one,
                inputPos = new Vector3(-2.4f, 1.4f, 0f),
                inputFont = 20f,
                diaryPath = "Assets/Art/Objects/diary.png",
                diaryPos = new Vector3(-0.6f, -1.9f, 1f),
                digits = 4, code = "1254", memId = 4,
                hint = "계산기는 글을 읽는 물건이 아니지.",
                prompt = new[]
                {
                    L("낡은 계산기", "방 안에는 오래된 책상, 찢어진 일기장, 낡은 계산기가 있다."),
                    L("낡은 계산기", "아래 글을 보고, 네 자리 암호를 말해줘."),
                    L("낡은 계산기", "책상 밑에 찢어진 일기가 있었다.\n문틈에는 검은 이끼가 눅눅하게 번져 있었다.\n창문 너머로 할아버지의 오르골 소리가 흘러나왔다.\n할아버지는 나를 무척 사랑하신다."),
                    L("낡은 계산기", "[E로 입력 시작 · 숫자키 입력 · Enter 제출 · H 힌트]"),
                },
                cutscene = new[]
                {
                    L("", "계산기 화면에 숫자 대신 짧은 문장이 떠오른다.  1254 →"),
                    L("할아버지", "에코, 나는 누구보다 널 사랑한단다."),
                    L("기억", "누군가 책상 앞에서 계산기를 두드리고 있다. 에코는 그 옆에서 일기를 쓰고 있다. 오르골 소리가 작게 흐른다."),
                    L("에코", "할아버지, 절대 내 곁을 떠나면 안 돼."),
                    L("", "노인은 애써 웃으며 계산기를 조용히 내려놓는다."),
                    L("", "기억 조각 ‘my’ 를 손에 넣었다."),
                },
            };
            Build("Calculator", c, true);
        }

        static void Build(string sceneName, Cfg c, bool withDiary)
        {
            string src = "Assets/Scenes/Stage1.unity";
            string dst = "Assets/Scenes/" + sceneName + ".unity";
            if (File.Exists(dst)) AssetDatabase.DeleteAsset(dst);
            if (!AssetDatabase.CopyAsset(src, dst))
            {
                Debug.LogError("[SceneBuilder] 복제 실패: " + dst);
                return;
            }
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);

            // NPC → NumberLockPuzzle 교체
            var npc = Object.FindObjectOfType<NPCInteraction>(true);
            if (npc == null) { Debug.LogError("[SceneBuilder] NPC 없음"); return; }
            var go = npc.gameObject;
            var indicator = go.transform.Find("Indicator");
            Object.DestroyImmediate(npc);
            go.name = sceneName + "Puzzle";
            go.transform.position = new Vector3(-2.2f, -2.4f, 0f);

            var puzzle = go.AddComponent<NumberLockPuzzle>();

            // 프롭 스프라이트
            var prop = new GameObject(sceneName + "Prop");
            var sr = prop.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(c.spritePath);
            sr.sortingOrder = -10;
            prop.transform.position = c.propPos;
            prop.transform.localScale = c.propScale;

            // 일기장(계산기 씬)
            if (withDiary && !string.IsNullOrEmpty(c.diaryPath))
            {
                var diary = new GameObject("Diary");
                var dsr = diary.AddComponent<SpriteRenderer>();
                dsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(c.diaryPath);
                dsr.sortingOrder = -8;
                diary.transform.position = c.diaryPos;
            }

            // 입력 표시 TMP (월드)
            var tmpGO = new GameObject("InputDisplay");
            var tmp = tmpGO.AddComponent<TextMeshPro>();
            tmp.text = "";
            tmp.fontSize = c.inputFont;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.85f, 0.4f);
            tmpGO.transform.position = c.inputPos;
            var rt = tmp.rectTransform; rt.sizeDelta = new Vector2(6f, 2f);

            // 직렬화 필드 배선
            var so = new SerializedObject(puzzle);
            so.FindProperty("speakerName").stringValue = c.speaker;
            so.FindProperty("hintText").stringValue = c.hint;
            so.FindProperty("digitCount").intValue = c.digits;
            so.FindProperty("correctCode").stringValue = c.code;
            so.FindProperty("rewardMemoryId").intValue = c.memId;
            so.FindProperty("rewardKeys").intValue = 1;
            so.FindProperty("inputDisplay").objectReferenceValue = tmp;
            if (indicator != null)
                so.FindProperty("interactIndicator").objectReferenceValue = indicator.gameObject;
            SetLines(so.FindProperty("promptLines"), c.prompt);
            SetLines(so.FindProperty("successCutscene"), c.cutscene);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SceneBuilder] " + sceneName + " 저장 완료");
        }

        static void SetLines(SerializedProperty arr, DialogueLine[] lines)
        {
            arr.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
                e.FindPropertyRelative("text").stringValue = lines[i].text;
            }
        }
    }
}
#endif
