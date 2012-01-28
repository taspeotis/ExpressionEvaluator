// -----------------------------------------------------------------------
// <copyright file="Extension.cs" company="Todd Aspeotis">
//  Copyright 2012 Todd Aspeotis
// </copyright>
// -----------------------------------------------------------------------

namespace ExpressionEvaluator
{
    using System;

    /// <summary>
    /// Represents an object that is accessible to an expression.
    /// </summary>
    public class Extension
    {
        /// <summary>
        /// Initializes a new instance of the Extension class.
        /// </summary>
        public Extension()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Extension class.
        /// </summary>
        /// <param name="name">The name of the extension exposed to the expressions.</param>
        public Extension(string name)
            : this(name, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Extension class.
        /// </summary>
        /// <param name="name">The name of the extension exposed to the expressions.</param>
        /// <param name="instance">The extension instance exposed to the expressions.</param>
        public Extension(string name, object instance)
            : this(name, instance, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Extension class.
        /// </summary>
        /// <param name="name">The name of the extension exposed to the expressions.</param>
        /// <param name="instance">The extension instance exposed to the expressions.</param>
        /// <param name="type">The type of the expression instance exposed to the expressions.</param>
        public Extension(string name, object instance, Type type)
        {
            Name = name;
            Instance = instance;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the name of the extension.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the instance of the extension.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// Gets or sets the type of the expression instance.
        /// </summary>
        /// <remarks>
        /// This value can be used to override the type of the extension exposed to the expressions.
        /// This value is optional if <see cref="Instance"/> is not <see langword="null"/>.
        /// </remarks>
        public Type Type { get; set; }

        /// <summary>
        /// Gets the type of the extension.
        /// </summary>
        /// <returns>
        /// If <see cref="Type"/> is not <see langword="null"/> then <see cref="Type"/> is returned.
        /// If <see cref="Type"/> is <see langword="null"/> and <see cref="Instance"/> is not <see langword="null"/> then the type of <see cref="Instance"/> is returned.
        /// If both <see cref="Type"/> and <see cref="Instance"/> are <see langword="null"/> then an exception is raised.
        /// </returns>
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
