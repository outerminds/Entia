using Entia.Components;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Component;
using Entia.Modules.Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Modules
{
    public sealed partial class Components
    {
        /// <summary>
        /// Gets a default component of type <typeref name="T"/>.
        /// If the component type has a <c>static</c> field, property or method tagged with the <see cref="Entia.Core.DefaultAttribute"/> attribute, this member will be used to instantiate the component.
        /// </summary>
        /// <returns>The default component.</returns>
        [ThreadSafe]
        public T Default<T>() where T : struct, IComponent => DefaultUtility.Default<T>();

        /// <summary>
        /// Gets a default component of provided <paramref name="type"/>.
        /// If the component type has a <c>static</c> field, property or method tagged with the <see cref="Entia.Core.DefaultAttribute"/> attribute, this member will be used to instantiate the component.
        /// </summary>
        /// <param name="type">The concrete component type.</param>
        /// <param name="component">The default component.</param>
        /// <returns>Returns <c>true</c> if a valid component was created; otherwise, <c>false</c>.</returns>
        [ThreadSafe]
        public bool TryDefault(Type type, out IComponent component)
        {
            if (ComponentUtility.IsConcrete(type) && DefaultUtility.Default(type) is IComponent casted)
            {
                component = casted;
                return true;
            }

            component = default;
            return false;
        }
    }
}