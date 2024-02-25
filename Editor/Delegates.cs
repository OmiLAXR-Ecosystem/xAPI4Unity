#if UNITY_EDITOR
using System;

namespace xAPI4Unity.Editor
{
    public delegate void OnTriggeredControlHandler(Type type, string label, string value);
}
#endif