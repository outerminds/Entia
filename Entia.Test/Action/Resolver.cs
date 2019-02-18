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
    public sealed class Resolve : Action<World, Model>
    {
        int _random;
        int _increment;
        int _set;
        bool _resolve;
        bool _resolved;

        public override bool Pre(World value, Model model)
        {
            _random = model.Random.Next(1, 100);
            _resolve = model.Random.NextDouble() < 0.5;
            return true;
        }

        public override void Do(World value, Model model)
        {
            for (int i = 0; i < _random; i++) value.Resolvers().Defer(this, @this => @this._increment++);
            value.Resolvers().Defer((random: _random, @this: this), data => data.@this._set = data.random);
            _resolved = _resolve && value.Resolvers().Resolve();
        }

        public override Property Check(World value, Model model) =>
            (_resolve == _resolved).Label("resolve == resolved")
            .And((_resolve == (_increment == _random)).Label("increment == random"))
            .And((_resolve == (_set == _random)).Label("set == random"));
    }
}