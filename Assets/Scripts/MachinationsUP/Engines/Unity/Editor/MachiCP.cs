using MachinationsUP.Config;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class MachiCP : EditorWindow
    {

        private string _APIURL = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket";
        private string _userKey = "4fc8b3a7-3909-43b6-96a0-0387cd85e896";
        private string _gameName = "Tanks";
        private string _diagramToken = "54dc6ccd-1f66-41d8-a8d5-0873e935a770";
        
        [MenuItem("Tools/Machinations/Open Machinations.io Control Panel")]
        static public void ShowWindow ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
        }
        
        [MenuItem("Tools/Machinations/Pause Sync")]
        static public void PauseSync ()
        {
            MachinationsDataLayer.Service.PauseSync();
        }
        
        [MenuItem("Tools/Machinations/Launch Machinations.io")]
        static public void Launch ()
        {
            System.Diagnostics.Process.Start("http://www.machinations.io");
        }
        
        void OnGUI ()
        {
            GUILayout.Label("Machinations.io Connection Settings", EditorStyles.boldLabel);
            _APIURL = EditorGUILayout.TextField("API URL", _APIURL);
            _userKey = EditorGUILayout.TextField("User Key", _userKey);
            _gameName = EditorGUILayout.TextField("Game Name", _gameName);
            _diagramToken = EditorGUILayout.TextField("Diagram Token", _diagramToken);
        }

        void OnEnable ()
        {
            SaveMachinationsConfig();
        }

        private void OnValidate ()
        {
            SaveMachinationsConfig();
        }

        private void SaveMachinationsConfig ()
        {
            Debug.Log("Saving Machinations Config.");
            MachinationsConfig.Instance.APIURL = _APIURL;
            MachinationsConfig.Instance.UserKey = _userKey;
            MachinationsConfig.Instance.GameName = _gameName;
            MachinationsConfig.Instance.DiagramToken = _diagramToken;
            MachinationsConfig.SaveSettings();
        }

        void OnDisable ()
        {
            SaveMachinationsConfig();
        }
        
        void OnDestroy ()
        {
            SaveMachinationsConfig();
        }

    }
}