using System;

namespace Entia
{
    [Flags]
    public enum States : byte
    {
        None = 0,
        All = Enabled | Disabled,
        Enabled = 1 << 0,
        Disabled = 1 << 1
    }
}
