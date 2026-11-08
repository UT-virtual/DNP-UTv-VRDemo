using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DNPDemo.Editor
{
    /// <summary>
    /// Inspector for <see cref="SubclassSelectorAttribute"/> fields: a type dropdown on the header row, the selected
    /// object's fields below, and the object's <c>ToString()</c> as the label so lists read like a timeline.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public sealed class SubclassSelectorDrawer : PropertyDrawer
    {
        const float DropdownWidth = 150f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ManagedReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            object value = property.managedReferenceValue;
            var dropdownRect = new Rect(position.xMax - DropdownWidth, position.y, DropdownWidth, EditorGUIUtility.singleLineHeight);
            string typeLabel = value == null ? "（種類を選択）" : ObjectNames.NicifyVariableName(value.GetType().Name);
            // Drawn before the field so it receives the click before the foldout header does.
            if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(typeLabel), FocusType.Keyboard))
            {
                ShowTypeMenu(property, value, dropdownRect);
            }

            var fieldLabel = new GUIContent(label);
            if (value != null && HasOwnToString(value.GetType()))
            {
                fieldLabel.text = value.ToString();
            }
            EditorGUI.PropertyField(position, property, fieldLabel, true);
        }

        static void ShowTypeMenu(SerializedProperty property, object current, Rect dropdownRect)
        {
            Type baseType = ResolveFieldType(property.managedReferenceFieldTypename);
            if (baseType == null)
            {
                return;
            }
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("（なし）"), current == null, () => Assign(serializedObject, path, null));
            foreach (Type type in TypeCache.GetTypesDerivedFrom(baseType)
                         .Where(IsSelectable)
                         .OrderBy(t => t.Name))
            {
                Type captured = type;
                menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), current?.GetType() == type,
                    () => Assign(serializedObject, path, Activator.CreateInstance(captured)));
            }
            menu.DropDown(dropdownRect);
        }

        static void Assign(SerializedObject serializedObject, string path, object value)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property == null)
            {
                return;
            }
            property.managedReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        static bool IsSelectable(Type type) =>
            !type.IsAbstract
            && !type.IsGenericType
            && type.IsDefined(typeof(SerializableAttribute), false)
            && !typeof(Object).IsAssignableFrom(type)
            && type.GetConstructor(Type.EmptyTypes) != null;

        static bool HasOwnToString(Type type) =>
            type.GetMethod(nameof(ToString), Type.EmptyTypes)?.DeclaringType != typeof(object);

        /// <summary>Parses Unity's "AssemblyName Namespace.Type" field type name (nested types are written "Outer/Inner").</summary>
        static Type ResolveFieldType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }
            int split = typeName.IndexOf(' ');
            if (split <= 0)
            {
                return null;
            }
            string fullName = typeName.Substring(split + 1).Replace('/', '+');
            return Type.GetType($"{fullName}, {typeName.Substring(0, split)}");
        }
    }
}
