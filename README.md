Notes:

- By default, parameters are synced and saved. Call NonSynced() and NonSaved() to save them.
- It has become more usual and more usual for animators to use floats instead of bool parameters because they can be used in blend trees for use in toggles. However, the expression parameter can be synced as a bool, with an implicit cast. In these cases, use the methods containing "BoolToFloat" to express that quirk.
- By default, the Merge Animator is initialized with absolute paths. TODO: decide if this should be relative/specified on init with a fluent buildern
- TODO: Allow merge animator to be initialized on another object than itself (this goes for any object, really)