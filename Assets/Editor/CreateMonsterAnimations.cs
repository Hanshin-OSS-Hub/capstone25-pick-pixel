
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Temporary editor utility – run via  Tools > Create Monster Animations
/// Creates Walk/Attack .anim clips for Oni, Tiger, Zombie and prints their GUIDs.
/// </summary>
public static class CreateMonsterAnimations
{
    [MenuItem("Tools/Create Monster Animations")]
    public static void Run()
    {
        // ── Oni ──────────────────────────────────────────────────────────
        CreateWalkAttack(
            walkTex:   "Assets/Images/Monster Image/도깨비 이동 모션.png",
            attackTex: "Assets/Images/Monster Image/도깨비 공격모션.png",
            walkSave:   "Assets/Animations/Monster/Oni/Oni_Walk.anim",
            attackSave: "Assets/Animations/Monster/Oni/Oni_Attack.anim",
            walkName:   "Oni_Walk",
            attackName: "Oni_Attack",
            splitAtHalf: false);

        // ── Tiger ─────────────────────────────────────────────────────────
        CreateWalkAttack(
            walkTex:   "Assets/Images/Monster Image/호랑이 이동.png",
            attackTex: "Assets/Images/Monster Image/호랑이 공격.png",
            walkSave:   "Assets/Animations/Monster/Tiger/Tiger_Walk.anim",
            attackSave: "Assets/Animations/Monster/Tiger/Tiger_Attack.anim",
            walkName:   "Tiger_Walk",
            attackName: "Tiger_Attack",
            splitAtHalf: false);

        // ── Zombie (combined sheet: first half = walk, second half = attack)
        CreateWalkAttack(
            walkTex:   "Assets/Images/Monster Image/강시 이동,공격모션.png",
            attackTex: "Assets/Images/Monster Image/강시 이동,공격모션.png",
            walkSave:   "Assets/Animations/Monster/Zombie/Zombie_Walk.anim",
            attackSave: "Assets/Animations/Monster/Zombie/Zombie_Attack.anim",
            walkName:   "Zombie_Walk",
            attackName: "Zombie_Attack",
            splitAtHalf: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== Monster Animation GUIDs ===");
        Debug.Log("Oni_Walk:      " + AssetDatabase.AssetPathToGUID("Assets/Animations/Monster/Oni/Oni_Walk.anim"));
        Debug.Log("Oni_Attack:    " + AssetDatabase.AssetPathToGUID("Assets/Animations/Monster/Oni/Oni_Attack.anim"));
        Debug.Log("Tiger_Walk:    " + AssetDatabase.AssetPathToGUID("Assets/Animations/Monster/Tiger/Tiger_Walk.anim"));
        Debug.Log("Tiger_Attack:  " + AssetDatabase.AssetPathToGUID("Assets/Animations/Monster/Tiger/Tiger_Attack.anim"));
        Debug.Log("Zombie_Walk:   " + AssetDatabase.AssetPathToGUID("Assets/Animations/Monster/Zombie/Zombie_Walk.anim"));
        Debug.Log("Zombie_Attack: " + AssetDatabase.AssetPathToGUID("Assets/Animations/Monster/Zombie/Zombie_Attack.anim"));
        Debug.Log("=== Done ===");
    }

    // ──────────────────────────────────────────────────────────────────────
    static void CreateWalkAttack(
        string walkTex, string attackTex,
        string walkSave, string attackSave,
        string walkName, string attackName,
        bool splitAtHalf)
    {
        if (splitAtHalf)
        {
            // Both walk and attack come from the SAME texture
            Sprite[] all = LoadSprites(walkTex);
            int half = all.Length / 2;

            Sprite[] walkS   = new Sprite[half];
            Sprite[] attackS = new Sprite[all.Length - half];
            System.Array.Copy(all, 0,    walkS,   0, half);
            System.Array.Copy(all, half, attackS, 0, all.Length - half);

            SaveClip(MakeClip(walkS,   walkName,   12f, true),  walkSave);
            SaveClip(MakeClip(attackS, attackName, 12f, false), attackSave);
        }
        else
        {
            Sprite[] walkS   = LoadSprites(walkTex);
            Sprite[] attackS = LoadSprites(attackTex);
            SaveClip(MakeClip(walkS,   walkName,   12f, true),  walkSave);
            SaveClip(MakeClip(attackS, attackName, 12f, false), attackSave);
        }
    }

    static Sprite[] LoadSprites(string texPath)
    {
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(texPath);
        var list = new List<Sprite>();
        foreach (var a in all)
            if (a is Sprite s) list.Add(s);

        list.Sort((a, b) => {
            int na = 0, nb = 0;
            string[] pa = a.name.Split('_');
            string[] pb = b.name.Split('_');
            int.TryParse(pa[pa.Length - 1], out na);
            int.TryParse(pb[pb.Length - 1], out nb);
            return na.CompareTo(nb);
        });
        return list.ToArray();
    }

    static AnimationClip MakeClip(Sprite[] sprites, string clipName, float fps, bool loop)
    {
        var clip = new AnimationClip();
        clip.name = clipName;
        clip.frameRate = fps;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        return clip;
    }

    static void SaveClip(AnimationClip clip, string path)
    {
        // Delete existing to avoid "asset already exists" error on re-run
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        Debug.Log("Created: " + path);
    }
}
