using System;
using System.Linq;
using System.Reflection;
using Entia.Core;
using Entia.Injectables;
using Entia.Modules;
using Entia.Modules.Component;
using Entia.Queriers;
using Entia.Queryables;
using FsCheck;

namespace Entia.Test
{
    public sealed class Query : Action<World, Model>
    {
        readonly Type[] _types;
        IQuerier[] _failures;

        public Query(params Type[] types) { _types = types; }

        public override void Do(World value, Model model)
        {
            _failures = _types.Select(value.Queriers().Get).OfType<Queriers.False>().ToArray();
        }

        public override Property Check(World value, Model model) => (_failures.Length == 0).Label("defaults.Length == 0");
    }
}