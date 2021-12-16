using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.EditorTools
{
    public enum DeclaredButtonTypes
    {
        Button = 0,
        Hidden = 1,
    }

    /// <summary>
    /// Creates a drop down hidden area.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class AberrationButtonAttribute : PropertyAttribute
    {
        public DeclaredButtonTypes declaredType;
        public bool enabled;
        public string name;
        public string reflect;
        public object[] parse;

        public AberrationButtonAttribute(DeclaredButtonTypes a_declaration = DeclaredButtonTypes.Hidden, bool a_enabled = false, 
            string a_name = "", string a_reflect = "", params object[] a_parse)
        {
            enabled = a_enabled;
            name = a_name;
            declaredType = a_declaration;
            reflect = a_reflect;
            parse = a_parse;
        }
    }

    /// <summary>
    /// Removes the drop down hidden area.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class AberrationFinishButtonAttribute : PropertyAttribute
    {
        public AberrationFinishButtonAttribute()
        { }
    }
}
