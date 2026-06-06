const PAUSE_FADE_SEC = 0.12;
const PLAY_FADE_SEC = 0.12;
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
  setNormalizationGain: (gain: number) => void;
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
  let normalizationGain = 1;
  let pauseInFlight: Promise<void> | null = null;
  let fadeGeneration = 0;

  function webAudioOutputGain(): number {
    return Math.max(0, targetVolume * normalizationGain);
  }

  function fallbackElementVolume(): number {
    return clampVolume(targetVolume * normalizationGain);
  }

  audio.volume = fallbackElementVolume();

  function tryWire(): boolean {
    if (webAudio) return true;
    try {
      context = new AudioContext();
      const source = context.createMediaElementSource(audio);
      gainNode = context.createGain();
      gainNode.gain.value = webAudioOutputGain();
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

  function applyWebAudioGain(immediate = false) {
    if (!webAudio || !gainNode || !context) return;
    const outputGain = webAudioOutputGain();
    const t = context.currentTime;
    gainNode.gain.cancelScheduledValues(t);
    if (immediate || audio.paused) {
      gainNode.gain.setValueAtTime(outputGain, t);
      return;
    }
    gainNode.gain.setValueAtTime(gainNode.gain.value, t);
    gainNode.gain.linearRampToValueAtTime(outputGain, t + 0.05);
  }

  return {
    audio,

    prime() {
      void resumeContext();
    },

    setVolume(volume: number) {
      targetVolume = clampVolume(volume);
      if (webAudio && gainNode && context) {
        applyWebAudioGain();
      } else if (audio.paused) {
        audio.volume = fallbackElementVolume();
      }
    },

    setNormalizationGain(gain: number) {
      normalizationGain = Number.isFinite(gain) && gain > 0 ? gain : 1;
      if (webAudio && gainNode && context) {
        applyWebAudioGain();
      } else if (audio.paused) {
        audio.volume = fallbackElementVolume();
      }
    },

    async playSmooth() {
      if (pauseInFlight) await pauseInFlight;
      cancelFades();

      const outputGain = webAudioOutputGain();
      const ctx = await resumeContext();
      if (ctx && gainNode) {
        const t = ctx.currentTime;
        gainNode.gain.cancelScheduledValues(t);
        gainNode.gain.setValueAtTime(MIN_GAIN, t);
        gainNode.gain.exponentialRampToValueAtTime(Math.max(outputGain, MIN_GAIN), t + PLAY_FADE_SEC);
        try {
          await audio.play();
        } catch (error) {
          throw error;
        }
        return;
      }

      const elementTarget = fallbackElementVolume();
      try {
        audio.volume = 0;
        await audio.play();
      } catch (error) {
        audio.volume = elementTarget;
        throw error;
      }
      await fadeElementVolume(elementTarget, FALLBACK_FADE_MS);
    },

    async pauseSmooth() {
      if (audio.paused) return;

      const run = async () => {
        const freezeAtSec =
          Number.isFinite(audio.currentTime) && audio.currentTime >= 0
            ? audio.currentTime
            : 0;

        cancelFades();
        const outputGain = webAudioOutputGain();
        const ctx = await resumeContext();

        if (ctx && gainNode) {
          const t = ctx.currentTime;
          const current = Math.max(gainNode.gain.value, MIN_GAIN);
          gainNode.gain.cancelScheduledValues(t);
          gainNode.gain.setValueAtTime(current, t);
          gainNode.gain.exponentialRampToValueAtTime(MIN_GAIN, t + PAUSE_FADE_SEC);
          await sleep(Math.ceil(PAUSE_FADE_SEC * 1000) + 25);
          if (!audio.paused) audio.pause();
          audio.currentTime = freezeAtSec;
          const t2 = ctx.currentTime;
          gainNode.gain.cancelScheduledValues(t2);
          gainNode.gain.setValueAtTime(outputGain, t2);
          return;
        }

        await fadeElementVolume(0, FALLBACK_FADE_MS);
        if (!audio.paused) audio.pause();
        audio.currentTime = freezeAtSec;
        audio.volume = fallbackElementVolume();
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
        gainNode.gain.setValueAtTime(webAudioOutputGain(), t);
      }
      audio.pause();
      audio.volume = fallbackElementVolume();
    },
  };
}
