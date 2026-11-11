using NAudio.CoreAudioApi;
using NAudio.Wave;
using Raylib_cs;

public enum Layer { Rumble, Crackle, Wind, Rain, Crickets, Birds, Thunder, Clock, Brown, Pink, Waves, Train, Hum, Bubbles, Underwater, Purr, Water }

/// <summary>Short sounds a scene can play now and then (random sound events).</summary>
public enum Evt { Chimes, Gull, Owl, Pop, Thunder, Notify } // Notify: the soft kalimba for the update pop-up

/// <summary>
/// Ambient audio mixer. Each layer plays a real (CC0) recording from the Assets folder,
/// looped seamlessly; if a file is missing the layer falls back to a synthesized version.
/// Audio is pulled by Windows (WASAPI) on its own thread, so it never depends on the video frame rate.
/// </summary>
public unsafe class AmbientAudio : ISampleProvider, IDisposable
{
    const int Rate = 48000;
    const int LayerCount = 17;

    public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(Rate, 2);

    readonly WasapiOut? output;
    readonly Random rng = new();

    public readonly float[] Target = new float[LayerCount];
    readonly float[] cur = new float[LayerCount];
    public volatile float Master = 0.7f;
    public volatile float Fade = 1f;

    // Real recordings, one looping buffer per layer (null = use the synth fallback).
    readonly float[]?[] loops = new float[LayerCount][];
    readonly int[] pos = new int[LayerCount];

    // Thunder is a one-shot taken from a rain+thunder clip, low-passed so mostly the rumble remains.
    float[]? thunder;
    int thunderStart, thunderPos = -1, thunderDelay;

    public bool HasRecording(Layer l) => l == Layer.Thunder ? thunder != null : loops[(int)l] != null;

    double t;

    /// <summary>How fast layer volumes ease to their targets (per sample). Lower = slower crossfades.</summary>
    public volatile float Smooth = 0.00008f;

    // noise generator state
    float brown, pb0, pb1, pb2, pb3, pb4, pb5, pb6;

    // synth fallback state
    float rum1, rum2, hissLp;
    float crackEnv, crackLp;
    float wind1, wind2;
    float rain1, rain2;
    float th1, th2, thAtt, thDec; bool thActive;
    double cPhaseA, cPhaseB; float cEnvA, cEnvB;
    int chirpLeft, chirpTotal, birdGap = Rate * 3, notesLeft;
    float chirpF0, chirpF1, birdBase = 3000; double birdPhase;
    int clockCounter; float tickEnv, tickFreq = 1900; double tickPhase; bool tock;

    public AmbientAudio(bool openDevice = true)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Assets");
        Load(Layer.Crackle, Path.Combine(dir, "fire.wav"), 0.16f, 0.5f);
        Load(Layer.Rain, Path.Combine(dir, "rain.ogg"), 0.13f, 1.0f);
        Load(Layer.Wind, Path.Combine(dir, "wind.ogg"), 0.12f, 1.0f);
        Load(Layer.Crickets, Path.Combine(dir, "crickets.mp3"), 0.11f, 0.15f);
        Load(Layer.Birds, Path.Combine(dir, "forest.mp3"), 0.12f, 1.5f);
        Load(Layer.Clock, Path.Combine(dir, "clock.wav"), 0f, 0f, peak: 0.35f);
        Load(Layer.Waves, Path.Combine(dir, "waves.ogg"), 0.14f, 1.0f);
        Load(Layer.Train, Path.Combine(dir, "train.ogg"), 0.12f, 1.5f);
        Load(Layer.Hum, Path.Combine(dir, "hum.mp3"), 0.10f, 1.5f);
        Load(Layer.Bubbles, Path.Combine(dir, "bubbles.wav"), 0.10f, 0.3f);
        Load(Layer.Underwater, Path.Combine(dir, "underwater.mp3"), 0.10f, 2.0f);
        Load(Layer.Purr, Path.Combine(dir, "purr.ogg"), 0.10f, 0.5f);
        Load(Layer.Water, Path.Combine(dir, "water.wav"), 0.10f, 1.5f);
        LoadEvents(dir);
        LoadThunder(Path.Combine(dir, "thunder.ogg"));

