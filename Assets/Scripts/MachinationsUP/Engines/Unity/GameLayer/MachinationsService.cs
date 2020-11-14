using System;
using System.Collections.Generic;
using System.Timers;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Logger;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.GameComms
{
    public enum State
    {

        Idling,
        InitRequested,
        InitComplete,
        SyncRequested,
        GetFromMachinations

    }

    public class MachinationsService
    {

        private SocketIOClient _socketClient;

        private Timer _scheduler;

        private State _currentState;

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

        public void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            MachinationsDataLayer.UpdateSourcesWithValuesFromMachinations(elementsFromBackEnd, updateFromDiagram);
        }

        /// <summary>
        /// Emits the 'Game Init Request' Socket event.
        /// </summary>
        public void EmitGameUpdateDiagramElementsRequest (JSONObject updateRequest)
        {
            _socketClient.EmitGameUpdateDiagramElementsRequest(updateRequest);
        }

        public void PauseSync ()
        {
            throw new System.NotImplementedException();
        }

    }
}