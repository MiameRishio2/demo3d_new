namespace MiTschMR.Skills
{
    using UnityEditor;
    using UnityEngine;

    [CustomEditor(typeof(SkillBarElement))]
    public class SkillBarElementEditor : Editor
    {
        private const string PROP_TARGET = "target";
        private const string PROP_KEYCODE = "keyCode";
        private const string PROP_SKILLICON = "skillIcon";
        private const string PROP_SKILLNOTASSIGNED = "skillNotAssigned";
        private const string PROP_SKILLCOOLDOWNICON = "skillCooldownIcon";
        private const string PROP_SHOWSKILLEXECUTINGICON = "showSkillExecutingIcon";
        private const string PROP_SKILLEXECUTINGICON = "skillExecutingIcon";
        private const string PROP_SHOWKEYCODETEXT = "showKeyCodeText";
        private const string PROP_KEYCODETEXT = "keyCodeText";

        // PROPERTIES: ----------------------------------------------------------------------------

        protected SerializedProperty spTarget;
        protected SerializedProperty spKeyCode;
        protected SerializedProperty spSkillIcon;
        protected SerializedProperty spSkillNotAssigned;
        protected SerializedProperty spSkillCooldownIcon;
        protected SerializedProperty spShowSkillExecutingIcon;
        protected SerializedProperty spSkillExecutingIcon;
        protected SerializedProperty spShowKeyCodeText;
        protected SerializedProperty spKeyCodeText;

        protected virtual void OnEnable()
        {
            this.spTarget = serializedObject.FindProperty(PROP_TARGET);
            this.spKeyCode = serializedObject.FindProperty(PROP_KEYCODE);
            this.spSkillIcon = serializedObject.FindProperty(PROP_SKILLICON);
            this.spSkillNotAssigned = serializedObject.FindProperty(PROP_SKILLNOTASSIGNED);
            this.spSkillCooldownIcon = serializedObject.FindProperty(PROP_SKILLCOOLDOWNICON);
            this.spShowSkillExecutingIcon = serializedObject.FindProperty(PROP_SHOWSKILLEXECUTINGICON);
            this.spSkillExecutingIcon = serializedObject.FindProperty(PROP_SKILLEXECUTINGICON);
            this.spShowKeyCodeText = serializedObject.FindProperty(PROP_SHOWKEYCODETEXT);
            this.spKeyCodeText = serializedObject.FindProperty(PROP_KEYCODETEXT);
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null) return;

            this.serializedObject.Update();

            EditorGUILayout.PropertyField(this.spTarget);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this.spKeyCode);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(this.spSkillIcon);
            EditorGUILayout.PropertyField(this.spSkillNotAssigned);
            EditorGUILayout.PropertyField(this.spSkillCooldownIcon);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(this.spShowSkillExecutingIcon);
            EditorGUI.BeginDisabledGroup(this.spShowSkillExecutingIcon.boolValue == false);
            EditorGUILayout.PropertyField(this.spSkillExecutingIcon, GUIContent.none);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(this.spShowKeyCodeText);
            EditorGUI.BeginDisabledGroup(this.spShowKeyCodeText.boolValue == false);
            EditorGUILayout.PropertyField(this.spKeyCodeText, GUIContent.none);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            this.serializedObject.ApplyModifiedProperties();
        }
    }
}
