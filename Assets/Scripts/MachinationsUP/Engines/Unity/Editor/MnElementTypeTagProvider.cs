using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    /// <summary>
    /// Provides Machinations-based ITagProvider functionality, required for the <see cref="PopupSearchList"/>.
    /// </summary>
    public class MnElementTypeTagProvider : ITagProvider
    {

        public string Name { get; set; }
        public bool Checked { get; set; }
        public Texture Aspect { get; set; }

    }
}