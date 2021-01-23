using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    /// <summary>
    /// The contract for a class to represent itself within the <see cref="PopupSearchList"/> tags.  
    /// </summary>
    public interface ITagProvider
    {

        /// <summary>
        /// Name of the tag. Displayed on hover.
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// If the tag has been checked or not.
        /// </summary>
        bool Checked { get; set; }
        
        /// <summary>
        /// Image to represent the tag with.
        /// </summary>
        Texture Aspect { get; set; }

    }
}