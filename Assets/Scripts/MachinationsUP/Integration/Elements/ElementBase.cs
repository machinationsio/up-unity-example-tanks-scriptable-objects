using System;
using System.Runtime.Serialization;
using MachinationsUP.Engines.Unity;
using MachinationsUP.Integration.Binder;
using MachinationsUP.Integration.Inventory;
using MachinationsUP.Logger;

namespace MachinationsUP.Integration.Elements
{
    /// <summary>
    /// Wraps a Machinations Value.
    /// </summary>
    [DataContract(Name = "MachinationsElementBase", Namespace = "http://www.machinations.io")]
    [KnownType(typeof(FormulaElement))]
    [Serializable]
    public class ElementBase
    {

        /// <summary>
        /// Permits the Unity Property inspector connection. Setting this manually will have no effect over <see cref="CurrentValue"/>.
        /// See <see cref="MachinationsUP.Engines.Unity.EditorExtensions.ElementBaseDrawer"/>
        /// </summary>
        public int _serializableValue;

        /// <summary>
        /// The value received from Machinations.
        /// </summary>
        [DataMember()]
        public int BaseValue { get; set; }

        /// <summary>
        /// The DiagramMapping associated with this ElementBase.
        /// </summary>
        public DiagramMapping DiagMapping { get; private set; }

        /// <summary>
        /// Parent <see cref="ElementBinder"/>. A Binder contains multiple ElementBase, to facilitate Game State Awareness.
        /// </summary>
        internal ElementBinder ParentElementBinder { get; }

        protected int _currentValue;

        /// <summary>
        /// INT value of this ElementBase.
        /// May be overrided in Child Classes.
        /// </summary>
        [DataMember()]
        virtual public int CurrentValue
        {
            get => _currentValue;
            protected set
            {
                _currentValue = value;
                _serializableValue = value;
            }
        }

        /// <summary>
        /// Top cap.
        /// </summary>
        [DataMember()]
        public int? MaxValue { get; set; }

        /// <summary>
        /// Bottom cap. NOT USED YET.
        /// </summary>
        [DataMember()]
        public int MinValue { get; set; }

        //For Serialization only!
        public ElementBase ()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="baseValue">Value to start with.</param>
        /// <param name="mapping">The DiagramMapping associated with this ElementBase.</param>
        /// <param name="parentBinder">Parent Element Binder.</param>
        public ElementBase (int baseValue, DiagramMapping mapping, ElementBinder parentBinder = null)
        {
            DiagMapping = mapping;
            ParentElementBinder = parentBinder;
            BaseValue = baseValue;
            MaxValue = null;
            Reset();
        }

        /// <summary>
        /// Initializes this ElementBase from another one.
        /// </summary>
        /// <param name="source"></param>
        public ElementBase (ElementBase source)
        {
            ParentElementBinder = source.ParentElementBinder;
            BaseValue = source.BaseValue;
            MaxValue = source.MaxValue;
            Reset();
        }

        /// <summary>
        /// When this is constructed by Serialization, the Diagram Mapping needs to be set afterwards.
        /// </summary>
        /// <param name="diagramMapping">Diagram Mapping associated with this ElementBase.</param>
        public void AssignDiagramMapping (DiagramMapping diagramMapping)
        {
            if (DiagMapping != null) throw new Exception("You cannot assign a Diagram Mapping when one has already been assigned!");
            DiagMapping = diagramMapping;
        }

        /// <summary>
        /// Resets the Current Value to the Base Value.
        /// </summary>
        virtual protected void Reset ()
        {
            CurrentValue = BaseValue;
        }

        /// <summary>
        /// Makes sure the value doesn't exceed Min or Max caps.
        /// </summary>
        private void Clamp ()
        {
            if (MaxValue != null && CurrentValue > MaxValue) CurrentValue = (int) MaxValue;
        }

        /// <summary>
        /// Changes the Int Value with the given amount.
        /// TODO: prepare for supporting other types than int.
        /// </summary>
        /// <param name="amount"></param>
        public void ChangeValueWith (int amount)
        {
            CurrentValue += amount;
            Clamp();
        }

        /// <summary>
        /// Changes the Value of the ElementBase to the given one.
        /// IMPORTANT: see override in <see cref="FormulaElement"/>.
        /// </summary>
        /// <param name="value">New Value.</param>
        virtual public void ChangeValueTo (int value)
        {
            CurrentValue = value;
            Clamp();
        }

        /// <summary>
        /// Changes the value of this ElementBase to the given one.
        /// This function will also trigger an update towards the Machinations Back-End.
        /// </summary>
        /// <param name="value">New Value.</param>
        public void ChangeValueFromEditor (int value)
        {
            int previousValue = CurrentValue;
            ChangeValueTo(value);
            MnDataLayer.EmitGameUpdateDiagramElementsRequest(this, previousValue);
        }

        /// <summary>
        /// Returns a duplicate of this Element Base. Required in <see cref="MnDataLayer"/> in CreateElement.
        /// </summary>
        /// <param name="parentBinder">Parent Element Binder.</param>
        /// <returns></returns>
        virtual public ElementBase Clone (ElementBinder parentBinder)
        {
            return new ElementBase(BaseValue, DiagMapping, parentBinder);
        }

        /// <summary>
        /// Ovewrites this ELementBase's data with the provided one.
        /// IMPORTANT: see override in <see cref="FormulaElement"/>.
        /// </summary>
        /// <param name="with">New data.</param>
        virtual public void Overwrite (ElementBase with)
        {
            MinValue = with.MinValue;
            MaxValue = with.MaxValue;
            BaseValue = with.BaseValue;
            ChangeValueTo(with.CurrentValue);
        }

        override public string ToString ()
        {
            return "ElementBase: CurrentValue: " + CurrentValue + " BaseValue: " + BaseValue + " MinValue: " + MinValue + " MaxValue: " + MaxValue;
        }

    }
}