using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class MachinationsElementType : ITagProvider
    {

        public string Name { get; set; }
        public bool Checked { get; set; }
        public Texture Aspect { get; set; }

    }
}