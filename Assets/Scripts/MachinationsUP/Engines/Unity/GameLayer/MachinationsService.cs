using System.Collections.Generic;
using MachinationsUP.Engines.Unity.BackendConnection;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.SyncAPI;
using UnityEngine;

namespace MachinationsUP.Engines.Unity.GameComms
{
    public class MachinationsBasicService : IMachinationsService
    {

        private SocketIOClient _socketClient;

        public void UseSocket (SocketIOClient socketClient)
        {
            _socketClient = socketClient;
        }
        
        public void ScheduleSync (IMachiDiagram diagram)
        {
            //TODO: review this.
            Debug.Log("Sync requested by " + diagram.DiagramName + " for " + diagram.ScriptableObjects.Count + " objects.");
            _socketClient.EmitMachinationsInitRequest();
        }
        
        public JSONObject GetInitRequestData (string diagramToken)
        {
            //Init Request components will be stored as top level items in this Dictionary.
            var initRequest = new Dictionary<string, JSONObject>();
            bool noItems = true; //TRUE: nothing to get Init Request for.
            
            //TODO go through all scenes.
            var scene = MachiGlobalLayer.GetScenes()[0];
            
            //Create individual JSON Objects for each Machination element to retrieve.
            //This is an Array because this is what the JSON Object Library expects.
            JSONObject[] keys = new JSONObject [scene.SourceElements.Keys.Count];
            int i = 0;
            foreach (DiagramMapping diagramMapping in scene.SourceElements.Keys)
            {
                if (scene.SourceElements[diagramMapping] != null)
                {
                    Debug.Log("Skipping requsting info for already initialized Source Element: " + scene.SourceElements[diagramMapping]);
                    continue;
                }
                noItems = false;
                
                var item = new Dictionary<string, JSONObject>();
                item.Add("id", new JSONObject(diagramMapping.DiagramElementID));
                //Create JSON Objects for all props that we have to retrieve.
                string[] sprops =
                {
                    SyncMsgs.JP_DIAGRAM_LABEL, SyncMsgs.JP_DIAGRAM_ACTIVATION, SyncMsgs.JP_DIAGRAM_ACTION,
                    SyncMsgs.JP_DIAGRAM_RESOURCES, SyncMsgs.JP_DIAGRAM_CAPACITY, SyncMsgs.JP_DIAGRAM_OVERFLOW
                };
                List<JSONObject> props = new List<JSONObject>();
                foreach (string sprop in sprops)
                    props.Add(JSONObject.CreateStringObject(sprop));
                //Add props field.
                item.Add("props", new JSONObject(props.ToArray()));

                keys[i++] = new JSONObject(item);
            }

            if (noItems) return null;
            
            //Finalize request by adding all top level items.
            initRequest.Add(SyncMsgs.JK_AUTH_DIAGRAM_TOKEN, JSONObject.CreateStringObject(diagramToken));
            //Wrapping the keys Array inside a JSON Object.
            initRequest.Add(SyncMsgs.JK_INIT_MACHINATIONS_IDS, new JSONObject(keys));

            return new JSONObject(initRequest);
        }

        public void FailedToConnect ()
        {
            foreach (IMachiDiagram machiScene in MachiGlobalLayer.GetScenes())
                machiScene.SyncFail();
        }

        public void InitComplete ()
        {
            foreach (IMachiDiagram machiScene in MachiGlobalLayer.GetScenes())
                machiScene.SyncComplete();
        }

        public void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false)
        {
            //TODO: optimize. Not all scenes may be interested.
            foreach (IMachiDiagram machiScene in MachiGlobalLayer.GetScenes())
                machiScene.UpdateWithValuesFromMachinations(elementsFromBackEnd, updateFromDiagram);
        }
        
    }
}