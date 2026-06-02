#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// 보스 스프라이트 시트를 모던 SpriteDataProvider API 로 균일 격자 슬라이스한다.
/// 트리거: 프로젝트 루트(혹은 Assets 옆)에 "BossSliceRequest.flag" 파일이 있으면
/// 도메인 리로드 시 자동 실행 후 플래그 삭제.
/// 메뉴로도 실행 가능: Tools/Boss/SliceSprites
/// </summary>
[InitializeOnLoad]
public static class BossSlicer
{
    const string Flag = "BossSliceRequest.flag"; // 프로젝트 루트 기준
    const string Dir = "Assets/Images/Boss/";

    static readonly (string name, int cols)[] Sheets =
    {
        ("Boss_Idle", 4),
        ("Boss_Walk", 6),
        ("Boss_Attack", 6),
        ("Boss_Death", 6),
        ("Boss_Fireball", 5),
        ("Boss_Hit", 2),
    };

    static BossSlicer()
    {
        EditorApplication.delayCall += () =>
        {
            if (File.Exists(Flag))
            {
                File.Delete(Flag);
                SliceAll();
            }
        };
    }

    [MenuItem("Tools/Boss/SliceSprites")]
    public static void SliceAll()
    {
        foreach (var s in Sheets)
            SliceOne(Dir + s.name + ".png", s.cols);
        AssetDatabase.Refresh();
        Debug.Log("[BossSlicer] 전체 슬라이스 완료");
    }

    static void SliceOne(string path, int cols)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogError("[BossSlicer] 임포터 없음: " + path); return; }

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.filterMode = FilterMode.Bilinear;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.SaveAndReimport();

        ti.GetSourceTextureWidthAndHeight(out int W, out int H);
        int cellW = W / cols;
        string nm = Path.GetFileNameWithoutExtension(path);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();

        var rects = new List<SpriteRect>();
        for (int c = 0; c < cols; c++)
        {
            rects.Add(new SpriteRect
            {
                name = nm + "_" + c,
                spriteID = GUID.Generate(),
                rect = new Rect(c * cellW, 0, cellW, H),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0f),
                border = Vector4.zero,
            });
        }
        dp.SetSpriteRects(rects.ToArray());

        // 이름<->ID 매핑 갱신 (Unity 2021+ 권장)
        var nameIdProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameIdProvider != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var r in rects)
                pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            nameIdProvider.SetNameFileIdPairs(pairs);
        }

        dp.Apply();
        ti.SaveAndReimport();
        Debug.Log($"[BossSlicer] {nm}: {W}x{H} cols={cols} cellW={cellW}");
    }
}
#endif
