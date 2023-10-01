ModularAvatarAsCode
====

Haï speaking: ModularAvatarAsCode is a companion library to AnimatorAsCode V1 that I am using in my personal avatar project to generate animator controllers non-destructively, and then declare the animator, its parameters, and the menu items to Modular Avatar.

It is wholly incomplete, but serves my purpose.

## Example usage

```csharp

var aac = NdAac.AnimatorAsCode("IkgAcEyeVizRange", my.relative, my.variant, my.assetKey, NdAac.Options().WriteDefaultsOn());

var fx = aac.NewAnimatorController();
var mainLayer = fx.CreateLayer();

var reduceRange = mainLayer.BoolParameter("FT_FlowerVizReduceRange");
var useConvergence = mainLayer.BoolParameter("FT_FlowerVizUseConvergence");

// (build the animator controller...)

var ma = MaAc.Create(my.gameObject);
ma.NewParameter(interpolatorLayer.FloatParameter("ConvergencePlaneDistance5M")).NotSaved();
ma.NewParameter(reduceRange);
ma.NewParameter(useConvergence).WithDefaultValue(true);
ma.NewMergeAnimator(fx, VRCAvatarDescriptor.AnimLayerType.FX);
ma.EditMenuItem(my.menuReduceRange).Name("Reduce FlowerViz range").Toggle(reduceRange).WithIcon(my.menuReduceRangeIcon);
ma.EditMenuItem(my.menuUseConvergence).Name("Use Convergence").Toggle(useConvergence).WithIcon(my.menuUseConvergenceIcon);
```

## Notes

- By default, parameters are synced and saved. Call NonSynced() and NonSaved() to save them.
- It has become more usual and more usual for animators to use floats instead of bool parameters because they can be used in blend trees for use in toggles. However, the expression parameter can be synced as a bool, with an implicit cast. In these cases, use the methods containing "BoolToFloat" to express that quirk.
- By default, the Merge Animator is initialized with absolute paths. TODO: decide if this should be relative/specified on init with a fluent buildern
- TODO: Allow merge animator to be initialized on another object than itself (this goes for any object, really)
- TODO: some functions carry state across invocations, i.e. if you create a parameter A, and never create it again, it may persist. This may have to be fixed or stabilized by decided how *semi-destructive* this workflow is