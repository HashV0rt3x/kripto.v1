using NAudio.Wave;

public class AudioService
{
    private WaveInEvent waveIn;
    private BufferedWaveProvider buffer;
    private WaveOutEvent waveOut;

    public void Start()
    {
        waveIn = new WaveInEvent();
        waveIn.DeviceNumber = 0;
        waveIn.WaveFormat = new WaveFormat(16000, 1); // Mono, 16kHz
        waveIn.DataAvailable += OnDataAvailable;
        waveIn.StartRecording();

        buffer = new BufferedWaveProvider(waveIn.WaveFormat)
        {
            DiscardOnBufferOverflow = true
        };

        waveOut = new WaveOutEvent();
        waveOut.Init(buffer);
        waveOut.Play();
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Loopback uchun: shu audio signalni o‘zimizga eshittiramiz
        buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
    }

    public void Stop()
    {
        waveIn?.StopRecording();
        waveIn?.Dispose();
        waveOut?.Stop();
        waveOut?.Dispose();
    }
}
