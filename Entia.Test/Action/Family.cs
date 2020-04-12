using Entia.Core;
using Entia.Messages;
using Entia.Modules;
using Entia.Modules.Family;
using FsCheck;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Entia.Test
{
    public class AdoptEntity : Action<World, Model>
    {
        Entity _parent;
        Entity _child;
        OnAdopt[] _onAdopt;
        bool _success;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            if (entities.Count <= 0) return false;
            _parent = entities.ElementAt(model.Random.Next(entities.Count));
            _child = entities.ElementAt(model.Random.Next(entities.Count));
            return true;
        }
        public override void Do(World value, Model model)
        {
            var families = value.Families();
            var messages = value.Messages();
            using var onAdopt = messages.Receive<OnAdopt>();
            _success = families.Adopt(_parent, _child);
            _onAdopt = onAdopt.Messages().ToArray();
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool tests, string label)> Tests()
            {
                var entities = value.Entities();
                var families = value.Families();

                if (_success)
                {
                    yield return (families.Parent(_child) == _parent, "Parent(child) == parent");
                    yield return (families.Ancestors(_child).Contains(_parent), "Ancestors(child).Contains(parent)");
                    yield return (families.Ancestors(_child).FirstOrDefault() == _parent, "Ancestors(child).First() == parent");
                    yield return (families.Has(_parent, _child), "Has(parent, child)");
                    yield return (families.Children(_parent).Contains(_child), "Children(parent).Contains(child)");
                    yield return (families.Descendants(_parent, From.Top).Contains(_child), "Descendants(parent, Top).Contains(child)");
                    yield return (families.Descendants(_parent, From.Bottom).Contains(_child), "Descendants(parent, Bottom).Contains(child)");
                    yield return (families.Family(_parent, From.Top).Contains(_child), "Family(parent, Top).Contains(child)");
                    yield return (families.Family(_parent, From.Bottom).Contains(_child), "Family(parent, Bottom).Contains(child)");
                    yield return (_onAdopt.Length == 1, "onAdopt.Length == 1");
                    yield return (_onAdopt.All(message => message.Child == _child), "onAdopt.All(message.Child == child)");
                    yield return (_onAdopt.All(message => message.Parent == _parent), "onAdopt.All(message.Parent == parent)");
                }
                else
                {
                    yield return (families.Parent(_child) != _parent, "Parent(child) != parent");
                    yield return (families.Ancestors(_child).Contains(_parent).Not(), "Ancestors(child).Contains(parent).Not()");
                    yield return (families.Descendants(_child, From.Top).Contains(_parent).Not(), "Descendants(child).Contains(parent).Not()");
                    yield return (families.Descendants(_parent, From.Top).Contains(_child).Not(), "Descendants(parent).Contains(child).Not()");
                }

                yield return (families.Adopt(_parent, _child) == _success, "Adopt(parent, child) == success");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_parent}, {_child}, {_success})";
    }

    public class RejectEntity : Action<World, Model>
    {
        Entity _child;
        Entity _parent;
        OnReject[] _onReject;
        bool _success;

        public override bool Pre(World value, Model model)
        {
            var entities = value.Entities();
            var families = value.Families();
            if (entities.Count <= 0) return false;
            _child = entities.ElementAt(model.Random.Next(entities.Count));
            _parent = families.Parent(_child);
            return true;
        }
        public override void Do(World value, Model model)
        {
            var families = value.Families();
            var messages = value.Messages();
            using var onReject = messages.Receive<OnReject>();
            _success = families.Reject(_child);
            _onReject = onReject.Messages().ToArray();
        }
        public override Property Check(World value, Model model)
        {
            return PropertyUtility.All(Tests());

            IEnumerable<(bool tests, string label)> Tests()
            {
                var entities = value.Entities();
                var families = value.Families();

                if (_success)
                {
                    yield return (families.Parent(_child) == Entity.Zero, "Parent(child) == Entity.Zero");
                    yield return (families.Parent(_child) != _parent, "Parent(child) == parent");
                    yield return (families.Ancestors(_child).Contains(_parent).Not(), "Ancestors(child).Contains(parent).Not()");
                    yield return (families.Has(_parent, _child).Not(), "Has(parent, child).Not()");
                    yield return (families.Children(_parent).Contains(_child).Not(), "Children(parent).Contains(child).Not()");
                    yield return (families.Descendants(_parent, From.Top).Contains(_child).Not(), "Descendants(parent, Top).Contains(child).Not()");
                    yield return (families.Descendants(_parent, From.Bottom).Contains(_child).Not(), "Descendants(parent, Bottom).Contains(child).Not()");
                    yield return (families.Family(_parent, From.Top).Contains(_child).Not(), "Family(parent, Top).Contains(child).Not()");
                    yield return (families.Family(_parent, From.Bottom).Contains(_child).Not(), "Family(parent, Bottom).Contains(child).Not()");
                    yield return (families.Roots().Contains(_child), "Roots().Contains(child)");
                    yield return (_onReject.Length == 1, "onAdopt.Length == 1");
                    yield return (_onReject.All(message => message.Child == _child), "onAdopt.All(message.Child == child)");
                    yield return (_onReject.All(message => message.Parent == _parent), "onAdopt.All(message.Parent == parent)");
                }
                else
                {
                    yield return (families.Parent(_child) == _parent, "Parent(child) == parent");
                }

                yield return (families.Reject(_child).Not(), "Reject(child).Not()");
            }
        }
        public override string ToString() => $"{GetType().Format()}({_parent}, {_child}, {_success})";
    }
}
