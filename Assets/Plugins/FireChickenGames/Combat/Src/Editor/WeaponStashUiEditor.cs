namespace FireChickenGames.Combat.Editor
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(WeaponStashUi))]
    public class WeaponStashUiEditor : Editor
    {
        protected WeaponStash instance;

        public SerializedProperty weaponNameText;
        public SerializedProperty weaponDescriptionText;

        public SerializedProperty ammoNameText;
        public SerializedProperty ammoDescriptionText;

        public SerializedProperty ammoInClipText;
        public SerializedProperty ammoMaxClipText;
        public SerializedProperty ammoInStorageText;

        protected void OnEnable()
        {
            instance = target as WeaponStash;

            weaponNameText = serializedObject.FindProperty("weaponNameText");
            weaponDescriptionText = serializedObject.FindProperty("weaponDescriptionText");

            ammoNameText = serializedObject.FindProperty("ammoNameText");
            ammoDescriptionText = serializedObject.FindProperty("ammoDescriptionText");

            ammoInClipText = serializedObject.FindProperty("ammoInClipText");
            ammoMaxClipText = serializedObject.FindProperty("ammoMaxClipText");
            ammoInStorageText = serializedObject.FindProperty("ammoInStorageText");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(weaponNameText, new GUIContent("UI"));
            EditorGUILayout.PropertyField(weaponDescriptionText, new GUIContent("UI"));

            EditorGUILayout.PropertyField(ammoNameText, new GUIContent("UI"));
            EditorGUILayout.PropertyField(ammoDescriptionText, new GUIContent("UI"));

            EditorGUILayout.PropertyField(ammoInClipText, new GUIContent("UI"));
            EditorGUILayout.PropertyField(ammoMaxClipText, new GUIContent("UI"));
            EditorGUILayout.PropertyField(ammoInStorageText, new GUIContent("UI"));
        }
    }
}
