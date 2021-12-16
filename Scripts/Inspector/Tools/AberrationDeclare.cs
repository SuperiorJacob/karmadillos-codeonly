using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.EditorTools
{
    public enum DeclarationTypes
    {
        Basic = 0,
        Debug = 1,
    }

    /// <summary>
    /// Declare this class will be using the aberration editor instead of the default. You can also declare an icon.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class AberrationDeclareAttribute : PropertyAttribute
    {
        public DeclarationTypes declaration;
        public string icon;

        public AberrationDeclareAttribute(DeclarationTypes a_declaration = DeclarationTypes.Basic, string a_icon = "")
        {
            declaration = a_declaration;
            icon = a_icon;
        }
    }


    /// <summary>
    /// A simplified way of setting object tooltips and showing debug summaries.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public class AberrationDescriptionAttribute : PropertyAttribute
    {
        public readonly string description;
        public readonly string author;
        public readonly string lastEdit;
        public readonly string id;

        public AberrationDescriptionAttribute(string a_description, string a_author = "", string a_lastEdit = "", string a_id = "")
        {
            description = a_description;
            author = a_author;
            lastEdit = a_lastEdit;
            id = a_id;
        }
    }

    /// <summary>
    /// Force a property to be required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class AberrationRequiredAttribute : PropertyAttribute
    {
        public readonly System.Type type;

        public AberrationRequiredAttribute(System.Type a_type = null)
        {
            type = a_type;
        }
    }
}