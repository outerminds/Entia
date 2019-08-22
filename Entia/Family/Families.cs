using System;
using System.Collections.Generic;
using Entia.Core;
using Entia.Core.Documentation;
using Entia.Messages;
using Entia.Modules.Family;
using Entia.Modules.Message;

namespace Entia.Modules
{
    namespace Family
    {
        public enum From : byte { Top, Bottom }
    }

    public sealed class Families : IModule
    {
        struct Relationships
        {
            public static readonly Relationships Empty = new Relationships { Children = (Array.Empty<Entity>(), 0) };

            public Entity Entity;
            public Entity Parent;
            public (Entity[] items, int count) Children;
        }

        readonly Entities _entities;
        readonly Emitter<OnAdopt> _onAdopt;
        readonly Emitter<OnReject> _onReject;
        Relationships[] _relationships;

        public Families(Messages messages, Entities entities) : this(messages, entities, new Relationships[entities.Count])
        {
            foreach (var entity in entities) Initialize(entity);
        }

        Families(Messages messages, Entities entities, params Relationships[] relationships)
        {
            _entities = entities;
            _relationships = relationships;
            _onAdopt = messages.Emitter<OnAdopt>();
            _onReject = messages.Emitter<OnReject>();
            messages.React((in OnCreate message) => Initialize(message.Entity));
            messages.React((in OnPostDestroy message) => Dispose(message.Entity));
        }

        [ThreadSafe]
        public Entity Root(Entity entity)
        {
            ref var relationships = ref GetRelationships(entity, out var success);
            if (success && relationships.Parent) return Root(relationships.Parent);
            return entity;
        }

        [ThreadSafe]
        public IEnumerable<Entity> Roots()
        {
            foreach (var relationships in _relationships)
                if (relationships.Entity && relationships.Parent == Entity.Zero) yield return relationships.Entity;
        }

        [ThreadSafe]
        public Entity Parent(Entity child)
        {
            ref var relationships = ref GetRelationships(child, out var success);
            return success ? relationships.Parent : default;
        }

        [ThreadSafe]
        public Slice<Entity>.Read Children(Entity parent)
        {
            ref var relationships = ref GetRelationships(parent, out var success);
            return success ? relationships.Children.Slice() : Dummy<Entity>.Array.Zero;
        }

        [ThreadSafe]
        public IEnumerable<Entity> Ancestors(Entity child)
        {
            var parent = child;
            while (TryGetRelationships(parent, out var relationships) && relationships.Parent)
                yield return parent = relationships.Parent;
        }

        [ThreadSafe]
        public IEnumerable<Entity> Descendants(Entity parent, From from = From.Top)
        {
            if (TryGetRelationships(parent, out var relationships))
            {
                for (int i = 0; i < relationships.Children.count; i++)
                {
                    var child = relationships.Children.items[i];
                    if (from == From.Top) yield return child;
                    foreach (var grandchild in Descendants(child, from)) yield return grandchild;
                    if (from == From.Bottom) yield return child;
                }
            }
        }

        [ThreadSafe]
        public IEnumerable<Entity> Siblings(Entity child)
        {
            if (TryGetRelationships(child, out var relationships))
            {
                return relationships.Parent ?
                    Children(relationships.Parent).Except(child) :
                    Roots().Except(child);
            }
            return Array.Empty<Entity>();
        }

        [ThreadSafe]
        public IEnumerable<Entity> Family(Entity entity, From from = From.Top)
        {
            var root = Root(entity);
            yield return root;
            foreach (var descendant in Descendants(root, from)) yield return descendant;
        }

        [ThreadSafe]
        public bool Has(Entity parent, Entity child)
        {
            ref var relationships = ref GetRelationships(child, out var success);
            return success && Has(parent, ref relationships);
        }

        public bool AdoptAt(int index, Entity parent, Entity child)
        {
            ref var relationships = ref GetRelationships(parent, out var success);
            return success && AdoptAt(index, ref relationships, child);
        }

        public bool AdoptAt(int index, Entity parent, params Entity[] children)
        {
            var adopted = false;
            ref var relationships = ref GetRelationships(parent, out var success);
            if (success) for (int i = 0; i < children.Length; i++) adopted |= AdoptAt(index + i, parent, children[i]);
            return adopted;
        }

        public bool Adopt(Entity parent, Entity child)
        {
            ref var relationships = ref GetRelationships(parent, out var success);
            return success && Adopt(ref relationships, child);
        }

        public bool Adopt(Entity parent, params Entity[] children)
        {
            var adopted = false;
            ref var relationships = ref GetRelationships(parent, out var success);
            if (success) for (int i = 0; i < children.Length; i++) adopted |= Adopt(ref relationships, children[i]);
            return adopted;
        }

        public bool RejectAt(int index, Entity parent)
        {
            ref var relationships = ref GetRelationships(parent, out var success);
            return success && RejectAt(index, ref relationships);
        }

        public bool RejectAt(int index, int count, Entity parent)
        {
            var rejected = false;
            ref var relationships = ref GetRelationships(parent, out var success);
            if (success) for (int i = 0; i < count; i++) rejected |= RejectAt(index, ref relationships);
            return rejected;
        }

