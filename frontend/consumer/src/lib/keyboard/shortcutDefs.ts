export type ShortcutKeyPart = string | string[];

export type ShortcutDefinition = {
  id: string;
  label: string;
  keys: ShortcutKeyPart[];
  group: "Playback" | "Navigation" | "Volume" | "Help";
};

export const SHORTCUT_DEFINITIONS: ShortcutDefinition[] = [
  {
    id: "play-pause",
    label: "Play / pause",
    keys: [["Ctrl", "Space"]],
    group: "Playback",
  },
  {
    id: "stop",
    label: "Stop playback (panic pause)",
    keys: [["Ctrl", "S"]],
    group: "Playback",
  },
  {
    id: "next",
    label: "Next track",
    keys: [["Ctrl", "→"]],
    group: "Playback",
  },
  {
    id: "previous",
    label: "Previous track",
    keys: [["Ctrl", "←"]],
    group: "Playback",
  },
  {
    id: "now-playing",
    label: "Now playing",
    keys: [["Ctrl", "I"]],
    group: "Navigation",
  },
  {
    id: "home",
    label: "Home",
    keys: [["Ctrl", "H"]],
    group: "Navigation",
  },
  {
    id: "search",
    label: "Search (focus bar)",
    keys: [["Ctrl", "K"]],
    group: "Navigation",
  },
  {
    id: "library",
    label: "Library",
    keys: [["Ctrl", "L"]],
    group: "Navigation",
  },
  {
    id: "settings",
    label: "Settings",
    keys: [["Ctrl", ","]],
    group: "Navigation",
  },
  {
    id: "volume-up",
    label: "Volume up",
    keys: [["Ctrl", "↑"]],
    group: "Volume",
  },
  {
    id: "volume-down",
    label: "Volume down",
    keys: [["Ctrl", "↓"]],
    group: "Volume",
  },
  {
    id: "mute",
    label: "Toggle mute",
    keys: [["Ctrl", "M"]],
    group: "Volume",
  },
  {
    id: "help",
    label: "Keyboard shortcuts",
    keys: [["Ctrl", "/"]],
    group: "Help",
  },
  {
    id: "playing-esc",
    label: "Leave now playing",
    keys: ["Esc"],
    group: "Navigation",
  },
];

export const SHORTCUT_GROUPS = ["Playback", "Navigation", "Volume", "Help"] as const;
