// -----------------------------------------------------------------------
// <copyright file="Extension.cs" company="Todd Aspeotis">
//  Copyright 2012, Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Extension
    {
        public Extension()
            : this(null, null, null)
        {
        }

        public Extension(string name)
            : this(name, null, null)
        {
        }

        public Extension(string name, object instance)
            : this(name, instance, null)
        {
        }

        public Extension(string name, object instance, Type type)
        {
            Name = name;
            Instance = instance;
            Type = type;
        }

        public string Name { get; set; }

        public object Instance { get; set; }

        public Type Type { get; set; }

        public Type GetExtensionType()
        {
            if (Type != null)
                return Type;

            if (Instance != null)
                return Instance.GetType();

            throw new InvalidOperationException("Both Type and Instance cannot be null.");
        }
    }
}
