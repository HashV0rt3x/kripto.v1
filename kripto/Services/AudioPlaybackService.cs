using kripto.Helpers;
using NAudio.Wave;
using SIPSorceryMedia.Abstractions;

public class AudioPlaybackService
{
    private BufferedWaveProvider? _bufferedWaveProvider;
    private IWavePlayer? _waveOut;

    public void StartPlayback(AudioFormat format)
    {
        var waveFormat = new WaveFormat(format.ClockRate, 16, format.ChannelCount);
        _bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        _waveOut = new WaveOutEvent();
        _waveOut.Init(_bufferedWaveProvider);
        _waveOut.Play();
    }

    public void PlayEncodedFrame(EncodedAudioFrame frame)
    {
        if (_bufferedWaveProvider != null && frame?.EncodedAudio != null)
        {
            var pcm = G711uLawDecoder.Decode(frame.EncodedAudio);
            _bufferedWaveProvider.AddSamples(pcm, 0, pcm.Length);
        }
    }

    public void PlayEncodedFrame(byte[] encodedAudio)
    {
        if (_bufferedWaveProvider != null && encodedAudio != null)
        {
            var pcm = G711uLawDecoder.Decode(encodedAudio);
            _bufferedWaveProvider.AddSamples(pcm, 0, pcm.Length);
        }
    }

    public void PlayFrame(byte[] pcmData)
    {
        if (_bufferedWaveProvider != null && pcmData != null)
        {
            _bufferedWaveProvider.AddSamples(pcmData, 0, pcmData.Length);
        }
    }

    public void StopPlayback()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _waveOut = null;
    }
}
