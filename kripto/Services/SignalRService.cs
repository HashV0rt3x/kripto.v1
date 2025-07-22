using Microsoft.AspNetCore.SignalR.Client;

public class SignalRService
{
    public HubConnection Connection;
    public Action<string, string> OnIncomingCall;
    public Action<string, string> OnCallAnswered;
    public Action<string>? OnCallEnded;
    public Action<string>? OnCallRejected;
    public Func<string, string, Task>? OnIceCandidateReceived;


    public async Task ConnectAsync(string myId)
    {
        Connection = new HubConnectionBuilder()
            .WithUrl($"http://192.168.0.206:5001/callhub?userId={myId}")
            .WithAutomaticReconnect()
            .Build();

        Connection.On<string, string>("IncomingCall", (from, offer) =>
        {
            OnIncomingCall?.Invoke(from, offer);
        });

        Connection.On<string, string>("CallAnswered", (from, answer) =>
        {
            OnCallAnswered?.Invoke(from, answer);
        });

        Connection.On<string>("CallEnded", (fromConnId) =>
        {
            OnCallEnded?.Invoke(fromConnId);
        });

        Connection.On<string>("CallRejected", (from) =>
        {
            OnCallRejected?.Invoke(from);
        });

        Connection.On<string, string>("ReceiveIceCandidate", (from, candidate) =>
        {
            OnIceCandidateReceived?.Invoke(from, candidate);
        });

        await Connection.StartAsync();
    }

    public async Task SendIceCandidateAsync(string toUser, string candidate)
    {
        await Connection.InvokeAsync("SendIceCandidate", toUser, candidate);
    }

    public async Task CallUserAsync(string targetId, string offer)
    {
        await Connection.InvokeAsync("CallUser", targetId, offer);
    }

    public async Task AnswerCallAsync(string callerConnId, string answer)
    {
        await Connection.InvokeAsync("AnswerCall", callerConnId, answer);
    }
    public async Task RejectCallAsync(string callerConnId)
    {
        await Connection.InvokeAsync("RejectCall", callerConnId);
    }
    public async Task EndCallAsync(string connId)
    {
        await Connection.InvokeAsync("EndCall", connId);
    }
}
