using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Windows;
using System.Text.Json;

public class WebRtcService
{
    public RTCPeerConnection Peer;
    private WindowsAudioEndPoint micInput;
    private WindowsAudioEndPoint speakerOutput;

    private bool isMuted = false;

    public Action<string> OnIceCandidateReady;

    public void InitAsCaller()
    {
        Init();
    }

    public void InitAsReceiver()
    {
        Init();
    }

    private void Init()
    {
        micInput = new WindowsAudioEndPoint(new AudioEncoder(), -1, -1, false, true); // mic
        speakerOutput = new WindowsAudioEndPoint(new AudioEncoder(), -1, -1, true, false); // speaker

        var config = new RTCConfiguration
        {
            iceServers = new List<RTCIceServer>
            {
                new RTCIceServer { urls = "stun:stun.l.google.com:19302" }
            }
        };

        Peer = new RTCPeerConnection(config);

        var audioTrack = new MediaStreamTrack(micInput.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);
        Peer.addTrack(audioTrack);
        micInput.OnAudioSourceEncodedSample += Peer.SendAudio;

        Peer.OnAudioFormatsNegotiated += (formats) =>
        {
            var format = formats.First();
            micInput.SetAudioSourceFormat(format);
            speakerOutput.SetAudioSinkFormat(format);
        };

        Peer.OnRtpPacketReceived += (rep, media, pkt) =>
        {
            if (media == SDPMediaTypesEnum.audio)
            {
                speakerOutput.GotAudioRtp(
                    rep,
                    pkt.Header.SyncSource,
                    pkt.Header.SequenceNumber,
                    pkt.Header.Timestamp,
                    pkt.Header.PayloadType,
                    pkt.Header.MarkerBit == 1,
                    pkt.Payload);
            }
        };

        Peer.onicecandidate += (candidate) =>
        {
            if (candidate != null)
            {
                var json = JsonSerializer.Serialize(candidate.toJSON());
                OnIceCandidateReady?.Invoke(json);
            }
        };

        Peer.onconnectionstatechange += async (state) =>
        {
            Console.WriteLine($"WebRTC connection state: {state}");
            if (state == RTCPeerConnectionState.connected)
            {
                await micInput.StartAudio();
                await speakerOutput.StartAudioSink();
            }
            else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
            {
                await micInput.CloseAudio();
                await speakerOutput.CloseAudio();
            }
        };
    }

    public async Task<string> CreateOfferAsync()
    {
        var offer = Peer.createOffer(null);
        await Peer.setLocalDescription(offer);
        return offer.sdp;
    }

    public async Task<string> CreateAnswerAsync()
    {
        var answer = Peer.createAnswer(null);
        await Peer.setLocalDescription(answer);
        return answer.sdp;
    }

    public async Task SetRemoteDescriptionAsync(string sdp, RTCSdpType type)
    {
        Peer.setRemoteDescription(new RTCSessionDescriptionInit
        {
            sdp = sdp,
            type = type
        });
    }

    public async Task AddIceCandidateAsync(string json)
    {
        var candidate = JsonSerializer.Deserialize<RTCIceCandidateInit>(json);
        Peer.addIceCandidate(candidate);
        await Task.CompletedTask;
    }

    public void StartAudio()
    {
        micInput?.StartAudio();
        speakerOutput?.StartAudioSink();
    }

    public void Mute()
    {
        if (!isMuted)
        {
            micInput?.CloseAudio();
            isMuted = true;
        }
    }

    public void Unmute()
    {
        if (isMuted)
        {
            micInput?.StartAudio();
            isMuted = false;
        }
    }

    public void Close()
    {
        Peer?.Close("User ended call");
        micInput?.CloseAudio();
        speakerOutput?.CloseAudio();
    }
}
