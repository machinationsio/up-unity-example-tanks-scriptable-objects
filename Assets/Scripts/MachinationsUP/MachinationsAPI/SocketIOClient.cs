using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.SyncAPI;
using MachinationsUP.Integration.GameObject;
using SocketIO;
using MachinationsUP.Logger;

namespace MachinationsUP.Engines.Unity.BackendConnection
{
    /// <summary>
    /// Wraps SocketIOGlobal, adding Machinations-specific handling.
    /// </summary>
    public class SocketIOClient
    {

        #region Configuration Constants/Fields

        private const int RECONNECTION_ATTEMPTS = 100;
        private int _reconnectionAttempts;

        #endregion

        /// <summary>
        /// Socket used for communication.
        /// </summary>
        private SocketIOGlobal _socket;

        /// <summary>
        /// Service that owns this Socket.
        /// </summary>
        private MnService _mnService;

        /// <summary>
        /// Where to connect for the Machinations API.
        /// </summary>
        readonly private string _socketURL;

        /// <summary>
        /// The User Key under which to make all API calls. This can be retrieved from
        /// the Machinations product.
        /// </summary>
        readonly private string _userKey;

        //TODO: Diagram Token system will soon be upgraded to support multiple diagrams.
        /// <summary>
        /// The Machinations Diagram Token will be used to identify ONE Diagram that this game is connected to.
        /// </summary>
        readonly private string _diagramToken;

        /// <summary>
        /// Game name is be used for associating a game with multiple diagrams.
        /// [TENTATIVE UPCOMING SUPPORT]
        /// </summary>
        readonly private string _gameName;

        /// <summary>
        /// Connection to Machinations Backend has been aborted.
        /// </summary>
        private bool _connectionAborted;

        /// <summary>
        /// Makes sure that the Socket executes its thread.
        /// Originally, the SocketIO Component was a Game Object and it did this on Update. 
        /// </summary>
        private System.Timers.Timer _socketPulse;

        /// <summary>
        /// Prepares and Connects the Socket to the Machinations Backend.
        /// </summary>
        /// <param name="mnService">MachinationsService to interact with for data transactions.</param>
        /// <param name="socketURL">The URL where the Machinations API resides.</param>
        /// <param name="userKey">User Key (API key) to use when connecting to the back-end.</param>
        /// <param name="diagramToken">Diagram Token to make requests to.</param>
        /// <param name="gameName">OPTIONAL. Game name.</param>
        public SocketIOClient (MnService mnService, string socketURL, string userKey, string diagramToken,
            string gameName = null)
        {
            _socketURL = socketURL;
            _userKey = userKey;
            _gameName = gameName;
            _diagramToken = diagramToken;
            _mnService = mnService;

            _socketPulse = new System.Timers.Timer(1000);
            _socketPulse.Elapsed += SocketPulse_OnElapsed;
            _socketPulse.Start();

            InitSocket();
        }

        /// <summary>
        /// Initializes the Socket IO component.
        /// </summary>
        public void InitSocket ()
        {
            if (_socket != null)
            {
                _socket.PrepareClose();
                _socket.Close();
            }
            _socket = new SocketIOGlobal();
            _socket.autoConnect = false;
            L.D("Instantiated SocketIO with Hash: " + _socket.GetHashCode() + " for URL: " + _socketURL);
            _socket.url = _socketURL;
            SocketIOComponent.MaxRetryCountForConnect = 1;

            //Setup socket.
            _socket.SetUserKey(_userKey);
            try
            {
                _socket.CreateSocket();
            }
            catch (Exception ex)
            {
                L.Ex(ex);
            }

            _socket.On(SyncMsgs.RECEIVE_AUTH_SUCCESS, OnAuthSuccess);
            _socket.On(SyncMsgs.RECEIVE_AUTH_DENY, OnAuthDeny);
            _socket.On(SyncMsgs.RECEIVE_OPEN, OnSocketOpen);
            _socket.On(SyncMsgs.RECEIVE_OPEN_START, OnSocketOpenStart);
            _socket.On(SyncMsgs.RECEIVE_GAME_INIT, OnGameInitResponse);
            _socket.On(SyncMsgs.RECEIVE_DIAGRAM_ELEMENTS_UPDATED, OnDiagramElementsUpdated);
            _socket.On(SyncMsgs.RECEIVE_ERROR, OnSocketError);
            _socket.On(SyncMsgs.RECEIVE_API_ERROR, OnSocketError);
            _socket.On(SyncMsgs.RECEIVE_CLOSE, OnSocketClose);
            _socket.On(SyncMsgs.RECEIVE_GAME_UPDATE_DIAGRAM_ELEMENTS, OnGameUpdateDiagramElementsRequest);
            try
            {
                L.D("SocketIO: Connect Start.");
                _socket.Connect();
                L.D("SocketIO: Connect End.");
            }
            catch (Exception ex)
            {
                L.Ex(ex);
            }
        }

