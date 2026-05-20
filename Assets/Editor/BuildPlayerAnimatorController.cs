using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BuildPlayerAnimatorController
{
    private const string ControllerPath = "Assets/Player_System/PlayerAnimator.controller";

    private const string BlinkMovementRoot = "Assets/Player_System/Blink/Art/Animations/Animations_Starter_Pack/Movement";
    private const string MixamoRoot = "Assets/Player_System/MixamoAnimations";

    [MenuItem("Tools/Build Player Animator Controller")]
    public static void Build()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        ClearController(controller);
        AddParameters(controller);

        AnimatorControllerLayer layer = controller.layers[0];
        AnimatorStateMachine stateMachine = layer.stateMachine;

        AnimationClip idle = LoadClip($"{BlinkMovementRoot}/Idle.fbx", "Idle");
        AnimationClip runForward = LoadClip($"{BlinkMovementRoot}/RunForward.fbx", "RunForward");
        AnimationClip runBackward = LoadClip($"{BlinkMovementRoot}/RunBackward.fbx", "RunBackward");
        AnimationClip runLeft = LoadClip($"{BlinkMovementRoot}/RunLeft.fbx", "RunLeft");
        AnimationClip runRight = LoadClip($"{BlinkMovementRoot}/RunRight.fbx", "RunRight");
        AnimationClip sprint = LoadClip($"{BlinkMovementRoot}/Sprint.fbx", "Sprint");

        AnimationClip jump = LoadPreferredClip($"{BlinkMovementRoot}/Jumps.fbx", "Jump", "Jump_Up");
        AnimationClip fall = LoadClip($"{BlinkMovementRoot}/FallingLoop.fbx", "FallingLoop");

        AnimationClip crouchIdle = LoadFirstClip($"{MixamoRoot}/MX_CrouchIdle.fbx");
        AnimationClip crouchForward = LoadFirstClip($"{MixamoRoot}/MX_CrouchWalkForward.fbx");
        AnimationClip crouchBack = LoadFirstClip($"{MixamoRoot}/MX_CrouchWalkBack.fbx");
        AnimationClip crouchLeft = LoadFirstClip($"{MixamoRoot}/MX_CrouchWalkLeft.fbx");
        AnimationClip crouchRight = LoadFirstClip($"{MixamoRoot}/MX_CrouchWalkRight.fbx");
        AnimationClip landing = LoadFirstClip($"{MixamoRoot}/MX_Landing.fbx");

        if (idle == null || runForward == null || runBackward == null || runLeft == null || runRight == null ||
            jump == null || fall == null || crouchIdle == null || crouchForward == null || landing == null)
        {
            Debug.LogError("PlayerAnimator build failed. Missing one or more required animation clips.");
            return;
        }

        AnimatorState movementState = stateMachine.AddState("Movement", new Vector3(250, 100, 0));
        BlendTree movementTree = Create2DBlendTree(controller, "MovementBlendTree", "Horizontal", "Vertical");

        movementTree.AddChild(idle, new Vector2(0f, 0f));
        movementTree.AddChild(runForward, new Vector2(0f, 1f));
        movementTree.AddChild(runBackward, new Vector2(0f, -1f));
        movementTree.AddChild(runLeft, new Vector2(-1f, 0f));
        movementTree.AddChild(runRight, new Vector2(1f, 0f));

        if (sprint != null)
            movementTree.AddChild(sprint, new Vector2(0f, 1.35f));

        movementState.motion = movementTree;
        movementState.speed = 1f;

        AnimatorState crouchState = stateMachine.AddState("Crouch", new Vector3(250, 280, 0));
        BlendTree crouchTree = Create2DBlendTree(controller, "CrouchBlendTree", "Horizontal", "Vertical");

        crouchTree.AddChild(crouchIdle, new Vector2(0f, 0f));
        crouchTree.AddChild(crouchForward, new Vector2(0f, 1f));
        crouchTree.AddChild(crouchBack != null ? crouchBack : crouchForward, new Vector2(0f, -1f));
        crouchTree.AddChild(crouchLeft != null ? crouchLeft : crouchForward, new Vector2(-1f, 0f));
        crouchTree.AddChild(crouchRight != null ? crouchRight : crouchForward, new Vector2(1f, 0f));

        crouchState.motion = crouchTree;
        crouchState.speed = 1f;

        AnimatorState jumpState = stateMachine.AddState("Jump", new Vector3(540, 60, 0));
        jumpState.motion = jump;
        jumpState.speed = 0.85f;

        AnimatorState fallState = stateMachine.AddState("Fall", new Vector3(540, 210, 0));
        fallState.motion = fall;
        fallState.speed = 1f;

        AnimatorState landState = stateMachine.AddState("Land", new Vector3(540, 360, 0));
        landState.motion = landing;
        landState.speed = 1f;

        stateMachine.defaultState = movementState;

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("PlayerAnimator.controller rebuilt: Movement, Crouch, Jump, Fall, Land.");
    }

    private static void ClearController(AnimatorController controller)
    {
        foreach (AnimatorControllerParameter parameter in controller.parameters.ToArray())
            controller.RemoveParameter(parameter);

        if (controller.layers == null || controller.layers.Length == 0)
            controller.AddLayer("Base Layer");

        AnimatorControllerLayer layer = controller.layers[0];

        if (layer.stateMachine == null)
            layer.stateMachine = new AnimatorStateMachine { name = "Base Layer" };

        AnimatorStateMachine sm = layer.stateMachine;

        foreach (ChildAnimatorState state in sm.states.ToArray())
            sm.RemoveState(state.state);

        foreach (ChildAnimatorStateMachine child in sm.stateMachines.ToArray())
            sm.RemoveStateMachine(child.stateMachine);

        foreach (AnimatorStateTransition transition in sm.anyStateTransitions.ToArray())
            sm.RemoveAnyStateTransition(transition);

        foreach (AnimatorTransition transition in sm.entryTransitions.ToArray())
            sm.RemoveEntryTransition(transition);
    }

    private static void AddParameters(AnimatorController controller)
    {
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Horizontal", AnimatorControllerParameterType.Float);
        controller.AddParameter("Vertical", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isCrouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isSprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("JumpTrigger", AnimatorControllerParameterType.Trigger);
    }

    private static BlendTree Create2DBlendTree(AnimatorController controller, string name, string parameterX, string parameterY)
    {
        BlendTree tree = new BlendTree
        {
            name = name,
            blendType = BlendTreeType.FreeformCartesian2D,
            blendParameter = parameterX,
            blendParameterY = parameterY,
            useAutomaticThresholds = false
        };

        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    private static AnimationClip LoadPreferredClip(string path, params string[] names)
    {
        foreach (string name in names)
        {
            AnimationClip clip = LoadClip(path, name);

            if (clip != null)
                return clip;
        }

        return LoadFirstClip(path);
    }

    private static AnimationClip LoadClip(string path, string clipName)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        AnimationClip clip = assets
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c.name.Equals(clipName, StringComparison.OrdinalIgnoreCase));

        if (clip == null)
            Debug.LogWarning($"Could not find clip '{clipName}' at '{path}'.");

        return clip;
    }

    private static AnimationClip LoadFirstClip(string path)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        AnimationClip clip = assets
            .OfType<AnimationClip>()
            .FirstOrDefault(c =>
                c != null &&
                !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase) &&
                !c.name.Contains("Take 001"));

        if (clip == null)
        {
            clip = assets
                .OfType<AnimationClip>()
                .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        }

        if (clip == null)
            Debug.LogWarning($"Could not find any usable animation clip at '{path}'.");

        return clip;
    }
}
