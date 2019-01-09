using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Entia.Core
{
	public static class MethodInfoExtensions
	{
		public static T CreateDelegate<T>(this MethodInfo method) where T : class, ICloneable, ISerializable =>
			Delegate.CreateDelegate(typeof(T), method) as T;
	}
}
