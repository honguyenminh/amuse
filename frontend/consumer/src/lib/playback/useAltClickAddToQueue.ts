"use client";

import { useQueueAddBurst } from "@/components/ui/QueueAddBurstProvider";
import { useSnackbar } from "@/components/ui/SnackbarProvider";
import { getCatalogRelease } from "@/lib/api/catalogClient";
import { getPlaylistPlayableTracks } from "@/lib/api/discoveryClient";
import { fromPlayableTrackDto } from "@/lib/playback/toPlaybackTrack";
import { useCallback, useState } from "react";
import { usePlayback } from "./PlaybackContext";
import { countNewQueueTracks, queueAddSnackbarMessage } from "./queueAddFeedback";
import { playableTracksFromRelease } from "./toPlaybackTrack";
import type { PlaybackTrack } from "./types";

const QUEUE_ADD_PULSE_MS = 550;

function useQueueAddPulse() {
  const { showBurstAt } = useQueueAddBurst();
  const [queueAddPulsing, setQueueAddPulsing] = useState(false);

  const triggerFeedback = useCallback(
    (x: number, y: number) => {
      showBurstAt(x, y);
      setQueueAddPulsing(true);
      window.setTimeout(() => setQueueAddPulsing(false), QUEUE_ADD_PULSE_MS);
    },
    [showBurstAt],
  );

  return { queueAddPulsing, triggerFeedback };
}

export function usePlayableClick(options: {
  tracks: PlaybackTrack[];
  hasAudio: boolean;
  onDefaultClick?: () => void;
  releaseTitle?: string;
}) {
  const { addToQueue, state } = usePlayback();
  const { showSnackbar } = useSnackbar();
  const { queueAddPulsing, triggerFeedback } = useQueueAddPulse();

  const addTracks = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0) {
        showSnackbar("No playable tracks");
        return;
      }
      const newTracks = countNewQueueTracks(tracks, state.queue);
      showSnackbar(
        queueAddSnackbarMessage(newTracks, { releaseTitle: options.releaseTitle }),
      );
      if (newTracks.length > 0) addToQueue(tracks);
    },
    [addToQueue, options.releaseTitle, showSnackbar, state.queue],
  );

  const onClick = useCallback(
    (event: React.MouseEvent) => {
      if (!event.altKey) {
        options.onDefaultClick?.();
        return;
      }
      event.preventDefault();
      event.stopPropagation();
      if (!options.hasAudio) return;
      triggerFeedback(event.clientX, event.clientY);
      addTracks(options.tracks);
    },
    [addTracks, options, triggerFeedback],
  );

  return { onClick, queueAddPulsing, addTracks };
}

export function useReleasePlayableClick(options: {
  releaseId: string;
  releaseTitle: string;
  tracks?: PlaybackTrack[];
}) {
  const { showSnackbar } = useSnackbar();
  const { queueAddPulsing, triggerFeedback } = useQueueAddPulse();
  const { addToQueue, state } = usePlayback();

  const addTracks = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0) {
        showSnackbar("No playable tracks");
        return;
      }
      const newTracks = countNewQueueTracks(tracks, state.queue);
      showSnackbar(
        queueAddSnackbarMessage(newTracks, { releaseTitle: options.releaseTitle }),
      );
      if (newTracks.length > 0) addToQueue(tracks);
    },
    [addToQueue, options.releaseTitle, showSnackbar, state.queue],
  );

  const onClick = useCallback(
    (event: React.MouseEvent) => {
      if (!event.altKey) return;
      event.preventDefault();
      event.stopPropagation();
      triggerFeedback(event.clientX, event.clientY);
      if (options.tracks && options.tracks.length > 0) {
        addTracks(options.tracks);
        return;
      }
      void getCatalogRelease(options.releaseId)
        .then((release) => addTracks(playableTracksFromRelease(release)))
        .catch(() => showSnackbar("Could not add to queue"));
    },
    [
      addTracks,
      options.releaseId,
      options.tracks,
      showSnackbar,
      triggerFeedback,
    ],
  );

  return { onClick, queueAddPulsing };
}

export function usePlaylistPlayableClick(options: {
  playlistId: string;
  playlistTitle: string;
}) {
  const { showSnackbar } = useSnackbar();
  const { queueAddPulsing, triggerFeedback } = useQueueAddPulse();
  const { addToQueue, state } = usePlayback();

  const addTracks = useCallback(
    (tracks: PlaybackTrack[]) => {
      if (tracks.length === 0) {
        showSnackbar("No playable tracks");
        return;
      }
      const newTracks = countNewQueueTracks(tracks, state.queue);
      showSnackbar(
        queueAddSnackbarMessage(newTracks, { releaseTitle: options.playlistTitle }),
      );
      if (newTracks.length > 0) addToQueue(tracks);
    },
    [addToQueue, options.playlistTitle, showSnackbar, state.queue],
  );

  const onClick = useCallback(
    (event: React.MouseEvent) => {
      if (!event.altKey) return;
      event.preventDefault();
      event.stopPropagation();
      triggerFeedback(event.clientX, event.clientY);
      void getPlaylistPlayableTracks(options.playlistId)
        .then((response) =>
          addTracks(response.tracks.map((track) => fromPlayableTrackDto(track))),
        )
        .catch(() => showSnackbar("Could not add to queue"));
    },
    [addTracks, options.playlistId, showSnackbar, triggerFeedback],
  );

  return { onClick, queueAddPulsing };
}
