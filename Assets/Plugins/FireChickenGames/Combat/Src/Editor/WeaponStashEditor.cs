namespace FireChickenGames.Combat.Editor
{
    using GameCreator.Core;
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(WeaponStash))]
    public class WeaponStashEditor : Editor
    {
        protected WeaponStash instance;

        public SerializedProperty character;
        public SerializedProperty weapons;
        public SerializedProperty stashedWeapon;
        public SerializedProperty stashedAmmo;

        public SerializedProperty weaponStashUi;

        protected void OnEnable()
        {
            instance = target as WeaponStash;

            character = serializedObject.FindProperty("character");

            weapons = serializedObject.FindProperty("weapons");
            stashedWeapon = serializedObject.FindProperty("_stashedWeapon");
            stashedAmmo = serializedObject.FindProperty("stashedAmmo");

            weaponStashUi = serializedObject.FindProperty("weaponStashUi");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(character, new GUIContent("Target"));
            EditorGUILayout.PropertyField(weaponStashUi, new GUIContent("UI"));

            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.PropertyField(stashedWeapon);
            EditorGUILayout.PropertyField(stashedAmmo);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(weapons);

            EditorGUI.EndDisabledGroup();
        }
    }
}
