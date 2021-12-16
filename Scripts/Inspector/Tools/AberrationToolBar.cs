using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AberrationGames.EditorTools
{
    /// <summary>
    /// Create a tool bar by name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class AberrationToolBarAttribute : PropertyAttribute
    {
        public string name;

        public AberrationToolBarAttribute(string a_name)
        {
            this.name = a_name;
        }
    }

    /// <summary>
    /// Close the toolbar (this is not needed if there are no further variables).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class AberrationEndToolBarAttribute : PropertyAttribute
    {
        public AberrationEndToolBarAttribute()
        { }
    }
}