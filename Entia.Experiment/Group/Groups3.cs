using Entia.Modules.Group;
using Entia.Queryables;
using Entia.Core;

namespace Entia.Modules
{
    public sealed class Groups3 : IModule, IResolvable
    {
        readonly Components3 _components;
        readonly Queriers2 _queriers;
        readonly Messages _messages;
        (IGroup3[] items, int count) _groups = (new IGroup3[8], 0);

        public Groups3(Components3 components, Queriers2 queriers, Messages messages)
        {
            _components = components;
            _queriers = queriers;
            _messages = messages;
        }

        public Group3<T> Get3<T>() where T : struct, IQueryable
        {
            var group = new Group3<T>(_components, _queriers, _messages);
            _groups.Push(group);
            return group;
        }

        public void Resolve()
        {
            for (int i = 0; i < _groups.count; i++) _groups.items[i].Resolve();
        }
    }
}