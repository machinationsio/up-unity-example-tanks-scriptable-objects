using System.Collections.Generic;
using MachinationsUP.Engines.Unity.GameComms;
using UnityEditor;
using UnityEngine;

namespace MachinationsUP.Engines.Unity
{
    static public class MachiGlobalLayer
    {

        static readonly private List<IMachiDiagram> _machiScenes = new List<IMachiDiagram>();

        static public IMachinationsService MachinationsService;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        static public void AddScene (IMachiDiagram scene)
        {
            _machiScenes.Clear();
            if (!_machiScenes.Contains(scene))
            {
                _machiScenes.Add(scene);
                Debug.Log("Scene " + scene.DiagramName + " gets MachinationsService with Hash: " + MachinationsService.GetHashCode());
                scene.MachinationsService = MachinationsService;
            }
        }

        static public List<IMachiDiagram> GetScenes ()
        {
            return _machiScenes;
        }

    }
}