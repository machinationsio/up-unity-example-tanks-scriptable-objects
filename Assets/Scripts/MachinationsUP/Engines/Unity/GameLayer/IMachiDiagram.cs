using System.Collections.Generic;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    
    public interface IMachiDiagram
    {

        string DiagramName { get; }
        
        IMachinationsService MachinationsService { get; set; }

        /// <summary>
        /// List with of all registered MachinationsGameObject.
        /// </summary>
        List<MachinationsGameObject> GameObjects { get; }

        /// <summary>
        /// List with of all registered MachinationsGameAwareObject.
        /// </summary>
        List<MachinationsGameAwareObject> GameAwareObjects { get; }
        
        Dictionary<IMachinationsScriptableObject, EnrolledScriptableObject> ScriptableObjects { get; }
        
        Dictionary<DiagramMapping, ElementBase> SourceElements { get; }

        void SyncComplete ();

        void SyncFail ();

        void UpdateWithValuesFromMachinations (List<JSONObject> elementsFromBackEnd, bool updateFromDiagram = false);

    }
}