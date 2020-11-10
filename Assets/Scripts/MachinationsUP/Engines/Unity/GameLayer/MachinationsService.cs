using System.Collections.Generic;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.SyncAPI;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.GameComms
{
    public class MachinationsService
    {

        private SocketIOClient _socketClient;

        public void UseSocket (SocketIOClient socketClient)
        {
            _socketClient = socketClient;
        }

        public void ScheduleSync ()
        {
            //TODO: review this.
            Debug.Log("Sync requested.");
            _socketClient.EmitMachinationsInitRequest();
        }

        public void FailedToConnect ()
        {
            MachinationsDataLayer.SyncFail();
        }

        public void InitComplete ()
        {
            MachinationsDataLayer.SyncComplete();
        }

        public void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            MachinationsDataLayer.UpdateWithValuesFromMachinations(elementsFromBackEnd, updateFromDiagram);
        }

        public void PauseSync ()
        {
            throw new System.NotImplementedException();
        }

    }
}