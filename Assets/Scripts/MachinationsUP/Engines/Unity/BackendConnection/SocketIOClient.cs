using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Xml;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.GameEngineAPI.Game;
using MachinationsUP.GameEngineAPI.GameObject;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.SyncAPI;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;
using SocketIO;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.BackendConnection
{
    public class SocketIOClient
    {
        
        #region Configuration Constants/Fields

        private const int RECONNECTION_ATTEMPTS = 3;
        private static int _reconnectionAttempts = 0;

        #endregion

        static private SocketIOGlobal _socket;

        private IMachinationsService _machinationsService;
        
        /// <summary>
        /// The User Key under which to make all API calls. This can be retrieved from
        /// the Machinations product.
        /// </summary>
        readonly private string _userKey;

        /// <summary>
        /// Game name is be used for associating a game with multiple diagrams.
        /// [TENTATIVE UPCOMING SUPPORT]
        /// </summary>
        readonly private string _gameName;

        /// <summary>
        /// The Machinations Diagram Token will be used to identify ONE Diagram that this game is connected to.
        /// </summary>
        readonly private string _diagramToken;

        /// <summary>
        /// Number of responses that are pending from the Socket.
        /// </summary>
        private int _pendingResponses;

        /// <summary>
        /// Connection to Machinations Backend has been aborted.
        /// </summary>
        static private bool _connectionAborted;

        public SocketIOClient (IMachinationsService machinationsService, string userKey, string gameName, string diagramToken)
        {
            _userKey = userKey;
            _gameName = gameName;
            _diagramToken = diagramToken;
            _machinationsService = machinationsService;
            InitSocket();
        }

        /// <summary>
        /// Initializes the Socket IO component.
        /// </summary>
        public void InitSocket ()
        {
            IsConnecting = true;

            _socket = new SocketIOGlobal();
            SocketIOComponent.MaxRetryCountForConnect = 1;
            //Socket must be kept throughout the game.
            //Setup socket.
            _socket.SetUserKey(_userKey);
            _socket.CreateSocket();
            _socket.On(SyncMsgs.RECEIVE_OPEN, OnSocketOpen);
            _socket.On(SyncMsgs.RECEIVE_OPEN_START, OnSocketOpenStart);
            _socket.On(SyncMsgs.RECEIVE_AUTH_SUCCESS, OnAuthSuccess);
            _socket.On(SyncMsgs.RECEIVE_AUTH_DENY, OnAuthDeny);
            _socket.On(SyncMsgs.RECEIVE_GAME_INIT, OnGameInitResponse);
            _socket.On(SyncMsgs.RECEIVE_DIAGRAM_ELEMENTS_UPDATED, OnDiagramElementsUpdated);
            _socket.On(SyncMsgs.RECEIVE_ERROR, OnSocketError);
            _socket.On(SyncMsgs.RECEIVE_API_ERROR, OnSocketError);
            _socket.On(SyncMsgs.RECEIVE_CLOSE, OnSocketClose);

            _socket.On(SyncMsgs.RECEIVE_GAME_UPDATE_DIAGRAM_ELEMENTS, OnGameUpdateDiagramElementsRequest);
            _socket.Connect();
        }
        
        public void ExecuteThread ()
        {
            _socket.ExecuteThread();
            
            if (_connectionAborted && SocketClosed && _reconnectionAttempts <= RECONNECTION_ATTEMPTS)
            {
                _reconnectionAttempts++;
                _connectionAborted = false;
                SocketClosed = false;
                Debug.Log("Attempt to reconnect to Machinations.");
                ConnectToMachinations();
            }
        }

        private void ConnectToMachinations (int waitSeconds = 0)
        {
            Debug.Log("Connecting in " + waitSeconds + " seconds.");
            Thread.Sleep(waitSeconds * 1000);
            InitSocket();
            /*
            //yield return new WaitUntil(() => _connectionAborted || (_socket.IsConnected && SocketOpenReceived && SocketOpenStartReceived));

            if (_connectionAborted)
            {
                Debug.LogError("MGL Connection failure. Game will proceed with default/cached values!");

                //Cache system active? Load Cache.
                if (!string.IsNullOrEmpty(cacheDirectoryName)) LoadCache();
                //Running in offline mode now.
                IsInOfflineMode = true;
                OnMachinationsUpdate?.Invoke(this, null);
            }
            else
            {
                IsConnecting = false;
                Debug.Log("MGL.Start: Connection achieved.");

                EmitMachinationsAuthRequest();

                yield return new WaitUntil(() => IsAuthenticated || IsInOfflineMode);

                EmitMachinationsInitRequest();

                yield return new WaitUntil(() => IsInitialized || IsInOfflineMode);

                Debug.Log("MGL.Start: Machinations Backend Sync complete. Resuming game.");
            }

            //Notify Game Engine of Machinations Init Complete.
            Instance._gameLifecycleProvider?.MachinationsInitComplete();
            */
        }

        public void PrepareClose ()
        {
            _socket.PrepareClose();
        }
        
        #region Socket IO - Communication with Machinations Back-end

        /// <summary>
        /// Handles event emission for a MachinationsGameObject.
        /// </summary>
        /// <param name="mgo">MachinationsGameObject that emitted the event.</param>
        /// <param name="evnt">The event that was emitted.</param>
        public void EmitGameEvent (MachinationsGameObject mgo, string evnt)
        {
            var sync = new Dictionary<string, string>
            {
                {SyncMsgs.JK_EVENT_GAME_OBJ_NAME, mgo.GameObjectName},
                {SyncMsgs.JK_EVENT_GAME_EVENT, evnt}
            };
            Debug.Log("MGL.EmitGameEvent " + evnt);
            _socket.Emit(SyncMsgs.SEND_GAME_EVENT, new JSONObject(sync));
        }

        /// <summary>
        /// Emits the 'Game Auth Request' Socket event.
        /// </summary>
        private void EmitMachinationsAuthRequest ()
        {
            _pendingResponses++;
            var authRequest = new Dictionary<string, string>
            {
                {SyncMsgs.JK_AUTH_GAME_NAME, _gameName},
                {SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, _diagramToken}
            };

            Debug.Log("MGL.EmitMachinationsAuthRequest with gameName " + _gameName + " and diagram token " + _diagramToken);

            _socket.Emit(SyncMsgs.SEND_API_AUTHORIZE, new JSONObject(authRequest));
        }

        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// </summary>
        public void EmitMachinationsInitRequest ()
        {
            if (!IsInitialized)
                throw new Exception("Socket not open!");
            Debug.Log("SocketIO.EmitMachinationsInitRequest.");

            var initRequestData = _machinationsService.GetInitRequestData(_diagramToken);
            //If there's nothing to request, quit.
            if (!initRequestData)
            {
                Debug.Log("not emitting anything");
                _machinationsService.InitComplete();
                return;
            }
            
            //TODO: better tracking of server responses. 
            _pendingResponses++;
            _socket.Emit(SyncMsgs.SEND_GAME_INIT, initRequestData);
        }

        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// </summary>
        public void EmitGameUpdateDiagramElementsRequest (ElementBase sourceElement, int previousValue)
        {
            //Update Request components will be stored as top level items in this Dictionary.
            var updateRequest = new Dictionary<string, JSONObject>();

            //The item will be a Dictionary comprising of "id" and "props". The "props" will contain the properties to update.
            var item = new Dictionary<string, JSONObject>();
            item.Add("id", new JSONObject(sourceElement.ParentElementBinder.DiagMapping.DiagramElementID));
            item.Add("type", JSONObject.CreateStringObject("resources"));
            item.Add("timeStamp", new JSONObject(DateTime.Now.Ticks));
            item.Add("parameter", JSONObject.CreateStringObject("number"));
            item.Add("previous", new JSONObject(previousValue));
            item.Add("value", new JSONObject(sourceElement.CurrentValue));

            JSONObject[] keys = new JSONObject [1];
            keys[0] = new JSONObject(item);

            //Finalize request by adding all top level items.
            updateRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(_diagramToken));
            //Wrapping the keys Array inside a JSON Object.
            updateRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));

            Debug.Log("MGL.EmitMachinationsUpdateElementsRequest.");

            _socket.Emit(SyncMsgs.SEND_GAME_UPDATE_DIAGRAM_ELEMENTS, new JSONObject(updateRequest));
        }

        private void OnSocketOpen (SocketIOEvent e)
        {
            SocketOpenReceived = true;
            Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
        }

        private void OnSocketOpenStart (SocketIOEvent e)
        {
            SocketOpenStartReceived = true;
            Debug.Log("[SocketIO] Open Start received: " + e.name + " " + e.data);
        }

        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnGameInitResponse (SocketIOEvent e)
        {
            Debug.Log("OnGameInitResponse DATA: " + e.data);

            //The answer from the back-end may contain multiple payloads.
            foreach (string payloadKey in e.data.keys)
                //For now, only interested in the "SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST" payload.
                if (payloadKey == SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST)
                    _machinationsService.UpdateWithValuesFromMachinations(e.data[SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST].list);
            
            //Decrease number of responses pending from the back-end.
            _pendingResponses--;
            //When reaching 0, initialization is considered complete.
            if (_pendingResponses == 0)
                IsInitialized = true;
        }

        /// <summary>
        /// Occurs when the game has received an update from Machinations because some Diagram elements were changed.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnDiagramElementsUpdated (SocketIOEvent e)
        {
            /*
            //The answer from the back-end may contain multiple payloads.
            foreach (string payloadKey in e.data.keys)
                //For now, only interested in the "SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST" payload.
                if (payloadKey == SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST)
                    UpdateWithValuesFromMachinations(e.data[SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST].list, true);\
            */
        }

        private void OnGameUpdateDiagramElementsRequest (SocketIOEvent e)
        {
            Debug.Log("Game Update Diagram Elements Response: " + e.data);
        }

        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnAuthSuccess (SocketIOEvent e)
        {
            Debug.Log("Game Auth Request Result: " + e.data);
            _pendingResponses--;
            //Initialization complete.
            if (_pendingResponses == 0)
                IsAuthenticated = true;
            EmitMachinationsInitRequest();
        }

        private void OnAuthDeny (SocketIOEvent e)
        {
            Debug.Log("Game Auth Request Failure: " + e.data);
            FailedToConnect(false);
        }

        private void OnSocketError (SocketIOEvent e)
        {
            Debug.Log("[SocketIO] !!!! Error received: " + e.name + " DATA: " + e.data + " ");
            FailedToConnect(true);
        }

        private void OnSocketClose (SocketIOEvent e)
        {
            Debug.Log("[SocketIO] !!!! Close received: " + e.name + " DATA:" + e.data);
            if (IsConnecting)
            {
                Debug.Log("[SocketIO] !!!! But was Connecting, so this is normal.");
                return;
            }

            SocketClosed = true;
            _connectionAborted = true;
        }
        
        /// <summary>
        /// Called on Socket Errors.
        /// </summary>
        /// <param name="calledFromThread">TRUE: was called from a Thread, skip loading Cache here.
        /// The Thread will have to handle that.</param>
        private void FailedToConnect (bool calledFromThread)
        {
            Debug.LogError("Failed to connect!");
            if (_pendingResponses > 0) _pendingResponses--;
            _connectionAborted = true;
            _socket.autoConnect = false;

            //Loading Cache cannot be done from a thread.
            if (!calledFromThread) _machinationsService.FailedToConnect();
        }

        #endregion

        /// <summary>
        /// TRUE when the Socket is open.
        /// </summary>
        static private bool SocketOpenReceived { set; get; }

        /// <summary>
        /// TRUE when the Socket is open.
        /// </summary>
        static private bool SocketOpenStartReceived { set; get; }

        /// <summary>
        /// TRUE when the Socket has been closed.
        /// </summary>
        static private bool SocketClosed { set; get; }

        /// <summary>
        /// TRUE when the Socket is connecting.
        /// </summary>
        static private bool IsConnecting { get; set; }
        
        /// <summary>
        /// TRUE when all Init-related tasks have been completed.
        /// </summary>
        static private bool IsAuthenticated { set; get; }
        
        private bool isInitialized;

        /// <summary>
        /// TRUE when all Init-related tasks have been completed.
        /// </summary>
        private bool IsInitialized
        {
            set
            {
                isInitialized = value;
                if (value)
                {
                    Debug.Log("MachinationsGameLayer Initialization Complete!");
                    _machinationsService.InitComplete();
                }
            }
            get => _socket.IsConnected && SocketOpenReceived && SocketOpenStartReceived;
        }
        
    }
}


