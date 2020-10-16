using System;
using System.Globalization;

namespace Entia.Json.Converters
{
    public sealed class ConcreteTimeSpan : Converter<TimeSpan>
    {
        public override Node Convert(in TimeSpan instance, in ToContext context) => instance.Ticks;
        public override TimeSpan Instantiate(in FromContext context) =>
            context.Node.TryLong(out var ticks) ? new TimeSpan(ticks) :
            context.Node.TryString(out var value) ? TimeSpan.Parse(value, CultureInfo.InvariantCulture) :
            default;
    }
}