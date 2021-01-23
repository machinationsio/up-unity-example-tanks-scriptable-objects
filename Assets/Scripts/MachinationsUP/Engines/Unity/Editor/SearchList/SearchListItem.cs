using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    /// <summary>
    /// This is an item to be listed & searched after within <see cref="PopupSearchList"/>.
    /// </summary>
    public class SearchListItem
    {

        /// <summary>
        /// Unique ID.
        /// </summary>
        public int ID { get; set; }
        
        /// <summary>
        /// Display name.
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Optional image.
        /// </summary>
        public Texture Aspect { get; set; }
        
        /// <summary>
        /// Is this Item selected?
        /// </summary>
        public bool Checked { get; set; }
        
        /// <summary>
        /// The ITagProvider associated with this Item (if any, can be filtered via tag).
        /// </summary>
        public ITagProvider TagProvider;
        
        /// <summary>
        /// Optional attached data.
        /// </summary>
        public object AttachedObject { get; set; }

    }
}