        private void SocketPulse_OnElapsed (object sender, ElapsedEventArgs e)
        {
            ExecuteThread();
        }

        private void ExecuteThread ()
        {
            _socket.ExecuteThread();
            //Reconnect on failure.
            if (_connectionAborted && _reconnectionAttempts <= RECONNECTION_ATTEMPTS)
            {
                L.E("SocketIO: Connection Aborted!");
                _reconnectionAttempts++;
                _connectionAborted = false;
                //Must close the Socket before we try to connect again.
                try
                {
                    _socket.ClearHandlers();
                    _socket.PrepareClose();
                    _socket.Close();
                }
                catch (Exception e)
                {
                    L.Ex(e);
                }
                _socket = null;
                L.D("SocketIO: Attempt #" + _reconnectionAttempts + " to reconnect to Machinations.");
                ConnectToMachinations(3);
            }
        }

        private void ConnectToMachinations (int waitSeconds = 0)
        {
            L.D("SocketIO: Connecting in " + waitSeconds + " seconds.");
            Thread.Sleep(waitSeconds * 1000);
            InitSocket();
        }

        #region Socket IO - Communication with Machinations Back-end

        /// <summary>
        /// Emits the 'Game Auth Request' Socket event.
        /// </summary>
        public void EmitAuthRequest ()
        {
            if (_socket == null)
            {
                throw new Exception("Cannot Emit Auth Request when Socket has been destroyed.");
            }
            var authRequest = new Dictionary<string, string>
            {
                {SyncMsgs.JK_AUTH_GAME_NAME, _gameName},
                {SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, _diagramToken}
            };

            L.D("SocketIO: EmitMachinationsAuthRequest with gameName " + _gameName + " and diagram token " + _diagramToken);

            _socket.Emit(SyncMsgs.SEND_API_AUTHORIZE, new JSONObject(authRequest));
        }

        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnAuthSuccess (SocketIOEvent e)
        {
            L.D("SocketIO: Game Auth Request Result: " + e.data);
            _mnService.AuthSuccess();
            //Initialization complete.
            IsAuthenticated = true;
        }
        
        private void OnAuthDeny (SocketIOEvent e)
        {
            L.D("SocketIO: Game Auth Request Failure: " + e.data);
            HandleConnectionFailure(false);
        }
        
        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// <param name="selectiveGet">TRUE: get only elements that we don't already have.</param>
        /// </summary>
        public void EmitDiagramInitRequest (bool selectiveGet)
        {
            if (!IsInitialized)
                throw new Exception("SocketIO: Socket not open!");
            L.D("SocketIO: EmitMachinationsInitRequest.");

            var initRequestData = MnDataLayer.GetInitRequestData(_diagramToken, selectiveGet);
            //If there's nothing to request, quit.
            if (!initRequestData)
            {
                L.D("SocketIO: EmitMachinationsInitRequest: no data has been requested.");
                _mnService.InitComplete(null);
                return;
            }

            //TODO: better tracking of server responses. 
            _socket.Emit(SyncMsgs.SEND_GAME_INIT, initRequestData);
        }

        /// <summary>
        /// The Machinations Back-end has answered.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnGameInitResponse (SocketIOEvent e)
        {
            L.D("SocketIO: OnGameInitResponse DATA: " + e.data);

            try
            {
                //The answer from the back-end may contain multiple payloads.
                foreach (string payloadKey in e.data.keys)
                    //For now, only interested in the "SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST" payload.
                    if (payloadKey == SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST)
                        _mnService.InitComplete(e.data[SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST].list);
            }
            catch (Exception ex)
            {
                L.Ex(ex);
            }
        }

