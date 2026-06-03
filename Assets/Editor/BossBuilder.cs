#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 슬라이스된 보스 스프라이트로 AnimationClip 6종 + AnimatorController 를 생성한다.
/// 메뉴: Tools/Boss/BuildAnimator
/// </summary>
[InitializeOnLoad]
public static class BossBuilder
{
    const string Dir = "Assets/Images/Boss/";
    const string AnimDir = "Assets/Animations/Boss/";
    const string Flag = "BossBuildRequest.flag";

    static BossBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            if (System.IO.File.Exists(Flag))
            {
                System.IO.File.Delete(Flag);
                Build();
            }
        };
    }

    [MenuItem("Tools/Boss/BuildAnimator")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations/Boss"))
            AssetDatabase.CreateFolder("Assets/Animations", "Boss");

        var idle = MakeClip("Boss_Idle", "Boss_Idle", 8f, true);
        var walk = MakeClip("Boss_Walk", "Boss_Walk", 10f, true);
        var atk  = MakeClip("Boss_Attack", "Boss_Attack", 12f, false);
        var fire = MakeClip("Boss_Fireball", "Boss_Fireball", 10f, false);
        var hit  = MakeClip("Boss_Hit", "Boss_Hit", 10f, false);
        var death= MakeClip("Boss_Death", "Boss_Death", 8f, false);

        string ctrlPath = AnimDir + "Boss.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Fireball", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        var sm = ctrl.layers[0].stateMachine;
        var sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        var sWalk = sm.AddState("Walk"); sWalk.motion = walk;
        var sAtk  = sm.AddState("Attack"); sAtk.motion = atk;
        var sFire = sm.AddState("Fireball"); sFire.motion = fire;
        var sHit  = sm.AddState("Hit"); sHit.motion = hit;
        var sDeath= sm.AddState("Death"); sDeath.motion = death;
        sm.defaultState = sIdle;

        // Idle <-> Walk (Speed)
        var iw = sIdle.AddTransition(sWalk); iw.hasExitTime = false; iw.duration = 0.05f;
        iw.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var wi = sWalk.AddTransition(sIdle); wi.hasExitTime = false; wi.duration = 0.05f;
        wi.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // AnyState -> Attack / Fireball / Hit (트리거 + 죽지 않았을 때)
        AddAny(sm, sAtk,  "Attack");
        AddAny(sm, sFire, "Fireball");
        AddAny(sm, sHit,  "Hit");

        // AnyState -> Death (Dead == true)
        var ad = sm.AddAnyStateTransition(sDeath);
        ad.hasExitTime = false; ad.duration = 0.05f; ad.canTransitionToSelf = false;
        ad.AddCondition(AnimatorConditionMode.If, 0, "Dead");

        // 공격/파이어볼/피격 -> Idle (exit time)
        BackToIdle(sAtk, sIdle);
        BackToIdle(sFire, sIdle);
        BackToIdle(sHit, sIdle);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BossBuilder] Animator + 클립 6종 생성 완료: " + ctrlPath);
    }

    static void AddAny(AnimatorStateMachine sm, AnimatorState dst, string trigger)
    {
        var t = sm.AddAnyStateTransition(dst);
        t.hasExitTime = false; t.duration = 0.03f; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0, trigger);
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
    }

    static void BackToIdle(AnimatorState from, AnimatorState idle)
    {
        var t = from.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.05f;
    }

    static AnimationClip MakeClip(string sheet, string clipName, float fps, bool loop)
    {
        string sheetPath = Dir + sheet + ".png";
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
            .OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
        if (sprites.Length == 0) { Debug.LogError("[BossBuilder] 스프라이트 없음: " + sheetPath); return null; }

        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = AnimDir + clipName + ".anim";
        AssetDatabase.DeleteAsset(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);
        Debug.Log($"[BossBuilder] clip {clipName}: {sprites.Length} frames @ {fps}fps loop={loop}");
        return clip;
    }
}
#endif
