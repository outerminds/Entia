using System;

namespace Entia.Core.Documentation
{
    [Flags]
    public enum Filters
    {
        None = 0, All = ~0,

        Types = Struct | Class | Interface | Enum | Delegate,
        Struct = 1 << 0,
        Class = 1 << 1,
        Interface = 1 << 2,
        Enum = 1 << 3,
        Delegate = 1 << 4,

        Accesses = Private | Protected | Internal | Public,
        Private = 1 << 5,
        Protected = 1 << 6,
        Internal = 1 << 7,
        Public = 1 << 8,

        Members = Field | Property | Method | Constructor | Destructor | Event | Type,
        Field = 1 << 9,
        Property = 1 << 10,
        Method = 1 << 11,
        Constructor = 1 << 12,
        Destructor = 1 << 13,
        Event = 1 << 14,
        Type = 1 << 15,

        Modifiers = Static | Sealed | Ref | ReadOnly,
        Static = 1 << 16,
        Sealed = 1 << 17,
        Ref = 1 << 18,
        ReadOnly = 1 << 19,

        Generic = 1 << 20,
    }

    public static class FiltersExtensions
    {
        public static bool HasAll(this Filters filters, Filters others) => (filters & others) == others;
        public static bool HasAny(this Filters filters, Filters others) => (filters & others) != 0;
        public static bool HasNone(this Filters filters, Filters others) => !filters.HasAny(others);
    }

    /// <summary>
    /// Indicates that the target of the attribute is thread-safe.
    /// <para/>
    /// -> In the case of a type, it indicates that all its <c>public</c> members (static and instance) are thread-safe.
    /// <para/>
    /// -> In the case of a property or event, it indicates that all its <c>public</c> accessors are thread-safe.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = true, AllowMultiple = false)]
    public sealed class ThreadSafeAttribute : Attribute { }

    [AttributeUsage(
        AttributeTargets.ReturnValue | AttributeTargets.Property | AttributeTargets.Field,
        Inherited = true, AllowMultiple = false)]
    public sealed class RefUsageAttribute : Attribute { }

    [AttributeUsage(
        AttributeTargets.ReturnValue | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field,
        Inherited = true, AllowMultiple = false)]
    public sealed class ReadUsageAttribute : Attribute { }

