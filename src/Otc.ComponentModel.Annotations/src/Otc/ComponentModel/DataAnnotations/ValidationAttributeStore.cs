// These sources have been forked from https://github.com/dotnet/corefx/releases/tag/v1.1.8
// then customized by Ole Consignado in order to meet it needs.
// Original sources should be found at: https://github.com/dotnet/corefx/tree/v1.1.8/src/System.ComponentModel.Annotations
// Thanks to Microsoft for making it open source!

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Otc.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Cache of <see cref="ValidationAttribute" />s
    /// </summary>
    /// <remarks>
    ///     This internal class serves as a cache of validation attributes and [Display] attributes.
    ///     It exists both to help performance as well as to abstract away the differences between
    ///     Reflection and TypeDescriptor.
    /// </remarks>
    internal class ValidationAttributeStore
    {
        private static readonly ValidationAttributeStore singleton = new ValidationAttributeStore();
        private readonly Dictionary<Type, TypeStoreItem> typeStoreItems = new Dictionary<Type, TypeStoreItem>();

        /// <summary>
        ///     Gets the singleton <see cref="ValidationAttributeStore" />
        /// </summary>
        internal static ValidationAttributeStore Instance
        {
            get { return singleton; }
        }

        /// <summary>
        ///     Retrieves the type level validation attributes for the given type.
        /// </summary>
        /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
        /// <returns>The collection of validation attributes.  It could be empty.</returns>
        internal IEnumerable<ValidationAttribute> GetTypeValidationAttributes(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var item = GetTypeStoreItem(validationContext.ObjectType);
            return item.ValidationAttributes;
        }

        /// <summary>
        ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given type.  It may be null.
        /// </summary>
        /// <param name="validationContext">The context that describes the type.  It cannot be null.</param>
        /// <returns>The display attribute instance, if present.</returns>
        internal DisplayAttribute GetTypeDisplayAttribute(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var item = GetTypeStoreItem(validationContext.ObjectType);
            return item.DisplayAttribute;
        }

        /// <summary>
        ///     Retrieves the set of validation attributes for the property
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The collection of validation attributes.  It could be empty.</returns>
        internal IEnumerable<ValidationAttribute> GetPropertyValidationAttributes(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName, validationContext.ObjectInstance);
            return item.ValidationAttributes;
        }

        internal Dictionary<PropertyInfo, object> GetAllPropertiesInfoAndHisValues(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            typeItem.InitializePropertyStore(validationContext.ObjectInstance);
            return typeItem.GetAllPropertiesInfoAndHisValues();
        }

        /// <summary>
        ///     Retrieves the <see cref="DisplayAttribute" /> associated with the given property
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The display attribute instance, if present.</returns>
        internal DisplayAttribute GetPropertyDisplayAttribute(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName, validationContext.ObjectInstance);
            return item.DisplayAttribute;
        }

        /// <summary>
        ///     Retrieves the Type of the given property.
        /// </summary>
        /// <param name="validationContext">The context that describes the property.  It cannot be null.</param>
        /// <returns>The type of the specified property</returns>
        internal Type GetPropertyType(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            var item = typeItem.GetPropertyStoreItem(validationContext.MemberName, validationContext.ObjectInstance);
            return item.PropertyType;
        }

        /// <summary>
        ///     Determines whether or not a given <see cref="ValidationContext" />'s
        ///     <see cref="ValidationContext.MemberName" /> references a property on
        ///     the <see cref="ValidationContext.ObjectType" />.
        /// </summary>
        /// <param name="validationContext">The <see cref="ValidationContext" /> to check.</param>
        /// <returns><c>true</c> when the <paramref name="validationContext" /> represents a property, <c>false</c> otherwise.</returns>
        internal bool IsPropertyContext(ValidationContext validationContext)
        {
            EnsureValidationContext(validationContext);
            var typeItem = GetTypeStoreItem(validationContext.ObjectType);
            PropertyStoreItem item;
            return typeItem.TryGetPropertyStoreItem(validationContext.MemberName, validationContext.ObjectInstance, out item);
        }

        /// <summary>
        ///     Retrieves or creates the store item for the given type
        /// </summary>
        /// <param name="type">The type whose store item is needed.  It cannot be null</param>
        /// <returns>The type store item.  It will not be null.</returns>
        private TypeStoreItem GetTypeStoreItem(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            lock (typeStoreItems)
            {
                TypeStoreItem item = null;
                if (!typeStoreItems.TryGetValue(type, out item))
                {
                    // use CustomAttributeExtensions.GetCustomAttributes() to get inherited attributes as well as direct ones
                    var attributes = CustomAttributeExtensions.GetCustomAttributes(type.GetTypeInfo(), true);
                    item = new TypeStoreItem(type, attributes);
                    typeStoreItems[type] = item;
                }
                return item;
            }
        }

        /// <summary>
        ///     Throws an ArgumentException of the validation context is null
        /// </summary>
        /// <param name="validationContext">The context to check</param>
        private static void EnsureValidationContext(ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }
        }

        internal static bool IsPublic(PropertyInfo p)
        {
            return (p.GetMethod != null && p.GetMethod.IsPublic) || (p.SetMethod != null && p.SetMethod.IsPublic);
        }

        internal static bool IsStatic(PropertyInfo p)
        {
            return (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic);
        }

        /// <summary>
        ///     Private abstract class for all store items
        /// </summary>
        private abstract class StoreItem
        {
            private readonly IEnumerable<ValidationAttribute> validationAttributes;

            internal StoreItem(IEnumerable<Attribute> attributes)
            {
                validationAttributes = attributes.OfType<ValidationAttribute>();
                DisplayAttribute = attributes.OfType<DisplayAttribute>().SingleOrDefault();
            }

            internal IEnumerable<ValidationAttribute> ValidationAttributes
            {
                get { return validationAttributes; }
            }

            internal DisplayAttribute DisplayAttribute { get; set; }
        }

        /// <summary>
        ///     Private class to store data associated with a type
        /// </summary>
        private class TypeStoreItem : StoreItem
        {
            private readonly object syncRoot = new object();
            private readonly Type type;
            private IList<Tuple<PropertyInfo, object, PropertyStoreItem>> propertyStoreItems;

            internal TypeStoreItem(Type type, IEnumerable<Attribute> attributes)
                : base(attributes)
            {
                this.type = type;
            }

            internal Dictionary<PropertyInfo, object> GetAllPropertiesInfoAndHisValues()
            {
                return propertyStoreItems.ToDictionary(s => s.Item1, x => x.Item2);
            }

            internal void InitializePropertyStore(object instance)
            {
                propertyStoreItems = null;

                ExtractAllPropertiesFromGivenInstance(instance);
            }

            internal PropertyStoreItem GetPropertyStoreItem(string propertyName, object value)
            {
                PropertyStoreItem item = null;
                if (!TryGetPropertyStoreItem(propertyName, value, out item))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture,
                            SR.AttributeStore_Unknown_Property, type.Name, propertyName), nameof(propertyName));
                }
                return item;
            }

            internal void ExtractAllPropertiesFromGivenInstance(object instance)
            {
                if (propertyStoreItems == null)
                {
                    lock (syncRoot)
                    {
                        if (propertyStoreItems == null)
                        {
                            propertyStoreItems = CreatePropertyStoreItems(type, instance);
                        }
                    }
                }
            }

            internal bool TryGetPropertyStoreItem(string propertyName, object instance, out PropertyStoreItem item)
            {
                if (string.IsNullOrEmpty(propertyName))
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }

                ExtractAllPropertiesFromGivenInstance(instance);

                var itemFound = propertyStoreItems.FirstOrDefault(s => s.Item1.Name == propertyName);

                item = itemFound.Item3;

                return itemFound.Item3 != null;
            }

            internal IEnumerable<PropertyInfo> GetProperties(Type type) => type.GetRuntimeProperties()
                    .Where(prop => IsPublic(prop) && !prop.GetIndexParameters().Any());

            //TODO: remover
            //internal bool IsTypePrimitive(PropertyInfo property)
            //{
            //    if (property != null)
            //        return property.PropertyType.GetTypeInfo().IsPrimitive || property.PropertyType == typeof(string);
            //    return false;
            //}

            internal bool IsTypeArray(PropertyInfo property)
            {
                if (property != null)
                {
                    return
                        IsTypeEnumerable(property) ||
                        IsTypeDictionary(property) ||
                        property.PropertyType.IsArray;
                }
                return false;
            }

            internal bool IsTypeDictionary(PropertyInfo property)
            {
                if (property != null)
                {
                    return property.PropertyType.GetTypeInfo().IsGenericType &&
                            property.PropertyType.GetTypeInfo().GetInterfaces().Contains(typeof(IDictionary));
                }

                return false;
            }

            internal bool IsTypeEnumerable(PropertyInfo property)
            {
                if (property != null)
                {
                    return property.PropertyType.GetTypeInfo().IsGenericType &&
                            property.PropertyType.GetTypeInfo().GetInterfaces().Contains(typeof(IEnumerable));
                }
                return false;
            }

            internal IList<Tuple<PropertyInfo, object, PropertyStoreItem>> CreatePropertyStoreItems(Type type, object instance)
            {
                var properties = GetProperties(type);

                var propertyStoreItems = new List<Tuple<PropertyInfo, object, PropertyStoreItem>>();

                foreach (PropertyInfo property in properties)
                {
                    var propertyStoreItem = new PropertyStoreItem(property.PropertyType,
                        CustomAttributeExtensions.GetCustomAttributes(property, true));

                    var propertyValue = GetValueFromProperty(property, instance);

                    propertyStoreItems.Add(
                        new Tuple<PropertyInfo, object, PropertyStoreItem>(
                            property, propertyValue, propertyStoreItem));

                    if (IsTypeArray(property))
                    {
                        if (IsTypeDictionary(property))
                        {
                            //TODO: pending.
                        }
                        if (IsTypeEnumerable(property))
                        {
                            var firstArgument = property.PropertyType.GenericTypeArguments.FirstOrDefault();

                            var iterable = (IEnumerable<object>)propertyValue;

                            foreach (var item in iterable)
                            {
                                var itemsProperty = CreatePropertyStoreItems(item.GetType(), item);

                                foreach (var itemProperty in itemsProperty)
                                {
                                    propertyStoreItems.Add(
                                        new Tuple<PropertyInfo, object, PropertyStoreItem>(
                                            itemProperty.Item1, itemProperty.Item2, itemProperty.Item3));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!property.PropertyType.GetTypeInfo().IsValueType)
                        {
                            var itemsProperty = CreatePropertyStoreItems(property.PropertyType, propertyValue);

                            foreach (var item in itemsProperty)
                            {
                                propertyStoreItems.Add(
                                    new Tuple<PropertyInfo, object, PropertyStoreItem>(
                                        item.Item1, item.Item2, item.Item3));
                            }
                        }
                    }
                }

                return propertyStoreItems;
            }


            internal object GetValueFromProperty(PropertyInfo property, object instance)
            {
                object valor = null;
                try
                {
                    valor = property.GetValue(instance);
                }
                catch (Exception)
                {
                    try
                    {
                        valor = property.GetValue(instance, null);
                    }
                    catch (Exception) { }
                }

                return valor;
            }

        }


        /// <summary>
        ///     Private class to store data associated with a property
        /// </summary>
        private class PropertyStoreItem : StoreItem
        {
            private readonly Type propertyType;

            internal PropertyStoreItem(Type propertyType, IEnumerable<Attribute> attributes)
                : base(attributes)
            {
                this.propertyType = propertyType;
            }

            internal PropertyStoreItem(Type propertyType, Attribute attribute)
                : base(new Attribute[] { attribute })
            {
                this.propertyType = propertyType;
            }

            internal Type PropertyType
            {
                get { return propertyType; }
            }
        }
    }
}
