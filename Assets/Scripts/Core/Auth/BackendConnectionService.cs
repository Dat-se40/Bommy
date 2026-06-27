using System;
using System.Threading;
using System.Threading.Tasks;
using Nakama;
using UnityEngine;

public struct BackendConnectionResult
{
    public bool Success { get; }
    public string Error { get; }

    private BackendConnectionResult(bool success, string error)
    {
        Success = success;
        Error = error;
    }

    public static BackendConnectionResult SuccessResult() => new(true, null);
    public static BackendConnectionResult FailResult(string error) => new(false, error);
}

public sealed class BackendConnectionService : MonoBehaviour
{
    private static BackendConnectionService instance;
    public static BackendConnectionService Instance => instance;

    public event Action<int> OnAttemptCountChanged;

    private int currentAttempts;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<BackendConnectionResult> CheckBackendAsync()
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return BackendConnectionResult.SuccessResult();

        AuthService authService = AuthService.GetOrCreate();

        try
        {
            IApiRpc rpc = await authService.RpcUnauthenticatedAsync("healthcheck", "{}");
            if (rpc != null && !string.IsNullOrEmpty(rpc.Payload) && rpc.Payload.Contains("\"ok\":true"))
            {
                return BackendConnectionResult.SuccessResult();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[BackendConnectionService] Healthcheck failed: {ex.Message}");
            return BackendConnectionResult.FailResult(ex.Message);
        }

        return BackendConnectionResult.FailResult("Invalid healthcheck response.");
    }

    public async Task<BackendConnectionResult> WaitForBackendAsync(CancellationToken token)
    {
        if (DedicatedServerBootstrap.IsDedicatedServerRuntime)
            return BackendConnectionResult.SuccessResult();

        int attempt = 0;
        float delay = 0.5f; // Starting delay

        while (!token.IsCancellationRequested)
        {
            attempt++;
            SetAttemptCount(attempt);

            BackendConnectionResult result = await CheckBackendAsync();
            if (result.Success)
            {
                return result;
            }

            // Exponential backoff capped around 5 seconds
            float waitTime = Mathf.Min(delay, 5.0f);
            delay *= 1.5f;

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(waitTime), token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }

        return BackendConnectionResult.FailResult("Cancelled or timed out waiting for backend.");
    }

    public void SetAttemptCount(int attempts)
    {
        currentAttempts = attempts;
        OnAttemptCountChanged?.Invoke(attempts);
    }

    public int GetAttemptCount()
    {
        return currentAttempts;
    }
}
