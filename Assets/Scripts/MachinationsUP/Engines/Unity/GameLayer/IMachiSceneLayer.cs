using System.Collections.Generic;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.Integration.Elements;
using MachinationsUP.Integration.GameObject;
using MachinationsUP.Integration.Inventory;

namespace MachinationsUP.Engines.Unity
{
    
    //TODO: this might potentially have to be renamed into Diagram. A Scene shouldn't be a Machinations concept. Machinations deals in Diagrams. A scene may contain MULTIPLE diagrams. 
    //If the developer wants to think in Scenes, the developer is welcomed to create a Scene class specific to their game in which they can do their stuff with
    //the Diagrams they included in their game.
    public interface IMachiSceneLayer
    {

        string SceneName { get; }
        
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