    public sealed class DisableDiagnosticsAttribute : Attribute { }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = true, AllowMultiple = true)]
    public sealed class TypeDiagnosticAttribute : Attribute
    {
        public readonly string Message;
        public Filters WithAllFilters, WithAnyFilters, WithNoneFilters;
        public Filters HaveAllFilters, HaveAnyFilters, HaveNoneFilters;
        public string[] WithAnyPrefixes, WithNonePrefixes;
        public string[] HaveAnyPrefixes, HaveNonePrefixes;
        public string[] WithAnySuffixes, WithNoneSuffixes;
        public string[] HaveAnySuffixes, HaveNoneSuffixes;
        public Type[] WithAllImplementations, WithAnyImplementations, WithNoneImplementations;
        public Type[] HaveAllImplementations, HaveAnyImplementations, HaveNoneImplementations;
        public Type[] WithAllAttributes, WithAnyAttributes, WithNoneAttributes;
        public Type[] HaveAllAttributes, HaveAnyAttributes, HaveNoneAttributes;
        public TypeDiagnosticAttribute(string message) { Message = message; }
    }

    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface,
        Inherited = true, AllowMultiple = true)]
    public sealed class ImplementsOneOf : Attribute
    {
        public readonly Type[] Types;
        public ImplementsOneOf(params Type[] types) { Types = types; }
    }

    // public sealed class InstanceMembersMustBeFieldsAttribute : TypeDiagnosticAttribute
    // {
    //     public InstanceMembersMustBeFieldsAttribute() : base("Instance member '{member}' in type '{type}' must be a field.")
    //     {
    //         WithAnyFilters = Filters.Members;
    //         WithNoneFilters = Filters.Static;
    //         HaveAllFilters = Filters.Field;
    //     }
    // }

    // public sealed class InstanceFieldsMustBePublicAttribute : TypeDiagnosticAttribute
    // {
    //     public InstanceFieldsMustBePublicAttribute() : base("Instance field '{member}' in type '{type}' must be public.")
    //     {
    //         WithAnyFilters = Filters.Field;
    //         WithNoneFilters = Filters.Static;
    //         HaveAllFilters = Filters.Public;
    //     }
    // }

    // public sealed class InstanceFieldsMustNotBeReadOnlyAttribute : TypeDiagnosticAttribute
    // {
    //     public InstanceFieldsMustNotBeReadOnlyAttribute() : base("Instance field '{member}' in type '{type}' must not be readonly.")
    //     {
    //         WithAnyFilters = Filters.Field;
    //         WithNoneFilters = Filters.Static;
    //         HaveNoneFilters = Filters.ReadOnly;
    //     }
    // }

    // public sealed class ImplementationsMustBeStructsAttribute : TypeDiagnosticAttribute
    // {
    //     public ImplementationsMustBeStructsAttribute() : base("Type '{type}' must be a struct.")
    //     {
    //         WithAllFilters = Filters.Types;
    //         HaveAllFilters = Filters.Struct;
    //     }
    // }

    // public sealed class MembersMustNotImplementAttribute : TypeDiagnosticAttribute
    // {
    //     public MembersMustNotImplementAttribute(params Type[] types) : base("Field '{member}' in type '{type}' must not store a value that implements '{implementation}'.")
    //     {
    //         WithAnyFilters = Filters.Members;
    //         HaveNoneImplementations = types;
    //     }
    // }

    // public sealed class PublicInstanceFieldsMustImplementAttribute : TypeDiagnosticAttribute
    // {
    //     public PublicInstanceFieldsMustImplementAttribute(params Type[] types) : base("Public field '{member}' in type '{type}' must store a value that implements '{implementation}'.")
    //     {
    //         WithAllFilters = Filters.Field | Filters.Public;
    //         HaveAllImplementations = types;
    //     }
    // }

    // public sealed class PrivateInstanceFieldsMustNotImplementAttribute : TypeDiagnosticAttribute
    // {
    //     public PrivateInstanceFieldsMustNotImplementAttribute(params Type[] types) : base("Private field '{member}' in type '{type}' must not store a value that implements '{implementation}'.")
    //     {
    //         WithAllFilters = Filters.Field | Filters.Private | Filters.Protected | Filters.Internal;
    //         HaveNoneImplementations = types;
    //     }
    // }

    // [ImplementationsMustBeStructs, InstanceMembersMustBeFields, InstanceFieldsMustBePublic, InstanceFieldsMustNotBeReadOnly]
    // [MembersMustNotImplement(typeof(IComponent), typeof(IResource), typeof(ISystem), typeof(IMessage), typeof(IPhase), typeof(IQueryable), typeof(INode))]
    // [TypeDiagnostic("Instance member '{member}' in type '{type}' must be a field.",
    //     WithAnyFilters = Filters.Members, WithNoneFilters = Filters.Static,
    //     HaveAllFilters = Filters.Field)]
    // [TypeDiagnostic("",
    //     WithAllFilters = Filters.Members | Filters.Static, WithAnyPrefixes = new[] { "Default", "GetDefault" },
    //     HaveAllAttributes = new[] { typeof(DefaultAttribute) })]
    // public interface IComponent { }

    // public interface IResource { }

    // public interface IMessage { }

    // [Implementations(AreAll = Types.Struct)]
    // [Members(Members.All, WithNoneModifiers = Filters.Static,
    //     AreAll = Members.Field,
    //     HaveAllAccesses = Accesses.Public,
    //     HaveNoneModifiers = Filters.ReadOnly,
    //     HaveNoneImplementations = new[] { typeof(IComponent), typeof(IResource), typeof(ISystem), typeof(IQueryable) })]
    // public interface IPhase { }

    // [Implementations(AreAll = Types.Struct)]
    // [Members(Members.Field, WithAllAccesses = Accesses.Public, WithNoneModifiers = Filters.Static,
    //     HaveAllImplementations = new[] { typeof(IInjectable) })]
    // [Members(Members.Field, WithAllAccesses = Accesses.Private, WithNoneModifiers = Filters.Static)]
    // public interface ISystem
    // {

    // }

    // [Implementations(AreAll = Types.Struct)]
    // [Members(
    //     Members.All, AreAll = Members.Field,
    //     HaveAllAccesses = Accesses.Public,
    //     WithNoneModifiers = Filters.Static,
    //     HaveNoneModifiers = Filters.ReadOnly,
    //     HaveAllImplementations = new[] { typeof(IQueryable), typeof(ValueType) },
    //     HaveNoneImplementations = new[] { typeof(IComponent), typeof(IResource), typeof(ISystem), typeof(IMessage), typeof(IPhase), typeof(INode) })]
    // public interface IQueryable { }

    // [Implementations(AreAll = Types.Struct)]
    // public interface INode { }

    // public interface IInjectable { }
}