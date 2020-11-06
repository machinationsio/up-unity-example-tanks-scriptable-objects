using System;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class MachiCP : EditorWindow
    {

        string myString = "Hello World";
        bool groupEnabled;
        bool myBool = true;
        float myFloat = 1.23f;

        [InitializeOnLoadMethod] 
        static public void InitUpdate() { EditorApplication.update += Update; }
        
        void OnEnable() { EditorApplication.update += Update; }
        void OnDisable() { EditorApplication.update -= Update; }
        
        [MenuItem("Tools/Machinations/Show Window")]
        static public void ShowWindow ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
        }

        void OnGUI ()
        {
            GUILayout.Label("Base Settings", EditorStyles.boldLabel);
            myString = EditorGUILayout.TextField("Text Field", myString);
            EditorGUILayout.TextField("Text Field", "bye world 3");

            groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            myBool = EditorGUILayout.Toggle("Toggle", myBool);
            myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            EditorGUILayout.EndToggleGroup();
        }

        static public void Update ()
        {
//            Debug.Log("update");
            //throw new NotImplementedException();
        }

        private void OnDestroy ()
        {
            //throw new NotImplementedException();
        }
        
        

    }
}