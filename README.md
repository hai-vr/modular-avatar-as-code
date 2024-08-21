Animator As Code - Modular Avatar functions
=====

*Animator As Code - Modular Avatar functions* contains facilities to automate the creation of Modular Avatar components.

The functionality of this library is strictly limited to the handling of animators and parameters.

## Installation

Install using the VRChat Creator Companion or ALCOM. Instructions are available in [this page](https://docs.hai-vr.dev/docs/products/animator-as-code/install).

## Documentation

- The documentation is available at [docs.hai-vr.dev](https://docs.hai-vr.dev/docs/products/animator-as-code/functions/modular-avatar).

## Example usage

```csharp
var ctrl = aac.NewAnimatorController();
var fx = ctrl.NewLayer();

var toggleFloatParameter = fx.FloatParameter("MyToggle");

fx.NewState("MotionTime")
    .WithAnimation(aac.NewBlendTree().Simple1D(toggleFloatParameter)
        .WithAnimation(aac.NewClip().Toggling(myObject, false), 0)
        .WithAnimation(aac.NewClip().Toggling(myObject, true), 1)
    )
    .WithWriteDefaultsSetTo(true);

// Create a new object in the scene. We will add Modular Avatar components inside it.
var modularAvatar = MaAc.Create(holder);

// By creating a Modular Avatar Merge Animator component,
// our animator controller will be added to the avatar's FX layer.
modularAvatar.NewMergeAnimator(ctrl, VRCAvatarDescriptor.AnimLayerType.FX);

// We use a float in the animator blend tree, but we declare it as a bool
// so that it takes 1 bit in the expression parameters.
// By default, it is saved and synced.
modularAvatar.NewBoolToFloatParameter(toggleFloatParameter).WithDefaultValue(true);
```


```csharp
var ma = MaAc.Create(my.gameObject);
ma.NewParameter(interpolatorLayer.FloatParameter("ConvergencePlaneDistance5M")).NotSaved();
ma.NewParameter(reduceRange);
ma.NewParameter(useConvergence).WithDefaultValue(true);
ma.NewMergeAnimator(fx, VRCAvatarDescriptor.AnimLayerType.FX);
ma.EditMenuItem(my.menuReduceRange).Name("Reduce FlowerViz range").Toggle(reduceRange).WithIcon(my.menuReduceRangeIcon);
ma.EditMenuItem(my.menuUseConvergence).Name("Use Convergence").Toggle(useConvergence).WithIcon(my.menuUseConvergenceIcon);
```
