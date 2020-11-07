using UnityEngine;

namespace MachinationsUP.Engines.Unity.GameComms
{
    public class MachinationsBasicService : IMachinationsService
    {

        public void ScheduleSync (IMachiSceneLayer sceneLayer)
        {
            Debug.Log("Sync requested by " + sceneLayer.SceneName + " for " + sceneLayer.ScriptableObjects.Count + " objects.");
        }

    }
}