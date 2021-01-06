using UnityEngine;

namespace MachinationsUP.Engines.Unity.Editor
{
    public class SearchListItem
    {

        public int ID { get; set; }
        public string Name { get; set; }
        public Texture Aspect { get; set; }
        public bool Checked { get; set; }
        public ITagProvider TagProvider;
        public object AttachedObject { get; set; }

    }
}