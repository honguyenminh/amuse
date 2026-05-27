const PAUSE_FADE_SEC = 0.12;
const MIN_GAIN = 0.0001;
const FALLBACK_FADE_MS = 120;

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function clampVolume(volume: number): number {
  return Math.max(0, Math.min(1, volume));
}

export type PlaybackOutput = {
  audio: HTMLAudioElement;
  prime: () => void;
  setVolume: (volume: number) => void;
  playSmooth: () => Promise<void>;
  pauseSmooth: () => Promise<void>;
  pauseImmediate: () => void;
};

/**
 * Playback output with optional Web Audio gain ramping.
 *
 * Pause clicks come from stopping the media decoder, not from speaker volume.
 * Ramping a GainNode (with `crossOrigin="anonymous"` + bucket CORS) fades the
 * signal before `pause()`. Falls back to element `volume` ramps when Web Audio
 * is unavailable.
 */
export function createPlaybackOutput(): PlaybackOutput {
  const audio = new Audio();
  audio.preload = "metadata";
  audio.crossOrigin = "anonymous";

  let context: AudioContext | null = null;
  let gainNode: GainNode | null = null;
  let webAudio = false;
  let targetVolume = 0.85;
  let pauseInFlight: Promise<void> | null = null;
  let fadeGeneration = 0;

  audio.volume = targetVolume;

  function tryWire(): boolean {
    if (webAudio) return true;
    try {
      context = new AudioContext();
      const source = context.createMediaElementSource(audio);
      gainNode = context.createGain();
      gainNode.gain.value = targetVolume;
      source.connect(gainNode);
      gainNode.connect(context.destination);
      audio.volume = 1;
      webAudio = true;
      return true;
    } catch {
      context = null;
      gainNode = null;
      return false;
    }
  }

  async function resumeContext(): Promise<AudioContext | null> {
    if (!tryWire()) return null;
    if (context!.state === "suspended") await context!.resume();
    return context;
  }

  async function fadeElementVolume(to: number, durationMs: number): Promise<void> {
    const generation = ++fadeGeneration;
    const from = audio.volume;
    const steps = Math.max(1, Math.round(durationMs / 10));
    for (let step = 1; step <= steps; step++) {
      if (generation !== fadeGeneration) return;
      audio.volume = from + (to - from) * (step / steps);
      await sleep(Math.ceil(durationMs / steps));
    }
    if (generation === fadeGeneration) audio.volume = to;
  }

  function cancelFades() {
    fadeGeneration++;
    if (webAudio && context && gainNode) {
      const t = context.currentTime;
      gainNode.gain.cancelScheduledValues(t);
    }
  }

  return {
    audio,

    prime() {
      void resumeContext();
    },

    setVolume(volume: number) {
      targetVolume = clampVolume(volume);
      if (webAudio && gainNode && context) {
        const t = context.currentTime;
        gainNode.gain.cancelScheduledValues(t);
        gainNode.gain.value = audio.paused ? targetVolume : gainNode.gain.value;
        if (!audio.paused) {
          gainNode.gain.setValueAtTime(gainNode.gain.value, t);
          gainNode.gain.linearRampToValueAtTime(targetVolume, t + 0.02);
        }
      } else if (audio.paused) {
        audio.volume = targetVolume;
      }
    },

    async playSmooth() {
      if (pauseInFlight) await pauseInFlight;
      cancelFades();

      const ctx = await resumeContext();
      if (ctx && gainNode) {
        const t = ctx.currentTime;
        gainNode.gain.cancelScheduledValues(t);
        gainNode.gain.setValueAtTime(targetVolume, t);
        try {
          await audio.play();
        } catch (error) {
          throw error;
        }
        return;
      }

      try {
        await audio.play();
      } catch (error) {
        audio.volume = targetVolume;
        throw error;
      }
      audio.volume = targetVolume;
    },

    async pauseSmooth() {
      if (audio.paused) return;

      const run = async () => {
        cancelFades();
        const ctx = await resumeContext();

        if (ctx && gainNode) {
          const t = ctx.currentTime;
          const current = Math.max(gainNode.gain.value, MIN_GAIN);
          gainNode.gain.cancelScheduledValues(t);
          gainNode.gain.setValueAtTime(current, t);
          gainNode.gain.exponentialRampToValueAtTime(MIN_GAIN, t + PAUSE_FADE_SEC);
          await sleep(Math.ceil(PAUSE_FADE_SEC * 1000) + 25);
          if (!audio.paused) audio.pause();
          const t2 = ctx.currentTime;
          gainNode.gain.cancelScheduledValues(t2);
          gainNode.gain.setValueAtTime(targetVolume, t2);
          return;
        }

        await fadeElementVolume(0, FALLBACK_FADE_MS);
        if (!audio.paused) audio.pause();
        audio.volume = targetVolume;
      };

      pauseInFlight = run().finally(() => {
        pauseInFlight = null;
      });
      await pauseInFlight;
    },

    pauseImmediate() {
      cancelFades();
      if (webAudio && gainNode && context) {
        const t = context.currentTime;
        gainNode.gain.setValueAtTime(targetVolume, t);
      }
      audio.pause();
      audio.volume = targetVolume;
    },
  };
}
