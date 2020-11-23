using System;
using System.Collections.Generic;
using System.Timers;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Logger;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.GameComms
{
    /// <summary>
    /// Service States. Will soon be a part of a State Machine implemented here.
    /// </summary>
    public enum State
    {

        Idling,
        InitRequested,
        InitComplete,
        SyncRequested,
        GetFromMachinations

    }

    /// <summary>
    /// Supervises communication with Machinations.
    /// </summary>
    public class MachinationsService
    {

        /// <summary>
        /// The current way of communicating with the Machinations back-end.
        /// </summary>
        private SocketIOClient _socketClient;

        /// <summary>
        /// Timer that handles service State changes.
        /// </summary>
        readonly private Timer _scheduler;

        /// <summary>
        /// Current State of the Service.
        /// </summary>
        private State _currentState;

        /// <summary>
        /// Diagram elements received from the Back-end. Sent from SocketIOClient.
        /// </summary>
        private List<JSONObject> _diagramElementsFromBackEnd;

        public bool IsGameRunning { get; set; }

        public MachinationsService ()
        {
            L.D("Instantiated MachinationsService with Hash: " + GetHashCode());
            _scheduler = new Timer(1000);
            _scheduler.Elapsed += Scheduler_OnElapsed;
            _scheduler.Start();
            _currentState = State.Idling;
        }

        private void Scheduler_OnElapsed (object sender, ElapsedEventArgs e)
        {
            //When the game is running, ProcessSchedule will be called by the MachinationsMainThreadHook.
            if (IsGameRunning) return;
            ProcessSchedule();
        }

        /// <summary>
        /// Handles Machinations tasks.
        /// </summary>
        public void ProcessSchedule ()
        {
            if (!_socketClient.IsInitialized || !_socketClient.IsAuthenticated)
            {
                L.D("--- Waiting for Socket Connection ---");
                return;
            }
            
            switch (_currentState)
            {
                case State.InitComplete:
                    _currentState = State.Idling;
                    MachinationsDataLayer.UpdateSourcesWithValuesFromMachinations(_diagramElementsFromBackEnd);
                    MachinationsDataLayer.SyncComplete();
                    break;
                //Wait at least 1 Timer interval before making the Sync request.
                case State.SyncRequested:
                    _currentState = State.GetFromMachinations;
                    break;
                case State.GetFromMachinations:
                    _currentState = State.Idling;
                    _socketClient.EmitMachinationsInitRequest();
                    break;
            }
        }

        /// <summary>
        /// Sets up the Socket to use for communicating with the Machinations back-end.
        /// Gets called by <see cref="MachinationsUP.Engines.Unity.Startup.MachinationsEntryPoint"/>.
        /// </summary>
        /// <param name="socketClient">SocketIOClient to use.</param>
        public void UseSocket (SocketIOClient socketClient)
        {
            _socketClient = socketClient;
        }

        /// <summary>
        /// Tells the Service to perform a Sync with the Machinations Back-end.
        /// Multiple calls to this function are allowed, as the Service will always wait some time after the last request has gone in.
        /// </summary>
        public void ScheduleSync ()
        {
            L.D("MachinationsService.ScheduleSync.");
            _currentState = State.SyncRequested;
        }

        #region Communication via Socket IO.

        //TODO: to be switched to event-based during next iteration.

        public void FailedToConnect (bool loadCache)
        {
            MachinationsDataLayer.SyncFail(loadCache);
        }

        public void InitComplete (List<JSONObject> diagramElementsFromBackEnd)
        {
            _diagramElementsFromBackEnd = diagramElementsFromBackEnd;
            _currentState = State.InitComplete;
        }

        #endregion

        /// <summary>
        /// Called by the Socket when an answer is received from the Machinations Back-end.
        /// TODO: useless passing of values?
        /// </summary>
        /// <param name="elementsFromBackEnd"></param>
        /// <param name="updateFromDiagram"></param>
        public void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            MachinationsDataLayer.UpdateSourcesWithValuesFromMachinations(elementsFromBackEnd, updateFromDiagram);
        }

        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// </summary>
        public void EmitGameUpdateDiagramElementsRequest (JSONObject updateRequest)
        {
            L.I("EmitGameUpdateDiagramElementsRequest: " + updateRequest);
            _socketClient.EmitGameUpdateDiagramElementsRequest(updateRequest);
        }

        public void PauseSync ()
        {
            throw new System.NotImplementedException();
        }

    }
}