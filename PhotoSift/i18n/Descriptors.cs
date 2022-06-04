using System;
using System.ComponentModel;
using System.Resources;
using static PhotoSift.NGettextShortSyntax;

namespace GlobalizedPropertyGrid
{
    #region GlobalizedPropertyDescriptor

    /// <summary>
    /// GlobalizedPropertyDescriptor enhances the base class bay obtaining the display name for a property
    /// from the resource.
    /// </summary>
    public class GlobalizedPropertyDescriptor : PropertyDescriptor
    {
        private readonly PropertyDescriptor basePropertyDescriptor;

        public GlobalizedPropertyDescriptor(PropertyDescriptor basePropertyDescriptor) : base(basePropertyDescriptor) => this.basePropertyDescriptor = basePropertyDescriptor;

        public override bool CanResetValue(object component) => basePropertyDescriptor.CanResetValue(component);

        public override Type ComponentType => basePropertyDescriptor.ComponentType;

        public override string Category
        {
            get
            {
                string display = this.basePropertyDescriptor.Category;
                string prefix = new string('\u200B', display.Length); // temporary plan
                return prefix + _(display);
            }
        }

        public override string DisplayName => this.basePropertyDescriptor.DisplayName;
        public override string Description => this.basePropertyDescriptor.Description;

        //public override string DisplayName // deprecated code
        //{
        //    get
        //    {
        //        return _(this.basePropertyDescriptor.DisplayName);
        //    }
        //}

        //public override string Description // deprecated code
        //{
        //    get
        //    {
        //        return _(this.basePropertyDescriptor.Description);
        //    }
        //}

        public override object GetValue(object component) => this.basePropertyDescriptor.GetValue(component);

        public override bool IsReadOnly => this.basePropertyDescriptor.IsReadOnly;

        public override string Name => this.basePropertyDescriptor.Name;

        public override Type PropertyType => this.basePropertyDescriptor.PropertyType;

        public override void ResetValue(object component) => this.basePropertyDescriptor.ResetValue(component);

        public override bool ShouldSerializeValue(object component) => this.basePropertyDescriptor.ShouldSerializeValue(component);

        public override void SetValue(object component, object value) => this.basePropertyDescriptor.SetValue(component, value);
    }
    #endregion

    #region GlobalizedObject

    /// <summary>
    /// GlobalizedObject implements ICustomTypeDescriptor to enable 
    /// required functionality to describe a type (class).<br></br>
    /// The main task of this class is to instantiate our own property descriptor 
    /// of type GlobalizedPropertyDescriptor.  
    /// </summary>
    public class GlobalizedObject : ICustomTypeDescriptor
    {
        private PropertyDescriptorCollection globalizedProps;

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        public EventDescriptorCollection GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        /// <summary>
        /// Called to get the properties of a type.
        /// </summary>
        /// <param name="attributes"></param>
        /// <returns></returns>
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            if (globalizedProps == null)
            {
                // Get the collection of properties
                PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this, attributes, true);

                globalizedProps = new PropertyDescriptorCollection(null);

                // For each property use a property descriptor of our own that is able to be globalized
                foreach (PropertyDescriptor oProp in baseProps)
                {
                    globalizedProps.Add(new GlobalizedPropertyDescriptor(oProp));
                }
            }
            return globalizedProps;
        }

        public PropertyDescriptorCollection GetProperties()
        {
            // Only do once
            if (globalizedProps == null)
            {
                // Get the collection of properties
                PropertyDescriptorCollection baseProps = TypeDescriptor.GetProperties(this, true);
                globalizedProps = new PropertyDescriptorCollection(null);

                // For each property use a property descriptor of our own that is able to be globalized
                foreach (PropertyDescriptor oProp in baseProps)
                {
                    globalizedProps.Add(new GlobalizedPropertyDescriptor(oProp));
                }
            }
            return globalizedProps;
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
    }

    #endregion

    //public class LocalizedCategoryAttribute : CategoryAttribute
    //{
    //    private readonly string _base;
    //    public LocalizedCategoryAttribute(string Category) => _base = Category;
    //    public string Category => _(this._base);
    //}
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string baseDescription;
        public LocalizedDescriptionAttribute(string Description) => baseDescription = Description;
        public override string Description => _(this.baseDescription);
    }
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private readonly string baseDisplayName;
        public LocalizedDisplayNameAttribute(string DisplayName) => baseDisplayName = DisplayName;
        public override string DisplayName => _(this.baseDisplayName);
    }

}
