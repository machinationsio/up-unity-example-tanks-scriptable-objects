using System;
using System.Timers;
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

        string myString = "Hello World";
        bool groupEnabled;
        bool myBool = true;
        float myFloat = 1.23f;

        static private Timer _gameQuery = new Timer(1000);

        static private SocketIOGlobal _socket;

        void OnEnable ()
        {
            EditorApplication.update += Update;
        }

        void OnDisable ()
        {
            EditorApplication.update -= Update;
        }

        [InitializeOnLoadMethod]
        static private void Konstruct ()
        {
            EditorApplication.update += Update;
            MachiGlobalLayer.MachinationsService = new MachinationsBasicService();
            Debug.Log("MachiCP.Konstruct: Set MachinationsService to " + MachiGlobalLayer.MachinationsService.GetHashCode());
            
            _socket = new SocketIOGlobal();
            _socket.CreateSocket();
            _socket.On(SyncMsgs.RECEIVE_OPEN, OnSocketOpen);
            _socket.On(SyncMsgs.RECEIVE_OPEN_START, OnSocketOpenStart);
            _socket.On(SyncMsgs.RECEIVE_AUTH_SUCCESS, OnAuthSuccess);
            _socket.On(SyncMsgs.RECEIVE_ERROR, OnSocketError);
            _socket.On(SyncMsgs.RECEIVE_CLOSE, OnSocketClose);
            _socket.Connect();

            //_gameQuery.Start();
            //_gameQuery.Elapsed += GameQueryOnElapsed;
        }

        [MenuItem("Tools/Machinations/Show Window")]
        static public void ShowWindow ()
        {
            GetWindow(typeof(MachiCP), false, "Machinations.io");
            _socket.Emit("game-event", JSONObject.CreateStringObject("caca"));
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
            _socket?.UpdateCallback();
            //            Debug.Log("update");
            //throw new NotImplementedException();
        }

        static private void OnSocketOpen (SocketIOEvent e)
        {
            Debug.Log("[SocketIO @@@@@@@@@@@] Open received: " + e.name + " " + e.data);
        }

        static private void OnSocketOpenStart (SocketIOEvent e)
        {
            Debug.Log("[SocketIO @@@@@@@@@@@] Open Start received: " + e.name + " " + e.data);
        }


        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        static private void OnAuthSuccess (SocketIOEvent e)
        {
            Debug.Log("SocketIO @@@@@@@@@@@ Game Auth Request Result: " + e.data);
        }

        static private void OnSocketError (SocketIOEvent e)
        {
            Debug.Log("[SocketIO @@@@@@@@@@@] !!!! Error received: " + e.name + " DATA: " + e.data + " ");
        }

        static private void OnSocketClose (SocketIOEvent e)
        {
            Debug.Log("[SocketIO @@@@@@@@@@@] !!!! Close received: " + e.name + " DATA:" + e.data);
        }

        private void OnDestroy ()
        {
            _socket?.PrepareClose();
        }

    }
}