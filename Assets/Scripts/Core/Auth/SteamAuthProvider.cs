using System;
using UnityEngine;

public sealed class SteamAuthProvider : MonoBehaviour
{
    [SerializeField] private string editorTicketOverride;

    public void RequestTicket(Action<bool, string> onCompleted)
    {
#if UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(editorTicketOverride))
        {
            onCompleted?.Invoke(true, editorTicketOverride);
            return;
        }
#endif

        // Steamworks.NET is intentionally not referenced until the package/app id are added.
        FlowGuard.Info(FlowGuard.TagRestApi, "Steam ticket provider is not configured; using offline account flow.", this);
        onCompleted?.Invoke(false, null);
    }
}
