namespace Entia.Modules
{
    public sealed partial class Components
    {
        /// <summary>
        /// Removes the components on the <paramref name="target"/> that the <paramref name="source"/> does not have.
        /// </summary>
        /// <param name="source">The source entity.</param>
        /// <param name="target">The target entity.</param>
        /// <param name="include">A filter that includes only the components that correspond to the provided states.</param>
        /// <returns>Returns <c>true</c> if a component was removed; otherwise, <c>false</c>.</returns>
        public bool Trim(Entity source, Entity target, States include = States.All)
        {
            ref var sourceData = ref GetData(source, out var sourceSuccess);
            ref var targetData = ref GetData(target, out var targetSuccess);
            if (sourceSuccess && targetSuccess)
            {
                ref var targetSlot = ref GetTransientSlot(target, ref targetData, Resolutions.None);
                var types = GetTargetTypes(targetSlot);
                var trimmed = false;
                for (int i = 0; i < types.Length; i++)
                {
                    ref readonly var metadata = ref types[i];
                    trimmed |=
                        TryGetDelegates(metadata, out var delegates) &&
                        !Has(sourceData, metadata, delegates, States.All) &&
                        Remove(ref targetSlot, metadata, delegates, include);
                }
                return trimmed;
            }
            return false;
        }
    }
}