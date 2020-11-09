using System.Collections.Generic;
using MachinationsUP.Engines.Unity.BackendConnection;

namespace MachinationsUP.Engines.Unity.GameComms
{
    public interface IMachinationsService
    {

        void UseSocket (SocketIOClient socket);
        
        void ScheduleSync (IMachiSceneLayer sceneLayer);

        JSONObject GetInitRequestData (string diagramToken);

        void FailedToConnect ();
        
        void InitComplete ();
        
        void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false);

    }
}