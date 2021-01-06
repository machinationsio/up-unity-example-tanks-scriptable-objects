using System;
using System.Runtime.Serialization;
using MachinationsUP.GameEngineAPI.States;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Elements;
using MachinationsUP.SyncAPI;

namespace MachinationsUP.Integration.Inventory
{
    /// <summary>
    /// Summarizes information about how a Machinations Object Property is mapped to a Diagram.
    /// Since a single <see cref="StatesAssociation"/> is supported, these are the precise coordinates of a Machinations Object Property.
    /// See <see cref="MachinationsUP.Integration.Binder"/>
    /// </summary>
    [DataContract(Name = "MachinationsDiagramMapping", Namespace = "http://www.machinations.io")]
    public class DiagramMapping
    {

        private string _name;

        /// <summary>
        /// The name of the Machinations Game Object that has this property.
        /// See <see cref="MachinationsUP.Integration.GameObject.MachinationsGameObject"/>
        /// </summary>
        [DataMember()]
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        private string _propertyName;

        /// <summary>
        /// The name of this Property.
        /// </summary>
        [DataMember()]
        public string PropertyName
        {
            get => _propertyName;
            set => _propertyName = value;
        }

        private StatesAssociation _statesAssociation;

        /// <summary>
        /// The States Association where this Property applies.
        /// </summary>
        [DataMember()]
        public StatesAssociation StatesAssoc
        {
            get => _statesAssociation;
            set => _statesAssociation = value;
        }

        private int _diagramElementID;

        /// <summary>
        /// Machinations Back-end Element ID.
        /// </summary>
        [DataMember()]
        public int DiagramElementID
        {
            get => _diagramElementID;
            set => _diagramElementID = value;
        }

        /// <summary>
        /// How to handle situations when different values come from the Diagram.
        /// //TODO: Not implemented yet.
        /// </summary>
        public OverwriteRules OvewriteRule { get; set; }

        /// <summary>
        /// When this Mapping is used inside a Scriptable Object, this will be the Element Base that is represented in the Editor.
        /// </summary>
        public ElementBase EditorElementBase { get; set; }
        
        /// <summary>
        /// A Default ElementBase, to be used only during OFFLINE mode.
        /// </summary>
        public ElementBase DefaultElementBase { get; set; }

        /// <summary>
        /// The last value received from the Machinations back-end.
        /// </summary>
        [DataMember()]
        public ElementBase CachedElementBase { get; set; }

        /// <summary>
        /// An ElementBase that will override the one in the Diagram.
        /// </summary>
        public ElementBase OverrideElementBase { get; set; }

        /// <summary>
        /// Verifies if this DiagramMapping matches the provided criteria.
        /// </summary>
        /// <param name="name">Game Object name to match.</param>
        /// /// <param name="propertyName">Game Object Property name to match.</param>
        /// <param name="statesAssociation">States Association to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (string name, string propertyName, StatesAssociation statesAssociation,
            bool stringifyStatesAssociation)
        {
            return Name == name && PropertyName == propertyName &&
                   (
                       (stringifyStatesAssociation &&
                        (
                            (StatesAssoc == null && statesAssociation == null) ||
                            (StatesAssoc != null && statesAssociation != null && StatesAssoc.ToString() == statesAssociation.ToString())))
                       ||
                       (!stringifyStatesAssociation && StatesAssoc == statesAssociation)
                   );
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches the Game Object-specific properties of an ElementBinder and
        /// any given <see cref="StatesAssociation"/>.
        /// </summary>
        /// <param name="elementBinder">Element Binder to verify.</param>
        /// <param name="statesAssociation">States Association to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (ElementBinder elementBinder, StatesAssociation statesAssociation, bool stringifyStatesAssociation)
        {
            return Matches(
                elementBinder.ParentGameObject != null
                    ? elementBinder.ParentGameObject.Name
                    : elementBinder.ParentScriptableObject?.Manifest.Name
                , elementBinder.GameObjectPropertyName, statesAssociation,
                stringifyStatesAssociation);
        }

        /// <summary>
        /// Verifies if this DiagramMapping matches another DiagramMapping.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping to verify.</param>
        /// <param name="stringifyStatesAssociation">In the case of cached States Association, string values will be used for comparison.</param>
        public bool Matches (DiagramMapping diagramMapping, bool stringifyStatesAssociation)
        {
            return Matches(diagramMapping.Name, diagramMapping.PropertyName, diagramMapping.StatesAssoc,
                stringifyStatesAssociation);
        }

        override public string ToString ()
        {
            return "DiagramMapping for " + Name + "." + PropertyName + "." +
                   (_statesAssociation != null ? _statesAssociation.Title : "N/A") +
                   " bound to DiagramID: " + DiagramElementID;
        }

    }
}