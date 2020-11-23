using UnityEngine;
using MachinationsUP.Engines.Unity.Editor.Graphics;
using MachinationsUP.Integration.Elements;
using UnityEditor;
using MachinationsUP.Logger;

namespace MachinationsUP.Engines.Unity.EditorExtensions
{
    [CustomPropertyDrawer(typeof(ElementBase), true)]
    public class ElementBaseDrawer : PropertyDrawer
    {

        private MachiCPGraphics cpGraphics;

        override public void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label); //This will change the value of position (see docs of PrefixLabel).

            //Get Scriptable Object with Control Panel Graphics.
            if (cpGraphics == null)
            {
                string assetGUID = AssetDatabase.FindAssets("t:MachiCPGraphics")[0];
                string path = AssetDatabase.GUIDToAssetPath(assetGUID);
                cpGraphics = (MachiCPGraphics) AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
            else
            {
                //Draw the Machinations Icon.
                GUIContent icon = new GUIContent(cpGraphics.MachinationsIcon);
                Rect iconPosition = new Rect(position);
                iconPosition.x -= 42; //Because 42 :).
                EditorGUI.LabelField(iconPosition, icon);
            }

            //Get the value from ElementBase's member.
            ElementBase eb = fieldInfo.GetValue(property.serializedObject.targetObject) as ElementBase;
            SerializedProperty baseValueProp = property.FindPropertyRelative("_serializableValue");
            //Store it here.
            int baseValue = baseValueProp.intValue;

            //Edit.
            EditorGUI.BeginChangeCheck();
            int newValue = EditorGUI.IntField(position, baseValue); //Get new value.
            if (EditorGUI.EndChangeCheck())
            {
                /*
                baseValueProp.intValue = newValue; //Store the new value.
                L.D("Set basevalue to " + newValue);
                L.D("The element's value is " + eb.CurrentValue);
                */
                baseValueProp.intValue = newValue;
                L.D("Set basevalue to " + newValue);
                eb.ChangeValueFromEditor(newValue);
                L.D("The element's value is " + eb.CurrentValue);
            }

            EditorGUI.EndProperty();
        }

    }
}