        /// <summary>
        /// Emits the 'Game Update Diagram Elements' Socket event.
        /// </summary>
        public void EmitGameUpdateDiagramElementsRequest (JSONObject updateRequest)
        {
            _socket.Emit(SyncMsgs.SEND_GAME_UPDATE_DIAGRAM_ELEMENTS, updateRequest);
        }
        
        /// <summary>
        /// Occurs when the game has received an update from Machinations because some Diagram elements were changed.
        /// </summary>
        /// <param name="e">Contains Init Data.</param>
        private void OnDiagramElementsUpdated (SocketIOEvent e)
        {
            //The answer from the back-end may contain multiple payloads.
            foreach (string payloadKey in e.data.keys)
                //For now, only interested in the "SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST" payload.
                if (payloadKey == SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST)
                    //TODO: switch to local instance.
                    MnDataLayer.Service.UpdateWithValuesFromMachinations(e.data[SyncMsgs.JK_DIAGRAM_ELEMENTS_LIST].list, true);
        }

        private void OnGameUpdateDiagramElementsRequest (SocketIOEvent e)
        {
            L.D("SocketIO: Game Update Diagram Elements Response: " + e.data);
        }
        
        /// <summary>
        /// Handles event emission for a MachinationsGameObject.
        /// </summary>
        /// <param name="mgo">MachinationsGameObject that emitted the event.</param>
        /// <param name="evnt">The event that was emitted.</param>
        public void EmitGameEvent (MnGameObject mgo, string evnt)
        {
            var sync = new Dictionary<string, string>
            {
                {SyncMsgs.JK_EVENT_GAME_OBJ_NAME, mgo.Name},
                {SyncMsgs.JK_EVENT_GAME_EVENT, evnt}
            };
            L.D("SocketIO: EmitGameEvent " + evnt);

            _socket.Emit(SyncMsgs.SEND_GAME_EVENT, new JSONObject(sync));
        }
        
        #region Socket IO Core Events

        private void OnSocketOpen (SocketIOEvent e)
        {
            SocketOpenReceived = true;
            L.D("[SocketIO] Open received: " + e.name + " " + e.data);
        }

        private void OnSocketOpenStart (SocketIOEvent e)
        {
            SocketOpenStartReceived = true;
            L.D("[SocketIO] Open Start received: " + e.name + " " + e.data);
        }

        private void OnSocketError (SocketIOEvent e)
        {
            L.D("[SocketIO::" + _socket.GetHashCode() + "] !!!! Error received: " + e.name + " DATA: " + e.data + " ");
            HandleConnectionFailure(true);
        }

        private void OnSocketClose (SocketIOEvent e)
        {
            L.D("[SocketIO::" + _socket.GetHashCode() + "] !!!! Close received: " + e.name + " DATA:" + e.data);
            HandleConnectionFailure(true);
        }
        
        #endregion

        /// <summary>
        /// Called on Socket Errors.
        /// </summary>
        /// <param name="calledFromThread">TRUE: was called from a Thread, skip loading Cache here.
        /// The Thread will have to handle that.</param>
        private void HandleConnectionFailure (bool calledFromThread)
        {
            L.E("SocketIO: Failed to connect!");
            _connectionAborted = true;
            _mnService.FailedToConnect(false);
        }

        #endregion

        /// <summary>
        /// TRUE when the Socket is open.
        /// </summary>
        private bool SocketOpenReceived { set; get; }

        /// <summary>
        /// TRUE when the Socket is open.
        /// </summary>
        private bool SocketOpenStartReceived { set; get; }

        /// <summary>
        /// TRUE when all Init-related tasks have been completed.
        /// </summary>
        public bool IsAuthenticated { set; get; }

        /// <summary>
        /// TRUE when all Init-related tasks have been completed.
        /// </summary>
        public bool IsInitialized => _socket != null && _socket.IsConnected && SocketOpenReceived && SocketOpenStartReceived;

    }
}