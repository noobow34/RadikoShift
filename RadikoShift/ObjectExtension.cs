using System.Runtime.CompilerServices;

namespace RadikoShift
{
    public static class ObjectExtension
    {
        private static readonly ConditionalWeakTable<object, string> _instanceIdList = [];

        public static void JournalWriteLine(this object obj, string value)
        {
            if (!_instanceIdList.TryGetValue(obj, out string? instanceId))
            {
                instanceId = Ulid.NewUlid().ToString();
                _instanceIdList.Add(obj, instanceId);
            }
            Console.WriteLine($"【{obj.GetType().Name}:{instanceId}】{value}");
        }
    }
}