        if (!openDevice) return;
        try
        {
            output = new WasapiOut(AudioClientShareMode.Shared, 200);
            output.Init(this);
            output.Play();
        }
        catch { output = null; } // no audio device: run silently
    }

    /// <summary>Decode any wav/ogg/mp3 to mono float at our sample rate.</summary>
    static float[]? Decode(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var wave = Raylib.LoadWave(path);
            Raylib.WaveFormat(ref wave, Rate, 32, 1);
            float* s = Raylib.LoadWaveSamples(wave);
            int n = (int)wave.FrameCount;
            var raw = new float[n];
            for (int i = 0; i < n; i++) raw[i] = s[i];
            Raylib.UnloadWaveSamples(s);
            Raylib.UnloadWave(wave);
            return n > Rate ? raw : null;
        }
        catch { return null; }
    }

    void Load(Layer layer, string path, float rmsTarget, float crossfadeSecs, float peak = 0)
    {
        var raw = Decode(path);
        if (raw == null) return;
        Finish(layer, raw, rmsTarget, crossfadeSecs, peak);
    }

    // Turns decoded audio into a seamless, loudness-matched loop for a layer.
    void Finish(Layer layer, float[] raw, float rmsTarget, float crossfadeSecs, float peak)
    {
        int n = raw.Length;
        int x = Math.Min((int)(Rate * crossfadeSecs), n / 4);
        var loop = new float[n - x];
        Array.Copy(raw, loop, loop.Length);
        // Equal-power crossfade of the tail into the head so the loop has no seam.
        for (int i = 0; i < x; i++)
        {
            float p = (float)i / x;
            loop[i] = raw[i] * MathF.Sin(p * MathF.PI / 2) + raw[n - x + i] * MathF.Cos(p * MathF.PI / 2);
        }

        float gain = 1;
        if (peak > 0)
        {
            float m = 0;
            foreach (float v in loop) m = MathF.Max(m, MathF.Abs(v));
            if (m > 1e-5f) gain = peak / m;
        }
        else
        {
            double sum = 0;
            foreach (float v in loop) sum += v * v;
            float rms = (float)Math.Sqrt(sum / loop.Length);
            if (rms > 1e-5f) gain = rmsTarget / rms;
        }
        for (int i = 0; i < loop.Length; i++) loop[i] *= gain;
        loops[(int)layer] = loop;
        posB[(int)layer] = loop.Length / 2f; // the second copy of a layered loop starts half a loop away
    }

    void LoadThunder(string path)
    {
        var raw = Decode(path);
        if (raw == null) return;
        // Two one-pole low-passes (~200 Hz) keep the rumble and drop most of the rain hiss.
        float a = 1 - MathF.Exp(-2 * MathF.PI * 200 / Rate);
        float y1 = 0, y2 = 0, peak = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            y1 += (raw[i] - y1) * a;
            y2 += (y1 - y2) * a;
            raw[i] = y2;
            peak = MathF.Max(peak, MathF.Abs(y2));
        }
        if (peak < 1e-6f) return;
        for (int i = 0; i < raw.Length; i++) raw[i] *= 0.9f / peak;

        // Find the loudest moment (100 ms windows) and start playback ~2.5 s before it.
        int win = Rate / 10, best = 0; float bestE = 0;
        for (int i = 0; i + win < raw.Length; i += win)
        {
            float e = 0;
            for (int k = 0; k < win; k++) e += raw[i + k] * raw[i + k];
            if (e > bestE) { bestE = e; best = i; }
        }
        thunderStart = Math.Max(0, best - (int)(Rate * 2.5));
        thunder = raw;
    }

    /// <summary>Schedule distant thunder (rumble arrives a moment after the flash).</summary>
    public void TriggerThunder()
    {
        thunderDelay = (int)(Rate * (0.1 + rng.NextDouble() * 0.4));
        thunderQueued = true;
    }
    volatile bool thunderQueued;

    // ISampleProvider: called by WASAPI on its own thread.
    public int Read(float[] buffer, int offset, int count)
    {
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        Reads++; FramesRead += count / 2;
        for (int i = 0; i < count; i += 2)
        {
            float s = Next();
            buffer[offset + i] = s;
            buffer[offset + i + 1] = s;
        }
        double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        if (ms > MaxReadMs) MaxReadMs = ms;
        return count;
    }
    public long Reads, FramesRead; public double MaxReadMs;

    static readonly Layer[] RecordedOnly = { Layer.Waves, Layer.Train, Layer.Hum, Layer.Bubbles, Layer.Underwater, Layer.Purr, Layer.Water };

    // Layers whose short loop is played twice, the second copy half a loop away and a little slower,
    // so the two drift against each other and the repeat is hard to notice.
    static readonly bool[] Layered = MakeLayered(Layer.Waves, Layer.Crickets, Layer.Wind, Layer.Underwater, Layer.Train, Layer.Water, Layer.Rain);
    static bool[] MakeLayered(params Layer[] ls) { var a = new bool[LayerCount]; foreach (var l in ls) a[(int)l] = true; return a; }
    const float SecondSpeed = 0.93f;
    readonly float[] posB = new float[LayerCount];

    float Loop(Layer l)
    {
        int li = (int)l;
        var b = loops[li]!;
        int p = pos[li];
        float s = b[p];
        if (++p >= b.Length) p = 0;
        pos[li] = p;
        if (Layered[li])
        {
            float q = posB[li];
            int qi = (int)q, q2 = qi + 1 >= b.Length ? 0 : qi + 1;
            float sb = b[qi] + (b[q2] - b[qi]) * (q - qi);
            q += SecondSpeed;
            if (q >= b.Length) q -= b.Length;
            posB[li] = q;
            s = (s + sb) * 0.7f; // two uncorrelated copies at 0.7 keep the loudness the same
        }
        return s;
    }

    float Next()
    {
        t += 1.0 / Rate;
        float w = (float)(rng.NextDouble() * 2 - 1);
        for (int i = 0; i < LayerCount; i++) cur[i] += (Target[i] - cur[i]) * Smooth;

        float o = 0;

        // Fire roar (synth only)
        float vRum = cur[(int)Layer.Rumble];
        if (vRum > 0.001f)
        {
            rum1 += (w - rum1) * 0.02f;
            rum2 += (rum1 - rum2) * 0.02f;
            o += rum2 * 10f * (0.8f + 0.2f * MathF.Sin((float)(t * 0.9))) * vRum;
        }

        // Fire
        float vCr = cur[(int)Layer.Crackle];
        if (loops[(int)Layer.Crackle] != null) { if (vCr > 0.001f) o += Loop(Layer.Crackle) * vCr; else Skip(Layer.Crackle); }
        else if (vCr > 0.001f)
        {
            if (rng.NextDouble() < 9.0 / Rate) { float big = (float)rng.NextDouble(); crackEnv = 0.25f + big * big * 0.75f; }
            crackEnv *= 0.9985f;
            crackLp += (w - crackLp) * 0.5f;
            hissLp += (w - hissLp) * 0.1f;
            o += (crackLp * crackEnv * 0.9f + (w - hissLp) * 0.02f) * vCr;
        }

        // Wind
        float vWind = cur[(int)Layer.Wind];
        if (loops[(int)Layer.Wind] != null) { if (vWind > 0.001f) o += Loop(Layer.Wind) * vWind; else Skip(Layer.Wind); }
        else if (vWind > 0.001f)
        {
            wind1 += (w - wind1) * 0.03f;
            wind2 += (w - wind2) * 0.004f;
            float gust = (0.5f + 0.5f * MathF.Sin((float)(t * 2 * Math.PI * 0.07)))
                       * (0.6f + 0.4f * MathF.Sin((float)(t * 2 * Math.PI * 0.17) + 1f));
            o += (wind1 - wind2) * 5f * (0.25f + 0.75f * gust) * vWind;
        }

        // Rain
        float vRain = cur[(int)Layer.Rain];
        if (loops[(int)Layer.Rain] != null) { if (vRain > 0.001f) o += Loop(Layer.Rain) * vRain; else Skip(Layer.Rain); }
        else if (vRain > 0.001f)
        {
            rain1 += (w - rain1) * 0.15f;
            rain2 += (w - rain2) * 0.02f;
            o += ((w - rain1) * 0.28f + rain2 * 1.6f) * (0.9f + 0.1f * MathF.Sin((float)(t * 0.4))) * vRain;
        }

        // Crickets
        float vCk = cur[(int)Layer.Crickets];
        if (loops[(int)Layer.Crickets] != null) { if (vCk > 0.001f) o += Loop(Layer.Crickets) * vCk; else Skip(Layer.Crickets); }
        else if (vCk > 0.001f)
        {
            cPhaseA += 2 * Math.PI * 4300 / Rate;
            cPhaseB += 2 * Math.PI * 4650 / Rate;
            cEnvA += (CricketGate(1.9, 0.0) - cEnvA) * 0.01f;
            cEnvB += (CricketGate(1.37, 0.31) - cEnvB) * 0.01f;
            o += (MathF.Sin((float)cPhaseA) * cEnvA + MathF.Sin((float)cPhaseB) * cEnvB) * 0.05f * vCk;
        }

        // Forest / birds
        float vBird = cur[(int)Layer.Birds];
        if (loops[(int)Layer.Birds] != null) { if (vBird > 0.001f) o += Loop(Layer.Birds) * vBird; else Skip(Layer.Birds); }
        else SynthBirds(ref o, vBird);

        // Layers that only ever play a recording
        foreach (var l in RecordedOnly)
        {
            if (loops[(int)l] == null) continue;
            float v = cur[(int)l];
            if (v > 0.001f) o += Loop(l) * v; else Skip(l);
        }

        // Noise layers (synthesized: they are just noise)
        float vBrown = cur[(int)Layer.Brown];
        if (vBrown > 0.001f)
        {
            brown = (brown + 0.02f * w) / 1.02f;
            o += brown * 5f * vBrown;
        }
        float vPink = cur[(int)Layer.Pink];
        if (vPink > 0.001f)
        {
            pb0 = 0.99886f * pb0 + w * 0.0555179f;
            pb1 = 0.99332f * pb1 + w * 0.0750759f;
            pb2 = 0.96900f * pb2 + w * 0.1538520f;
            pb3 = 0.86650f * pb3 + w * 0.3104856f;
            pb4 = 0.55000f * pb4 + w * 0.5329522f;
            pb5 = -0.7616f * pb5 - w * 0.0168980f;
            float pink = pb0 + pb1 + pb2 + pb3 + pb4 + pb5 + pb6 + w * 0.5362f;
            pb6 = w * 0.115926f;
            o += pink * 0.12f * vPink;
        }

        // Thunder one-shot
        if (thunder != null)
        {
            if (thunderQueued && thunderDelay > 0 && --thunderDelay == 0) { thunderQueued = false; thunderPos = thunderStart; }
            if (thunderPos >= 0)
            {
                o += thunder[thunderPos] * cur[(int)Layer.Thunder];
                if (++thunderPos >= thunder.Length) thunderPos = -1;
            }
        }
        else SynthThunder(ref o, w);

        // Clock
        float vClock = cur[(int)Layer.Clock];
        if (loops[(int)Layer.Clock] != null) { if (vClock > 0.001f) o += Loop(Layer.Clock) * vClock; else Skip(Layer.Clock); }
        else SynthClock(ref o, w, vClock);

        MixEvent(ref o);

        o *= Master * Fade;
        return MathF.Tanh(o);
    }

    // Keep silent layers advancing so they come back in the middle of the loop, not restarting abruptly.
    void Skip(Layer l)
    {
        var b = loops[(int)l]!;
        if (++pos[(int)l] >= b.Length) pos[(int)l] = 0;
        if (Layered[(int)l] && (posB[(int)l] += SecondSpeed) >= b.Length) posB[(int)l] -= b.Length;
    }

    // ---- random sound events (owl, chimes, gull, log pop, distant thunder) ----

    readonly float[]?[] clips = new float[6][];
    static readonly float[] ClipGain = { 0.4f, 0.25f, 0.18f, 0.25f, 0, 0.3f };
    float[]? evBuf; int evPos, evStart, evEnd, evFade; float evGain; bool evAlways;
    volatile int evQueued = -1;
    /// <summary>Loudness of the random events relative to the loops (0 = off).</summary>
    public volatile float EventLevel = 1f;

    /// <summary>Ask the audio thread to play a one-shot. Called from the main thread; one event plays at a time.</summary>
    public void PlayEvent(Evt e)
    {
        if (e == Evt.Thunder) { TriggerThunder(); return; }
        evQueued = (int)e;
    }

    // Loads the recorded clips and builds the synthesized ones (owl, pop).
    void LoadEvents(string dir)
    {
        clips[(int)Evt.Chimes] = Peaked(Decode(Path.Combine(dir, "chimes.ogg")));
        clips[(int)Evt.Gull] = Peaked(Decode(Path.Combine(dir, "gull.wav")));
        clips[(int)Evt.Owl] = MakeOwl();
        clips[(int)Evt.Pop] = MakePop();
        clips[(int)Evt.Notify] = Peaked(Decode(Path.Combine(dir, "notify.wav")));
    }

    static float[]? Peaked(float[]? a)
    {
        if (a == null) return null;
        float m = 0;
        foreach (float v in a) m = MathF.Max(m, MathF.Abs(v));
        if (m > 1e-5f) for (int i = 0; i < a.Length; i++) a[i] /= m;
        return a;
    }

    // A soft owl call: one long hoo, a pause, then three shorter ones, each a gently falling tone.
    static float[] MakeOwl()
    {
        var a = new float[(int)(Rate * 3.2)];
        void Hoot(double at, double dur, double f0)
        {
            int s0 = (int)(at * Rate), n = (int)(dur * Rate);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < a.Length; i++)
            {
                double p = (double)i / n;
                double env = Math.Pow(Math.Sin(Math.PI * Math.Pow(p, 0.7)), 2);
                ph += 2 * Math.PI * f0 * (1 - 0.1 * p) / Rate;
                a[s0 + i] += (float)((Math.Sin(ph) + 0.25 * Math.Sin(2 * ph)) * env);
            }
        }
        Hoot(0.0, 0.5, 420); Hoot(1.1, 0.28, 400); Hoot(1.5, 0.28, 400); Hoot(1.9, 0.6, 380);
        return Peaked(a)!;
    }

    // A log pop: a very short burst of low-passed noise with a quick decay.
    static float[] MakePop()
    {
        var rnd = new Random(3);
        var a = new float[(int)(Rate * 0.25)];
        float lp = 0;
        for (int i = 0; i < a.Length; i++)
        {
            lp += ((float)(rnd.NextDouble() * 2 - 1) - lp) * 0.25f;
            a[i] = lp * MathF.Exp(-i / (Rate * 0.02f));
        }
        return Peaked(a)!;
    }

    // Starts a queued event (on the audio thread). Chimes play a random 14 s excerpt; the gull plays its first 8 s.
    void StartEvent(int kind)
    {
        var b = clips[kind];
        if (b == null) return;
        int len = kind == (int)Evt.Chimes ? Rate * 14 : kind == (int)Evt.Gull ? Rate * 8 : b.Length;
        len = Math.Min(len, b.Length);
        evStart = kind == (int)Evt.Chimes && b.Length > len ? rng.Next(0, b.Length - len) : 0;
        evEnd = evStart + len; evPos = evStart; evBuf = b; evGain = ClipGain[kind];
        evFade = kind == (int)Evt.Chimes ? Rate * 2 : kind == (int)Evt.Pop ? Rate / 500 : (int)(Rate * 0.05);
        evAlways = kind == (int)Evt.Notify; // the update chime plays even with random events switched off
    }

    // Mixes the playing event into o.
    void MixEvent(ref float o)
    {
        int q = evQueued;
        if (q >= 0) { evQueued = -1; StartEvent(q); }
        if (evBuf == null) return;
        float env = MathF.Min(1f, MathF.Min((evPos - evStart) / (float)evFade, (evEnd - evPos) / (float)evFade));
        o += evBuf[evPos] * env * evGain * (evAlways ? 1f : EventLevel);
        if (++evPos >= evEnd) evBuf = null;
    }

    // ---- synth fallbacks (only used if a recording is missing) ----

    void SynthBirds(ref float o, float vBird)
    {
        if (chirpLeft > 0)
        {
            float p = 1f - chirpLeft / (float)chirpTotal;
            birdPhase += 2 * Math.PI * (chirpF0 + (chirpF1 - chirpF0) * p) / Rate;
            o += MathF.Sin((float)birdPhase) * MathF.Sin(MathF.PI * p) * 0.16f * vBird;
            chirpLeft--;
        }
        else if (birdGap > 0) birdGap--;
        else if (notesLeft > 0)
        {
            chirpTotal = chirpLeft = (int)(Rate * (0.06 + rng.NextDouble() * 0.08));
            chirpF0 = birdBase * (0.9f + (float)rng.NextDouble() * 0.2f);
            chirpF1 = chirpF0 * (0.7f + (float)rng.NextDouble() * 0.8f);
            birdGap = (int)(Rate * (0.05 + rng.NextDouble() * 0.07));
            notesLeft--;
        }
        else
        {
            notesLeft = 2 + rng.Next(5);
            birdBase = 2200 + (float)rng.NextDouble() * 2000;
            birdGap = (int)(Rate * (4 + rng.NextDouble() * 16));
        }
    }

    void SynthThunder(ref float o, float w)
    {
        if (thunderQueued && thunderDelay > 0 && --thunderDelay == 0) { thunderQueued = false; thActive = true; thAtt = 0; thDec = 1; }
        if (!thActive) return;
        th1 += (w - th1) * 0.006f;
        th2 += (th1 - th2) * 0.006f;
        thAtt += (1 - thAtt) * 0.0001f;
        thDec *= 0.999995f;
        o += th2 * 20f * thAtt * thDec * (0.65f + 0.35f * MathF.Sin((float)(t * 2 * Math.PI * 0.5))) * cur[(int)Layer.Thunder];
        if (thDec < 0.001f) thActive = false;
    }

    void SynthClock(ref float o, float w, float vClock)
    {
        if (++clockCounter >= Rate) { clockCounter = 0; tock = !tock; tickFreq = tock ? 1500 : 1900; tickEnv = 1; }
        if (tickEnv > 0.001f && vClock > 0.001f)
        {
            tickPhase += 2 * Math.PI * tickFreq / Rate;
            o += (MathF.Sin((float)tickPhase) * 0.6f + w * 0.4f) * tickEnv * 0.18f * vClock;
        }
        tickEnv *= 0.9975f;
    }

    float CricketGate(double rate, double offset)
    {
        double g = (t * rate + offset) % 1.0;
        if (g >= 0.4) return 0;
        return (g / 0.4 * 4) % 1.0 < 0.6 ? 1f : 0f;
    }

    /// <summary>Render audio without a device (benchmark / tests).</summary>
    public void Render(float[] stereo) => Read(stereo, 0, stereo.Length);

    public void Dispose()
    {
        try { output?.Stop(); output?.Dispose(); } catch { }
    }
}
