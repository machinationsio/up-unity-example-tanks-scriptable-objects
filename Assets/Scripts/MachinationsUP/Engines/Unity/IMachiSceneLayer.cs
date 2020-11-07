using System.Collections.Generic;
using MachinationsUP.Engines.Unity.GameComms;
using MachinationsUP.Integration.GameObject;

namespace MachinationsUP.Engines.Unity
{
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

    }
}