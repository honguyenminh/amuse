type BufferRange = { start: number; end: number };

type DashBufferController = {
  getAllRangesWithSafetyFactor?: (seekTime: number) => BufferRange[];
  clearBuffers?: (ranges: BufferRange[]) => Promise<unknown>;
  setIsBufferingCompleted?: (value: boolean) => void;
};

type DashScheduleController = {
  startScheduleTimer?: (timeout?: number) => void;
};

export type DashAudioStreamProcessor = {
  getType?: () => string;
  getBufferController?: () => DashBufferController;
  setExplicitBufferingTime?: (time: number) => void;
  getScheduleController?: () => DashScheduleController;
};

export type DashActiveStream = {
  getStreamProcessors?: () => DashAudioStreamProcessor[];
};

export type DashPlayerWithStream = {
  getActiveStream?: () => DashActiveStream | null;
};

export function getContinuousBufferEnd(audio: HTMLAudioElement, timeSec: number): number | null {
  const buffered = audio.buffered;
  for (let i = 0; i < buffered.length; i++) {
    if (timeSec >= buffered.start(i) && timeSec <= buffered.end(i)) {
      return buffered.end(i);
    }
  }
  return null;
}

export function hasBufferedRangeAfter(audio: HTMLAudioElement, afterSec: number): boolean {
  const buffered = audio.buffered;
  for (let i = 0; i < buffered.length; i++) {
    if (buffered.start(i) > afterSec + 0.1) return true;
  }
  return false;
}

export function bufferAheadSeconds(audio: HTMLAudioElement, timeSec: number): number {
  const end = getContinuousBufferEnd(audio, timeSec);
  if (end === null) return 0;
  return Math.max(0, end - timeSec);
}

export function getAudioStreamProcessor(
  player: DashPlayerWithStream,
): DashAudioStreamProcessor | null {
  const processors = player.getActiveStream?.()?.getStreamProcessors?.() ?? [];
  return processors.find((processor) => processor.getType?.() === "audio") ?? null;
}

export function resyncBufferingFromPlayhead(
  player: DashPlayerWithStream,
  audio: HTMLAudioElement,
): void {
  if (audio.seeking) return;

  const processor = getAudioStreamProcessor(player);
  if (!processor) return;

  const timeSec = audio.currentTime;
  if (bufferAheadSeconds(audio, timeSec) > 1) return;

  const continuousEnd = getContinuousBufferEnd(audio, timeSec);
  const resumeFrom = continuousEnd ?? timeSec;

  processor.setExplicitBufferingTime?.(resumeFrom);
  processor.getBufferController?.()?.setIsBufferingCompleted?.(false);
  processor.getScheduleController?.()?.startScheduleTimer?.(0);
}

export async function pruneDisjointAheadBuffer(
  player: DashPlayerWithStream,
  audio: HTMLAudioElement,
  seekTimeSec: number,
): Promise<void> {
  const continuousEnd = getContinuousBufferEnd(audio, seekTimeSec);
  if (continuousEnd === null) return;
  if (!hasBufferedRangeAfter(audio, continuousEnd)) return;

  const processor = getAudioStreamProcessor(player);
  const bufferController = processor?.getBufferController?.();
  if (!bufferController?.getAllRangesWithSafetyFactor || !bufferController.clearBuffers) {
    return;
  }

  const clearRanges = bufferController.getAllRangesWithSafetyFactor(seekTimeSec);
  if (clearRanges.length === 0) return;

  bufferController.setIsBufferingCompleted?.(false);
  await bufferController.clearBuffers(clearRanges);

  processor?.setExplicitBufferingTime?.(continuousEnd);
  processor?.getScheduleController?.()?.startScheduleTimer?.(0);
}