        public bool Reject(Entity child)
        {
            ref var relationships = ref GetRelationships(child, out var success);
            return success && Reject(ref relationships);
        }

        public bool Reject(params Entity[] children)
        {
            var success = false;
            for (int i = 0; i < children.Length; i++) success |= Reject(children[i]);
            return success;
        }

        public bool Replace(Entity child, Entity replacement)
        {
            ref var relationships = ref GetRelationships(replacement, out var success);
            return success && Replace(child, ref relationships);
        }

        public bool Clear()
        {
            var cleared = false;
            for (int i = 0; i < _relationships.Length; i++)
            {
                ref var relationships = ref _relationships[i];
                cleared |= relationships.Entity && Reject(ref relationships);
            }
            return cleared;
        }

        [ThreadSafe]
        bool TryGetRelationships(Entity entity, out Relationships relationships)
        {
            relationships = GetRelationships(entity, out var success);
            return success;
        }

        [ThreadSafe]
        ref Relationships GetRelationships(Entity entity, out bool success)
        {
            if (entity && entity.Index < _relationships.Length)
            {
                ref var relationships = ref _relationships[entity.Index];
                success = entity == relationships.Entity;
                return ref relationships;
            }
            success = false;
            return ref Dummy<Relationships>.Value;
        }

        void Initialize(Entity entity)
        {
            ArrayUtility.Ensure(ref _relationships, entity.Index + 1);
            ref var relationships = ref _relationships[entity.Index];
            relationships.Entity = entity;
            relationships.Children.items = relationships.Children.items ?? Array.Empty<Entity>();
        }

        void Dispose(Entity entity)
        {
            ref var relationships = ref _relationships[entity.Index];
            Reject(ref relationships);
            for (int i = relationships.Children.count - 1; i >= 0; i--)
            {
                var child = relationships.Children.items[i];
                if (RejectAt(i, ref relationships)) _entities.Destroy(child);
            }
            relationships.Entity = default;
        }

        [ThreadSafe]
        bool Has(Entity parent, ref Relationships child)
        {
            ref var relationships = ref GetRelationships(parent, out var success);
            return success && Has(ref relationships, ref child);
        }

        [ThreadSafe]
        bool Has(ref Relationships parent, ref Relationships child) => child.Parent == parent.Entity;

        bool Adopt(ref Relationships parent, Entity child)
        {
            ref var relationships = ref GetRelationships(child, out var success);
            return success && Adopt(ref parent, ref relationships);
        }

        bool Adopt(ref Relationships parent, ref Relationships child) => AdoptAt(parent.Children.count, ref parent, ref child);

        bool AdoptAt(int index, ref Relationships parent, Entity child)
        {
            ref var relationships = ref GetRelationships(child, out var success);
            return success && AdoptAt(index, ref parent, ref relationships);
        }

        bool AdoptAt(int index, ref Relationships parent, ref Relationships child)
        {
            if (parent.Entity == child.Entity) return false;

            // NOTE: the child must be rejected from its current parent
            Reject(ref child);
            // NOTE: all ancestors must be rejected from the child's children to ensure that no family loop is created
            Reject(ref child, ref parent);
            ref var relationships = ref GetRelationships(parent.Parent, out var success);
            while (success)
            {
                if (Reject(ref child, ref relationships)) break;
                relationships = ref GetRelationships(relationships.Parent, out success);
            }

            parent.Children.Insert(Math.Min(parent.Children.count, index), child.Entity);
            child.Parent = parent.Entity;
            _onAdopt.Emit(new OnAdopt { Parent = parent.Entity, Child = child.Entity, Index = index });
            return true;
        }

        bool Reject(ref Relationships child)
        {
            ref var relationships = ref GetRelationships(child.Parent, out var success);
            return success && Reject(ref relationships, ref child);
        }

        bool Reject(ref Relationships parent, ref Relationships child) =>
            child.Parent == parent.Entity && RejectAt(parent.Children.IndexOf(child.Entity), ref parent, ref child);

        bool RejectAt(int index, ref Relationships parent)
        {
            ref var relationships = ref GetRelationships(parent.Children.items[index], out var success);
            return success && RejectAt(index, ref parent, ref relationships);
        }

        bool RejectAt(int index, ref Relationships parent, ref Relationships child)
        {
            if (parent.Children.RemoveAt(index))
            {
                child.Parent = default;
                _onReject.Emit(new OnReject { Parent = parent.Entity, Child = child.Entity, Index = index });
                return true;
            }
            return false;
        }

        bool Replace(Entity child, ref Relationships replacement)
        {
            ref var relationships = ref GetRelationships(child, out var success);
            return success && Replace(ref relationships, ref replacement);
        }

        bool Replace(ref Relationships child, ref Relationships replacement)
        {
            ref var relationships = ref GetRelationships(child.Parent, out var success);
            return success && Replace(ref child, ref relationships, ref replacement);
        }

        bool Replace(ref Relationships child, ref Relationships parent, ref Relationships replacement)
        {
            if (child.Entity == replacement.Entity) return false;

            var index = parent.Children.IndexOf(child.Entity);
            return index >= 0 && RejectAt(index, ref parent, ref child) && AdoptAt(index, ref parent, ref replacement);
        }
    }
}
