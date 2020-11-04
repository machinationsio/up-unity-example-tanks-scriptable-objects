using UnityEngine;
using System.Collections;
using System.IO;
using MachinationsUP.Integration.Elements;
using UnityEditor;

namespace MachinationsUP.Engines.Unity.EditorExtensions
{
    [CustomPropertyDrawer(typeof(ElementBase), true)]
    public class ElementBaseDrawer : PropertyDrawer
    {
        
        override public void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, label); //This will change the value of position (see docs of PrefixLabel).

            //Draw a Machinations Icon.
            //First, load the image.
            Texture2D tex = new Texture2D(16, 16);
            //This will auto-resize the texture dimensions.
            tex.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, @"Scripts\MachinationsUP\zRes\logo-just-m-16.png")));

            //Now draw the image.
            GUIContent icon = new GUIContent(tex);
            Rect iconPosition = new Rect(position);
            iconPosition.x -= 42; //Because 42 :).
            EditorGUI.LabelField(iconPosition, icon);

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
                Debug.Log("Set basevalue to " + newValue);
                Debug.Log("The element's value is " + eb.CurrentValue);
                */
                baseValueProp.intValue = newValue;
                Debug.Log("Set basevalue to " + newValue);
                eb.ChangeValueFromEditor(newValue);
                Debug.Log("The element's value is " + eb.CurrentValue);
            }

            EditorGUI.EndProperty();
        }

    }
}