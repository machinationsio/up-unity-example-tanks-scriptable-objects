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

        private string userKey = "ABC";
        private string gameName = "DEF";
        private string diagramToken = "GHI";

        static private IMachinationsService _machinationsService;
        private SocketIOClient _socketClient;

        static private MachiCP _instance;
        
        public MachiCP ()
        {
            if (_instance != null)
            {
                throw new Exception("Multiple MachiCP not supported yet");
            }
            _instance = this;
        }

        void OnEnable ()
        {
            EditorApplication.update += Update;
            _machinationsService.UseSocket(_socketClient = new SocketIOClient(_machinationsService, userKey, gameName, diagramToken));
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

        [MenuItem("Tools/Machinations/Show Window")]
        static public void ShowWindow ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
        }
        
        void OnGUI ()
        {
            GUILayout.Label("Machinations.io Connection Settings", EditorStyles.boldLabel);
            userKey = EditorGUILayout.TextField("Text Field", userKey);
            gameName = EditorGUILayout.TextField("Text Field", gameName);
            diagramToken = EditorGUILayout.TextField("Text Field", diagramToken);
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