/*


        private IEnumerator ConnectToMachinations (int waitSeconds = 0)
        {
            Debug.Log("Connecting in " + waitSeconds + " seconds.");
            yield return new WaitForSeconds(waitSeconds);

            //Notify Game Engine of Machinations Init Start.
            Instance._gameLifecycleProvider?.MachinationsInitStart();

            //Attempt to init Socket.
            _connectionAborted = InitSocket() == false;

            yield return new WaitUntil(() => _connectionAborted || (_socket.IsConnected && SocketOpenReceived && SocketOpenStartReceived));

            if (_connectionAborted)
            {
                Debug.LogError("MGL Connection failure. Game will proceed with default/cached values!");

                //Cache system active? Load Cache.
                if (!string.IsNullOrEmpty(cacheDirectoryName)) LoadCache();
                //Running in offline mode now.
                IsInOfflineMode = true;
                OnMachinationsUpdate?.Invoke(this, null);
            }
            else
            {
                IsConnecting = false;
                Debug.Log("MGL.Start: Connection achieved.");

                EmitMachinationsAuthRequest();

                yield return new WaitUntil(() => IsAuthenticated || IsInOfflineMode);

                EmitMachinationsInitRequest();

                yield return new WaitUntil(() => IsInitialized || IsInOfflineMode);

                Debug.Log("MGL.Start: Machinations Backend Sync complete. Resuming game.");
            }

            //Notify Game Engine of Machinations Init Complete.
            Instance._gameLifecycleProvider?.MachinationsInitComplete();

            yield return 1;
        }
        
        */