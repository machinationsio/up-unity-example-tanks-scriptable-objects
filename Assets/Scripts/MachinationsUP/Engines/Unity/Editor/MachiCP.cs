using System;
using System.Timers;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.SyncAPI;
using SocketIO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class MachiCP : EditorWindow
    {

        private string _APIURL = "ws://127.0.0.1:5000/socket.io/?EIO=4&transport=websocket";
        private string _userKey = "4fc8b3a7-3909-43b6-96a0-0387cd85e896";
        private string _gameName = "Tanks";
        private string _diagramToken = "54dc6ccd-1f66-41d8-a8d5-0873e935a770";

        static private IMachinationsService _machinationsService;
        private SocketIOClient _socketClient;

        static private MachiCP _instance;
        
        public MachiCP ()
        {
            if (_instance != null)
            {
                throw new Exception("Multiple MachiCP not supported yet.");
            }
            _instance = this;
        }

        void OnEnable ()
        {
            EditorApplication.update += Update;
            _machinationsService.UseSocket(_socketClient = new SocketIOClient(_machinationsService, _APIURL, _userKey, _gameName, _diagramToken));
        }

        void OnDisable ()
        {
            EditorApplication.update -= Update;
        }

        [InitializeOnLoadMethod]
        static private void Konstruct ()
        {
            EditorApplication.update += UpdateStatic;
            _machinationsService = MachiGlobalLayer.MachinationsService = new MachinationsBasicService();
            Debug.Log("MachiCP.Konstruct: Set MachinationsService with Hash: " + MachiGlobalLayer.MachinationsService.GetHashCode());
        }

        [MenuItem("Tools/Machinations/Open Machinations.io Control Panel")]
        static public void ShowWindow ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
        }
        
        [MenuItem("Tools/Machinations/Pause Sync")]
        static public void PauseSync ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
        }
        
        [MenuItem("Tools/Machinations/Launch Machinations.io")]
        static public void Launch ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
        }

        void OnGUI ()
        {
            GUILayout.Label("Machinations.io Connection Settings", EditorStyles.boldLabel);
            _APIURL = EditorGUILayout.TextField("API URL", _APIURL);
            _userKey = EditorGUILayout.TextField("User Key", _userKey);
            _gameName = EditorGUILayout.TextField("Game Name", _gameName);
            _diagramToken = EditorGUILayout.TextField("Diagram Token", _diagramToken);
        }

        public void Update ()
        {
            _socketClient?.ExecuteThread();
            //            Debug.Log("update");
            //throw new NotImplementedException();
        }

        static private void UpdateStatic ()
        {
            if (_instance) _instance.Update();
        }

        private void OnDestroy ()
        {
            _socketClient.PrepareClose();
        }

    }
}