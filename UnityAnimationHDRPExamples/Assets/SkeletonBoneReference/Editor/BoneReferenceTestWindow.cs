using System;
using UnityEditor;
using Unity.Animation.Authoring;

[Serializable]
class BoneReferenceTestWindow : EditorWindow
{
    [MenuItem("Window/Bone Reference Test Window")]
    static void Init()
    {
        var window = (BoneReferenceTestWindow)EditorWindow.GetWindow(typeof(BoneReferenceTestWindow));
        window.Show();
    }

    public SkeletonBoneReference skeletonBoneReference1 = default;
    public SkeletonBoneReference skeletonBoneReference2 = default;


    public Skeleton defaultSkeleton1 = default;
    public Skeleton defaultSkeletonB => defaultSkeleton1;
    //[ShowFullPath]
    [SkeletonReference(nameof(defaultSkeleton1))] public TransformBindingID transformBindingID1 = default;
    //[ShowFullPath]
    [SkeletonReference(nameof(defaultSkeletonB))] public TransformBindingID transformBindingID2 = default;


    public Skeleton defaultSkeleton2 = default;
    //[ShowFullPath]
    [SkeletonReference(nameof(defaultSkeleton2))] public TransformBindingID transformBindingID3 = default;


    public SerializedObject     serializedObject;

    public SerializedProperty   skeletonBoneReference1Property;
    public SerializedProperty   skeletonBoneReference2Property;

    public SerializedProperty   defaultSkeleton1Property;
    public SerializedProperty   transformBindingID1Property;
    public SerializedProperty   transformBindingID2Property;

    public SerializedProperty   defaultSkeleton2Property;
    public SerializedProperty   transformBindingID3Property;

    public void OnEnable()
    {
        autoRepaintOnSceneChange = true;
        serializedObject = new SerializedObject(this);
        skeletonBoneReference1Property  = serializedObject.FindProperty(nameof(skeletonBoneReference1));
        skeletonBoneReference2Property  = serializedObject.FindProperty(nameof(skeletonBoneReference2));

        defaultSkeleton1Property        = serializedObject.FindProperty(nameof(defaultSkeleton1));
        transformBindingID1Property     = serializedObject.FindProperty(nameof(transformBindingID1));
        transformBindingID2Property     = serializedObject.FindProperty(nameof(transformBindingID2));

        defaultSkeleton2Property        = serializedObject.FindProperty(nameof(defaultSkeleton2));
        transformBindingID3Property     = serializedObject.FindProperty(nameof(transformBindingID3));
    }

    public void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("SkeletonBoneReference", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skeletonBoneReference1Property);
            EditorGUILayout.PropertyField(skeletonBoneReference2Property);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skeleton", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSkeleton1Property);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("TransformBindingID", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(transformBindingID1Property);
            EditorGUILayout.PropertyField(transformBindingID2Property);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skeleton", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSkeleton2Property);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("TransformBindingID", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(transformBindingID3Property);
        }
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
