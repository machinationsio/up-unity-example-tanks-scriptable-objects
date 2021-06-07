using UnityEngine;
using MachinationsUP.Engines.Unity.Editor.Graphics;
using MachinationsUP.Integration.Elements;
using UnityEditor;
using MachinationsUP.Logger;

namespace MachinationsUP.Engines.Unity.EditorExtensions
{
    
    /// <summary>
    /// Custom-draws Machinations Elements in the Unity Property Inspector.
    /// For now, it mainly handles taking the proper value to be drawn, and adding a Machinations icon, to illustrate that the value
    /// is bound to Machinations.
    /// </summary>
    [CustomPropertyDrawer(typeof(ElementBase), true)]
    public class ElementBaseDrawer : PropertyDrawer
    {

        private MnCPanelGraphics cpGraphics;

        override public void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            //Get the ElementBase for which this Property Drawer is used.
            ElementBase eb = fieldInfo.GetValue(property.serializedObject.targetObject) as ElementBase;

            //Add the diagram ID in the label.
            if (eb?.DiagMapping != null)
            {
                //label.text += " [" + eb.DiagMapping.DiagramElementID + "]";
            }

            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label); //This will change the value of position (see docs of PrefixLabel).

            //Get Scriptable Object with Control Panel Graphics.
            if (cpGraphics == null)
            {
                string assetGUID = AssetDatabase.FindAssets("t:MnCPanelGraphics")[0];
                string path = AssetDatabase.GUIDToAssetPath(assetGUID);
                cpGraphics = (MnCPanelGraphics) AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
            else
            {
                //Draw the Machinations Icon.
                GUIContent icon = new GUIContent(cpGraphics.MachinationsIcon);
                Rect iconPosition = new Rect(position);
                iconPosition.x -= 42; //Because 42 :).
                EditorGUI.LabelField(iconPosition, icon);
            }

            //Get the Base Value from ElementBase's member.
            SerializedProperty baseValueProp = property.FindPropertyRelative("_serializableValue");
            //Store it here.
            int baseValue = baseValueProp.intValue;

            //Can only proceed if we have an ElementBase to work with.
            if (eb == null) return;
            
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
                L.T("Set basevalue to " + newValue);
                eb.ChangeValueFromEditor(newValue);
                L.T("The element's value is " + eb.CurrentValue);
            }

            EditorGUI.EndProperty();
        }

    }
}