using System.Reflection;

namespace LegacyCore.Utilities
{
    internal static class ReflectionUtil
    {
        public static void SetPrivateField(this object obj, string name, object value)
            => obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(obj, value);

        public static T GetPrivateField<T>(this object obj, string name)
            => (T)obj.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(obj);
    }
}
