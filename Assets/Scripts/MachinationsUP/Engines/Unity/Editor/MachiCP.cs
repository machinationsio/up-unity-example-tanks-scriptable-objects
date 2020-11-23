using System.IO;
using MachinationsUP.Config;
using MachinationsUP.Engines.Unity.Editor.Graphics;
using UnityEditor;
using UnityEngine;
using MachinationsUP.Logger;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class MachiCP : EditorWindow
    {

        private string _APIURL = "wss://api.machinations.io/socket.io/?EIO=4&transport=websocket";
        private string _userKey = "<<ENTER YOUR USER KEY>>";
        private string _gameName = "Brave New Game";
        private string _diagramToken = "<<ENTER YOUR DIAGRAM TOKEN>>";
        private bool _restoredFromSettings;

        //private int _index = 0;

        #region Menu

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
            System.Diagnostics.Process.Start("http://my.machinations.io");
        }

        #endregion

        /// <summary>
        /// Draws the Machionations Control Panel GUI.
        /// </summary>
        void OnGUI ()
        {
            if (MachinationsConfig.Instance == null && MachinationsConfig.HasSettings) return;
            if (!_restoredFromSettings && MachinationsConfig.HasSettings && MachinationsConfig.Instance != null)
            {
                _restoredFromSettings = true;
                _APIURL = MachinationsConfig.Instance.APIURL;
                _userKey = MachinationsConfig.Instance.UserKey;
                _gameName = MachinationsConfig.Instance.GameName;
                _diagramToken = MachinationsConfig.Instance.DiagramToken;
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.Label("Machinations.io Connection Settings", EditorStyles.boldLabel);
            _APIURL = EditorGUILayout.TextField("API URL", _APIURL);
            _userKey = EditorGUILayout.TextField("User Key", _userKey);
            _gameName = EditorGUILayout.TextField("Game Name", _gameName);
            _diagramToken = EditorGUILayout.TextField("Diagram Token", _diagramToken);

            /* FUTURE FUNCTIONALITY - CODE GENERATION.
            EditorGUILayout.Space();
            EditorGUILayout.Separator();
            EditorGUILayout.Space();
            _index = EditorGUILayout.Popup(_index, new [] {"Player Tank", "Player Tank Shell", "Enemy Tank", "Enemy Tank Shell"});
            if (GUILayout.Button("Create"))
                CreateMachinationsCode();
            */

            if (GUI.changed)
            {
                SaveMachinationsConfig();
            }
        }

        /*
        /// <summary>
        /// Responsible for triggering Machinations Code Generation.
        /// </summary>
        void CreateMachinationsCode ()
        {
            switch (_index)
            {
                case 0:
                    string template = File.ReadAllText(Path.Combine(Application.dataPath, "MachinationsTemplates", "Template.cst"));
                    template = template.Replace("<<ClassName>>", "NewObject");
                    File.WriteAllText(Path.Combine(Application.dataPath, "MachinationsOut", "NewObject.cs"), template);
                    break;
                case 1:
                    break;
            }
        }
        */

        void OnEnable ()
        {
            SaveMachinationsConfig();
        }

        void OnDestroy ()
        {
            SaveMachinationsConfig();
        }

        /// <summary>
        /// Saves any setting changed.
        /// </summary>
        private void SaveMachinationsConfig ()
        {
            //Only saving settings after they have been restored.
            if (!_restoredFromSettings && MachinationsConfig.HasSettings) return;
            MachinationsConfig.Instance = new MachinationsConfig();
            MachinationsConfig.Instance.APIURL = _APIURL;
            MachinationsConfig.Instance.UserKey = _userKey;
            MachinationsConfig.Instance.GameName = _gameName;
            MachinationsConfig.Instance.DiagramToken = _diagramToken;
            MachinationsConfig.SaveSettings();
        }

    }
}