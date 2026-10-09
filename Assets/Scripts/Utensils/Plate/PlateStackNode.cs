using System.Collections.Generic;
using Unity.Netcode.Components;

public class PlateStackNode : AttachableNode
{
    public readonly List<AttachableBehaviour> Stack = new List<AttachableBehaviour>();

    protected override void OnAttached(AttachableBehaviour a)
    {
        if (!Stack.Contains(a)) Stack.Add(a);
    }

    protected override void OnDetached(AttachableBehaviour a)
    {
        Stack.Remove(a);
    }
}