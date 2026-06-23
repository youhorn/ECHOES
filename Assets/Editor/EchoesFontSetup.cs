// ECHOES — 한글 TMP 폰트 자동 세팅
// 메뉴: Tools ▸ ECHOES ▸ Setup Korean Font (TMP)
//
// 하는 일:
//   1) TMP Essential Resources 가 없으면 자동 임포트 (없을 때만)
//   2) Assets/Fonts/NotoSansKR-Regular.ttf 로 Dynamic TMP Font Asset 생성
//      → Assets/Fonts/NotoSansKR SDF.asset
//   3) TMP Settings 의 Default Font + Fallback 목록에 등록
//      → 모든 TMP 텍스트에서 한글이 자동으로 렌더됨 (□ 두부 방지)
//
// Dynamic 모드라 11,172개 한글 음절을 미리 아틀라스에 굽지 않고
// 런타임에 필요한 글자만 렌더합니다. 빌드에서도 정상 동작합니다.

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace Echoes.EditorTools
{
    [InitializeOnLoad]
    public static class EchoesFontSetup
    {
        const string TtfPath        = "Assets/Fonts/NotoSansKR-Regular.ttf";
        const string FontAssetPath  = "Assets/Fonts/NotoSansKR SDF.asset";
        const string EssentialsPkg  = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
        const string PendingKey     = "ECHOES_PENDING_KOREAN_FONT";

        // TMP Essentials 임포트 후 도메인 리로드되면 이어서 폰트 생성
        static EchoesFontSetup()
        {
            if (SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                EditorApplication.delayCall += () => Run(silent: false);
            }
        }

        [MenuItem("Tools/ECHOES/Setup Korean Font (TMP)")]
        public static void SetupMenu() => Run(silent: false);

        // 손상된 폰트 에셋을 깨끗하게 재생성 (런타임 아틀라스 참조 끊김 → MissingReferenceException 해결)
        [MenuItem("Tools/ECHOES/Rebuild Korean Font (Clean)")]
        public static void RebuildMenu()
        {
            if (!EditorUtility.DisplayDialog("ECHOES",
                "한글 폰트 에셋을 완전히 새로 만듭니다.\n" +
                "(손상된 아틀라스 참조 제거 → 단일 4096 아틀라스 + 멀티아틀라스 OFF)\n\n" +
                "기존 NotoSansKR SDF.asset 은 삭제 후 재생성됩니다. 계속할까요?",
                "재생성", "취소"))
                return;

            if (File.Exists(FontAssetPath))
                AssetDatabase.DeleteAsset(FontAssetPath);
            AssetDatabase.Refresh();
            Run(silent: false);
        }

        static void Run(bool silent)
        {
            // 1) TTF 존재 확인
            Font font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                Debug.LogError($"[ECHOES] 폰트 TTF 를 찾을 수 없습니다: {TtfPath}\n" +
                               "NotoSansKR-Regular.ttf 가 Assets/Fonts/ 에 있는지 확인하세요.");
                return;
            }

            // 2) TMP Essentials(설정 에셋) 없으면 자동 임포트 후 대기
            if (TMP_Settings.instance == null)
            {
                if (File.Exists(EssentialsPkg))
                {
                    Debug.Log("[ECHOES] TMP Essential Resources 가 없어 자동 임포트합니다. " +
                              "임포트가 끝나면 한글 폰트 세팅이 자동으로 이어집니다…");
                    SessionState.SetBool(PendingKey, true);
                    AssetDatabase.ImportPackage(EssentialsPkg, false);
                    return; // 임포트 → 도메인 리로드 → 생성자에서 이어짐
                }

                Debug.LogError("[ECHOES] TMP Settings 가 없습니다. " +
                               "Window ▸ TextMeshPro ▸ Import TMP Essential Resources 를 먼저 실행한 뒤 " +
                               "Tools ▸ ECHOES ▸ Setup Korean Font 를 다시 눌러주세요.");
                return;
            }

            // 3) 폰트 에셋 생성 (이미 있으면 재사용)
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = CreateDynamicFontAsset(font);
                if (fontAsset == null) return;
                Debug.Log($"[ECHOES] TMP 폰트 에셋 생성 완료: {FontAssetPath} (Dynamic)");
            }
            else
            {
                Debug.Log($"[ECHOES] 기존 폰트 에셋 재사용: {FontAssetPath}");
            }

            // 4) TMP Settings 에 Default + Fallback 등록
            RegisterWithTMPSettings(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
                EditorUtility.DisplayDialog("ECHOES",
                    "한글 TMP 폰트 세팅 완료!\n\n" +
                    "• NotoSansKR SDF (Dynamic) 생성\n" +
                    "• TMP 기본 폰트 + 폴백으로 등록\n\n" +
                    "이제 모든 TMP 텍스트에서 한글이 정상 표시됩니다.", "확인");
        }

        static TMP_FontAsset CreateDynamicFontAsset(Font font)
        {
            // Dynamic: 90pt 샘플링, SDFAA, 단일 4096 아틀라스, 런타임 글리프 렌더.
            // ⚠️ 멀티아틀라스는 반드시 OFF. ON 이면 런타임에 2번째 아틀라스 페이지가
            //    생성되는데, 이 페이지는 영구 서브에셋이 아니라 도메인 리로드 후
            //    참조가 끊겨 MissingReferenceException 이 매 프레임 터진다.
            //    4096 한 장이면 한글 사용량(<500자)이 모두 들어가 2번째 페이지가
            //    필요 없다 → 손상 원천 차단. (4096²/99² ≈ 1700자 수용)
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 4096,
                atlasHeight: 4096,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: false);

            if (asset == null)
            {
                Debug.LogError("[ECHOES] TMP_FontAsset.CreateFontAsset 실패.");
                return null;
            }

            asset.name = "NotoSansKR SDF";
            AssetDatabase.CreateAsset(asset, FontAssetPath);

            // 모든 아틀라스 텍스처를 서브 에셋으로 중첩 (멀티아틀라스 OFF 라 보통 1장)
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    var tex = asset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = asset.name + (i == 0 ? " Atlas" : $" Atlas {i}");
                    AssetDatabase.AddObjectToAsset(tex, asset);
                }
            }
            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        static void RegisterWithTMPSettings(TMP_FontAsset fontAsset)
        {
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null) return;

            var so = new SerializedObject(settings);

            // 기본 폰트
            var defProp = so.FindProperty("m_defaultFontAsset");
            if (defProp != null) defProp.objectReferenceValue = fontAsset;

            // 전역 폴백 목록 (중복 방지 후 추가)
            var fbProp = so.FindProperty("m_fallbackFontAssets");
            if (fbProp != null)
            {
                bool exists = false;
                for (int i = 0; i < fbProp.arraySize; i++)
                {
                    if (fbProp.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset) { exists = true; break; }
                }
                if (!exists)
                {
                    fbProp.arraySize++;
                    fbProp.GetArrayElementAtIndex(fbProp.arraySize - 1).objectReferenceValue = fontAsset;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            Debug.Log("[ECHOES] TMP Settings 에 기본 폰트 + 폴백 등록 완료.");
        }
    }
}
#endif
