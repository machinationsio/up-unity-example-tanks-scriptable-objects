using System.Collections.Generic;
using MachinationsUP.Engines.Unity.GameComms;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity
{
    static public class MachiGlobalLayer
    {

        static readonly private List<IMachiSceneLayer> _machiScenes = new List<IMachiSceneLayer>();

        static public IMachinationsService MachinationsService;

        static public void AddScene (IMachiSceneLayer scene)
        {
            _machiScenes.Clear();
            if (!_machiScenes.Contains(scene))
            {
                _machiScenes.Add(scene);
                Debug.Log("Scene " + scene.SceneName + " gets MachinationsService with Hash: " + MachinationsService.GetHashCode());
                scene.MachinationsService = MachinationsService;
            }
        }

        static public List<IMachiSceneLayer> GetScenes ()
        {
            return _machiScenes;
        }

    }
}