using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    public interface ITagProvider
    {

        string Name { get; set; }
        bool Checked { get; set; }
        Texture Aspect { get; set; }

    }
}