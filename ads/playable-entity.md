A playable entity is an entity that can be:

1. streamed
2. added to/removed from playing queue, both play next and queue bottom

Concrete playable entities would be single tracks.



A playable entity collection is a collection of playable entities, which can:

1. contain zero, one, or more playable entity.
2. be a playable entity itself, which will queue its content on being queued.
3. be saved to library.

Concrete playable entity collections would be releases (not release group), playlists and mixes.