using System;
using UnityEditor;
using Unity.Animation.Authoring;
using Unity.Animation.Hybrid;
using UnityEngine;

[Serializable]
class BoneReferenceTestWindow : EditorWindow
{
    // SkeletonBoneReferences (using contained Skeleton)
    public SkeletonBoneReference skeletonBoneReference1 = default;
    public SkeletonBoneReference skeletonBoneReference2 = default;
    public SkeletonBoneReference skeletonBoneReferenceC = default;


    // SkeletonBoneReference with Skeleton Override Field
    [SerializeField] Skeleton defaultSkeleton1 = default;
    [SkeletonReference(nameof(defaultSkeleton1))] public SkeletonBoneReference skeletonBoneReference3 = default;
    [SkeletonReference(nameof(defaultSkeleton1))] public SkeletonBoneReference skeletonBoneReference4 = default;


    // SkeletonBoneReference with Skeleton Override Property
    [SerializeField] Skeleton defaultSkeleton2 = default;
    public Skeleton defaultSkeleton2Property => defaultSkeleton2;
    [SkeletonReference(nameof(defaultSkeleton2Property))] public SkeletonBoneReference skeletonBoneReference5 = default;
    [SkeletonReference(nameof(defaultSkeleton2Property))] public SkeletonBoneReference skeletonBoneReference6 = default;


    // TransformBindingID with Skeleton Property
    [SerializeField] Skeleton defaultSkeleton3 = default;
    public Skeleton defaultSkeleton3Property => defaultSkeleton3;
    [SkeletonReference(nameof(defaultSkeleton3Property))] public TransformBindingID transformBindingID1 = default;
    [SkeletonReference(nameof(defaultSkeleton3Property))] public TransformBindingID transformBindingID2 = default;


    // TransformBindingID with Skeleton Field
    public Skeleton defaultSkeleton4 = default;
    [SkeletonReference(nameof(defaultSkeleton4))] public TransformBindingID transformBindingID3 = default;
    [SkeletonReference(nameof(defaultSkeleton4))] public TransformBindingID transformBindingID4 = default;


    public SerializedObject   serializedObject;

    public SerializedProperty skeletonBoneReference1SerializedProperty;
    public SerializedProperty skeletonBoneReference2SerializedProperty;
    public SerializedProperty skeletonBoneReferenceCSerializedProperty;

    public SerializedProperty defaultSkeleton1SerializedProperty;
    public SerializedProperty skeletonBoneReference3SerializedProperty;
    public SerializedProperty skeletonBoneReference4SerializedProperty;

    public SerializedProperty defaultSkeleton2SerializedProperty;
    public SerializedProperty skeletonBoneReference5SerializedProperty;
    public SerializedProperty skeletonBoneReference6SerializedProperty;

    public SerializedProperty defaultSkeleton3SerializedProperty;
    public SerializedProperty transformBindingID1SerializedProperty;
    public SerializedProperty transformBindingID2SerializedProperty;

    public SerializedProperty defaultSkeleton4SerializedProperty;
    public SerializedProperty transformBindingID3SerializedProperty;
    public SerializedProperty transformBindingID4SerializedProperty;


    public void OnEnable()
    {
        autoRepaintOnSceneChange = true;
        serializedObject = new SerializedObject(this);
        skeletonBoneReference1SerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReference1));
        skeletonBoneReference2SerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReference2));
        skeletonBoneReferenceCSerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReferenceC));

        defaultSkeleton1SerializedProperty          = serializedObject.FindProperty(nameof(defaultSkeleton1));
        skeletonBoneReference3SerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReference3));
        skeletonBoneReference4SerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReference4));

        defaultSkeleton2SerializedProperty          = serializedObject.FindProperty(nameof(defaultSkeleton2));
        skeletonBoneReference5SerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReference5));
        skeletonBoneReference6SerializedProperty    = serializedObject.FindProperty(nameof(skeletonBoneReference6));

        defaultSkeleton3SerializedProperty          = serializedObject.FindProperty(nameof(defaultSkeleton3));
        transformBindingID1SerializedProperty       = serializedObject.FindProperty(nameof(transformBindingID1));
        transformBindingID2SerializedProperty       = serializedObject.FindProperty(nameof(transformBindingID2));

        defaultSkeleton4SerializedProperty          = serializedObject.FindProperty(nameof(defaultSkeleton4));
        transformBindingID3SerializedProperty       = serializedObject.FindProperty(nameof(transformBindingID3));
        transformBindingID4SerializedProperty       = serializedObject.FindProperty(nameof(transformBindingID4));
    }

    public void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("SkeletonBoneReference (using contained Skeleton)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(skeletonBoneReference1SerializedProperty);
            EditorGUILayout.PropertyField(skeletonBoneReference2SerializedProperty);
            EditorGUILayout.PropertyField(skeletonBoneReferenceCSerializedProperty);

            EditorGUILayout.Space(40);
            EditorGUILayout.LabelField("SkeletonBoneReference with Skeleton Override Field", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSkeleton1SerializedProperty, EditorGUIUtility.TrTextContent("Skeleton Property"));
            EditorGUILayout.PropertyField(skeletonBoneReference3SerializedProperty);
            EditorGUILayout.PropertyField(skeletonBoneReference4SerializedProperty);

            EditorGUILayout.Space(40);
            EditorGUILayout.LabelField("SkeletonBoneReference with Skeleton Override Property", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSkeleton2SerializedProperty, EditorGUIUtility.TrTextContent("Skeleton Field"));
            EditorGUILayout.PropertyField(skeletonBoneReference5SerializedProperty);
            EditorGUILayout.PropertyField(skeletonBoneReference6SerializedProperty);

            EditorGUILayout.Space(40);
            EditorGUILayout.LabelField("TransformBindingID with Skeleton Property", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSkeleton3SerializedProperty, EditorGUIUtility.TrTextContent("Skeleton Property"));
            EditorGUILayout.PropertyField(transformBindingID1SerializedProperty);
            EditorGUILayout.PropertyField(transformBindingID2SerializedProperty);

            EditorGUILayout.Space(40);
            EditorGUILayout.LabelField("TransformBindingID with Skeleton Field", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSkeleton4SerializedProperty, EditorGUIUtility.TrTextContent("Skeleton Field"));
            EditorGUILayout.PropertyField(transformBindingID3SerializedProperty);
            EditorGUILayout.PropertyField(transformBindingID4SerializedProperty);
        }
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    [MenuItem("Window/Bone Reference Test Window")]
    static void Init()
    {
        var window = (BoneReferenceTestWindow)EditorWindow.GetWindow(typeof(BoneReferenceTestWindow));
        window.Show();
    }
}
