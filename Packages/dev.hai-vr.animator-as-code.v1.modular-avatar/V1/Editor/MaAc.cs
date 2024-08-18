using System;
using System.Collections.Generic;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace AnimatorAsCode.V1.ModularAvatar
{
    public class MaAc
    {
        private readonly GameObject _root;
        private readonly bool _isDummy;

        private bool paramsCreated;
        private bool animatorCreated;
        private int _mergeAnimatorIndex = 0;

        private MaAc(GameObject root, bool isDummy = false)
        {
            _root = root;
            _isDummy = isDummy;
        }

        /// Create a Modular Avatar As Code base, targeting the root object. The root object is the object that you will attach the Merge Armature component to.
        public static MaAc Create(GameObject root, bool setDirty = true)
        {
            if (setDirty) EditorUtility.SetDirty(root);
            return new MaAc(root);
        }

        /// Create a new instance of MaAc targeting another object. Beware that this new instance of MaAc forgets all previous method invocations, so invoking some stateful functions like "NewMergeAnimator" may cause different results.
        /// If this instance is a dummy, this remains a dummy and returns itself.
        public MaAc On(GameObject otherRoot)
        {
            if (_isDummy) return this;
            
            return new MaAc(otherRoot);
        }

        /// Create a dummy Modular Avatar As Code base that doesn't create or modify anything when you call any of its methods.<br/>
        /// - If you need to disable the creation of Modular Avatar components without heavily modifying the code, use this.<br/>
        /// - Calling the methods will still return appropriate objects that do nothing.
        public static MaAc Dummy()
        {
            return new MaAc(null, true);
        }

        /// Import parameters from an existing VRCExpressionParameters asset. If the object already contains a ModularAvatarParameters component: The first invocation of this function, or NewParameter function, whichever happens first, will wipe all existing parameters from that component.
        public MaAc ImportParameters(VRCExpressionParameters parameters)
        {
            if (_isDummy) return this;
            
            foreach (var param in parameters.parameters)
            {
                var parameter = EnsureParamComponentCreated();
                parameter.parameters.Add(new ParameterConfig
                {
                    nameOrPrefix = param.name,
                    syncType = SyncType(param.valueType),
                    localOnly = !param.networkSynced,
                    saved = param.saved,
                    defaultValue = param.defaultValue
                });
            }

            return this;
        }

        private ParameterSyncType SyncType(VRCExpressionParameters.ValueType parameterValueType)
        {
            switch (parameterValueType)
            {
                case VRCExpressionParameters.ValueType.Int: return ParameterSyncType.Int;
                case VRCExpressionParameters.ValueType.Float: return ParameterSyncType.Float;
                case VRCExpressionParameters.ValueType.Bool: return ParameterSyncType.Bool;
                default:
                    throw new ArgumentOutOfRangeException(nameof(parameterValueType), parameterValueType, null);
            }
        }

        /// Declare a new Float parameter, by default saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. If the object already contains a ModularAvatarParameters component: The first invocation of this function, or ImportParameters function, whichever happens first, will wipe all existing parameters from that component.
        public MaacParameter<float> NewParameter(AacFlFloatParameter aacParameter)
        {
            if (_isDummy) return MaacParameter<float>.Dummy();
            
            var parameter = CreateParameter(aacParameter.Name, ParameterSyncType.Float, out var index);
            return new MaacParameter<float>(parameter, new [] { index });
        }
        
        /// Declare a new Int parameter, by default saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. If the object already contains a ModularAvatarParameters component: The first invocation of this function, or ImportParameters function, whichever happens first, will wipe all existing parameters from that component.
        public MaacParameter<int> NewParameter(AacFlIntParameter aacParameter)
        {
            if (_isDummy) return MaacParameter<int>.Dummy();

            var parameter = CreateParameter(aacParameter.Name, ParameterSyncType.Int, out var index);
            return new MaacParameter<int>(parameter, new [] { index });
        }
        
        /// Declare a new Bool parameter, by default saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. If the object already contains a ModularAvatarParameters component: The first invocation of this function, or ImportParameters function, whichever happens first, will wipe all existing parameters from that component.
        public MaacParameter<bool> NewParameter(AacFlBoolParameter aacParameter)
        {
            if (_isDummy) return MaacParameter<bool>.Dummy();

            var parameter = CreateParameter(aacParameter.Name, ParameterSyncType.Bool, out var index);
            return new MaacParameter<bool>(parameter, new [] { index });
        }
        
        /// Declare a new Bool parameter, acknowledging that the animator has exposed it as a Float. By default it is saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. If the object already contains a ModularAvatarParameters component: The first invocation of this function, or ImportParameters function, whichever happens first, will wipe all existing parameters from that component.
        public MaacParameter<bool> NewBoolToFloatParameter(AacFlFloatParameter aacParameter)
        {
            if (_isDummy) return MaacParameter<bool>.Dummy();

            var parameter = CreateParameter(aacParameter.Name, ParameterSyncType.Bool, out var index);
            return new MaacParameter<bool>(parameter, new [] { index });
        }

        private ModularAvatarParameters CreateParameter(string parameterName, ParameterSyncType parameterSyncType, out int index)
        {
            var parameter = EnsureParamComponentCreated();
            index = parameter.parameters.Count;
            parameter.parameters.Add(new ParameterConfig
            {
                nameOrPrefix = parameterName,
                syncType = parameterSyncType,
                localOnly = false,
                saved = true
            });
            return parameter;
        }

        /// Declare a new animator to be merged. Every call to NewMergeAnimator will create a new ModularAvatarMergeAnimator, as long as this instance of MaAc is reused. The path mode is set to Absolute.
        public MaacMergeAnimator NewMergeAnimator(AacFlController controller, VRCAvatarDescriptor.AnimLayerType layerType)
        {
            if (_isDummy) return MaacMergeAnimator.Dummy();

            return NewMergeAnimator(controller.AnimatorController, layerType);
        }

        /// Declare a new raw animator to be merged. Every call to NewMergeAnimator will create a new ModularAvatarMergeAnimator, as long as this instance of MaAc is reused. The path mode is set to Absolute.
        public MaacMergeAnimator NewMergeAnimator(AnimatorController animator, VRCAvatarDescriptor.AnimLayerType layerType)
        {
            if (_isDummy) return MaacMergeAnimator.Dummy();

            ModularAvatarMergeAnimator mergeAnimator;
            
            var components = _root.GetComponents<ModularAvatarMergeAnimator>();
            if (_mergeAnimatorIndex >= components.Length)
            {
                mergeAnimator = Undo.AddComponent<ModularAvatarMergeAnimator>(_root);
            }
            else
            {
                mergeAnimator = components[_mergeAnimatorIndex];
            }

            _mergeAnimatorIndex++;
            
            mergeAnimator.animator = animator;
            mergeAnimator.layerType = layerType;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            return new MaacMergeAnimator(mergeAnimator);
        }

        /// Writes over an existing MergeAnimator component, setting the controller and layer type to be merged. The path mode is set to Absolute.
        public MaacMergeAnimator UsingMergeAnimator(ModularAvatarMergeAnimator mergeAnimator, AacFlController controller, VRCAvatarDescriptor.AnimLayerType layerType)
        {
            if (_isDummy) return MaacMergeAnimator.Dummy();

            var animator = controller.AnimatorController;
            return UsingMergeAnimator(mergeAnimator, animator, layerType);
        }

        /// Writes over an existing MergeAnimator component, setting the raw controller and layer type to be merged. The path mode is set to Absolute.
        public MaacMergeAnimator UsingMergeAnimator(ModularAvatarMergeAnimator mergeAnimator, AnimatorController animator, VRCAvatarDescriptor.AnimLayerType layerType)
        {
            if (_isDummy) return MaacMergeAnimator.Dummy();

            mergeAnimator.animator = animator;
            mergeAnimator.layerType = layerType;
            mergeAnimator.pathMode = MergeAnimatorPathMode.Absolute;
            return new MaacMergeAnimator(mergeAnimator);
        }

        /// Declare new Int parameters, by default saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. Function calls on the resulting objects will affect all parameters of that group. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist.
        public MaacParameter<int> NewParameter(AacFlIntParameterGroup aacParameterGroup)
        {
            if (_isDummy) return MaacParameter<int>.Dummy();

            return CreateForGroup<int, AacFlIntParameter>(aacParameterGroup.ToList(), ParameterSyncType.Int);
        }

        /// Declare new Float parameters, by default saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. Function calls on the resulting objects will affect all parameters of that group.
        public MaacParameter<float> NewParameter(AacFlFloatParameterGroup aacParameterGroup)
        {
            if (_isDummy) return MaacParameter<float>.Dummy();
            
            return CreateForGroup<float, AacFlFloatParameter>(aacParameterGroup.ToList(), ParameterSyncType.Float);
        }

        /// Declare new Bool parameters, by default saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. Function calls on the resulting objects will affect all parameters of that group.
        public MaacParameter<bool> NewParameter(AacFlBoolParameterGroup aacParameterGroup)
        {
            if (_isDummy) return MaacParameter<bool>.Dummy();
            
            return CreateForGroup<bool, AacFlBoolParameter>(aacParameterGroup.ToList(), ParameterSyncType.Bool);
        }

        /// Declare new Bool parameters, acknowledging that the animator has exposed them as Floats. By default it is saved and synced. This creates a ModularAvatarParameters on the targeted object if it doesn't already exist. Function calls on the resulting objects will affect all parameters of that group.
        public MaacParameter<bool> NewBoolToFloatParameter(AacFlFloatParameterGroup aacParameterGroup)
        {
            if (_isDummy) return MaacParameter<bool>.Dummy();

            return CreateForGroup<bool, AacFlFloatParameter>(aacParameterGroup.ToList(), ParameterSyncType.Bool);
        }

        private MaacParameter<TParamType> CreateForGroup<TParamType, TParam>(List<TParam> groupAsList, ParameterSyncType parameterSyncType) where TParam : AacFlParameter
        {
            var parameter = EnsureParamComponentCreated();

            var firstIndex = parameter.parameters.Count;
            foreach (var param in groupAsList)
            {
                parameter.parameters.Add(new ParameterConfig
                {
                    nameOrPrefix = param.Name,
                    syncType = parameterSyncType
                });
            }

            return new MaacParameter<TParamType>(parameter, Enumerable.Range(firstIndex, groupAsList.Count).ToArray());
        }

        private ModularAvatarParameters EnsureParamComponentCreated()
        {
            var parameter = GetOrAddComponent<ModularAvatarParameters>(_root);
            if (!paramsCreated)
            {
                parameter.parameters = new List<ParameterConfig>();
                paramsCreated = true;
            }

            return parameter;
        }

        /// Edit one menu item on this object. It is not possible to declare multiple menu items on the same object.
        public MaacMenuItem EditMenuItemOnSelf()
        {
            if (_isDummy) return MaacMenuItem.Dummy();

            return EditMenuItem(_root);
        }

        /// Edit one menu item on the receiver object. It is not possible to declare multiple menu items on that same object.
        public MaacMenuItem EditMenuItem(GameObject receiver)
        {
            if (_isDummy) return MaacMenuItem.Dummy();

            var menuItem = GetOrAddComponent<ModularAvatarMenuItem>(receiver);
            return new MaacMenuItem(new [] { menuItem });
        }

        /// Edit one menu item on all the receiver objects. It is not possible to declare multiple menu items on those same objects. Function calls on the resulting objects will affect all of those menu items. Use this in case you have multiple identical menu items scattered across different menus. The array can safely contain null values.
        public MaacMenuItem EditMenuItem(params GameObject[] receiversWithNulls)
        {
            if (_isDummy) return MaacMenuItem.Dummy();

            var menuItems = receiversWithNulls
                // Warning: Mutating function inside LINQ
                .Where(o => o != null)
                .Select(receiver => GetOrAddComponent<ModularAvatarMenuItem>(receiver))
                .ToArray();
            return new MaacMenuItem(menuItems);
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var result = go.GetComponent<T>();
            return result != null ? result : go.AddComponent<T>();
        }
    }

    public class MaacMergeAnimator
    {
        private readonly ModularAvatarMergeAnimator mergeAnimator;
        private readonly bool _isDummy;

        internal MaacMergeAnimator(ModularAvatarMergeAnimator mergeAnimator)
        {
            this.mergeAnimator = mergeAnimator;
        }

        public static MaacMergeAnimator Dummy()
        {
            return new MaacMergeAnimator();
        }

        private MaacMergeAnimator()
        {
            _isDummy = true;
        }

        /// Mark the path mode as relative. By default, merge animators are made absolute.
        public MaacMergeAnimator Relative()
        {
            if (_isDummy) return this;
            
            mergeAnimator.pathMode = MergeAnimatorPathMode.Relative;
            return this;
        }
    }

    public class MaacMenuItem
    {
        private readonly ModularAvatarMenuItem[] menuItems;
        private readonly bool _isDummy;

        internal MaacMenuItem(ModularAvatarMenuItem[] menuItems)
        {
            this.menuItems = menuItems;
        }

        public static MaacMenuItem Dummy()
        {
            return new MaacMenuItem();
        }

        private MaacMenuItem()
        {
            _isDummy = true;
        }

        /// Set the menu item type as Toggle.
        public MaacMenuItem Toggle(AacFlBoolParameter parameter)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.name = parameter.Name;
                control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                control.value = 1;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Button for a Bool parameter.
        public MaacMenuItem Button(AacFlBoolParameter parameter)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.name = parameter.Name;
                control.type = VRCExpressionsMenu.Control.ControlType.Button;
                control.value = 1;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Toggle for a Bool parameter, which is exposed as a Float in the animator.
        public MaacMenuItem ToggleBoolToFloat(AacFlFloatParameter parameter)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.name = parameter.Name;
                control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                control.value = 1;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Button for a Bool parameter, which is exposed as a Float in the animator.
        public MaacMenuItem ButtonBoolToFloat(AacFlFloatParameter parameter)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.name = parameter.Name;
                control.type = VRCExpressionsMenu.Control.ControlType.Button;
                control.value = 1;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Toggle for a Int parameter, setting the value when toggled.
        public MaacMenuItem ToggleSets(AacFlIntParameter parameter, int value)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                control.value = value;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Toggle for a Float parameter, setting the value when toggled.
        public MaacMenuItem ToggleSets(AacFlFloatParameter parameter, float value)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                control.value = value;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Button for a Int parameter, setting the value when pressed.
        public MaacMenuItem ButtonSets(AacFlIntParameter parameter, int value)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.type = VRCExpressionsMenu.Control.ControlType.Button;
                control.value = value;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Button for a Float parameter, setting the value when pressed.
        public MaacMenuItem ButtonSets(AacFlFloatParameter parameter, float value)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.type = VRCExpressionsMenu.Control.ControlType.Button;
                control.value = value;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Toggle for a Bool parameter. However, when toggled, the value of the bool will be forced to false. Toggling will never set the value to true.
        public MaacMenuItem ToggleForcesBoolToFalse(AacFlBoolParameter parameter)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
                control.value = 0;
                return control;
            });
            return this;
        }

        /// Set the menu item type as Button for a Bool parameter. However, when pressed, the value of the bool will be forced to false. Pressing will never set the value to true.
        public MaacMenuItem ButtonForcesBoolToFalse(AacFlBoolParameter parameter)
        {
            EditControlWithParameter(parameter, control =>
            {
                control.type = VRCExpressionsMenu.Control.ControlType.Button;
                control.value = 0;
                return control;
            });
            return this;
        }
        
        /// Set the menu item name to be displayed on the menu.
        public MaacMenuItem Name(string menuItemName)
        {
            if (_isDummy) return this;
            
            foreach (var menuItem in menuItems)
            {
                menuItem.name = menuItemName;
                menuItem.gameObject.name = menuItemName;
            }
            return this;
        }

        /// Set the menu item type as Radial for a Float parameter.
        public MaacMenuItem Radial(AacFlFloatParameter floatParam)
        {
            EditControlWithParameter(null, control =>
            {
                control.subParameters = new VRCExpressionsMenu.Control.Parameter[1]
                {
                    new VRCExpressionsMenu.Control.Parameter
                    {
                        name = floatParam.Name
                    }
                };
                control.type = VRCExpressionsMenu.Control.ControlType.RadialPuppet;
                return control;
            });
            return this;
        }

        /// Set the menu item icon.
        public MaacMenuItem WithIcon(Texture2D icon)
        {
            EditControl(control =>
            {
                control.icon = icon;
                return control;
            });
            return this;
        }

        private void EditControlWithParameter(AacFlParameter parameterNullable, Func<VRCExpressionsMenu.Control, VRCExpressionsMenu.Control> func)
        {
            if (_isDummy) return;
            
            foreach (var menuItem in menuItems)
            {
                var control = menuItem.Control;
                var controlParameter = control.parameter ?? new VRCExpressionsMenu.Control.Parameter(); 
                controlParameter.name = parameterNullable != null ? parameterNullable.Name : "";
                control.parameter = controlParameter;
                control = func(control);
                menuItem.Control = control;
            }
        }

        private void EditControl(Func<VRCExpressionsMenu.Control, VRCExpressionsMenu.Control> func)
        {
            if (_isDummy) return;
            
            foreach (var menuItem in menuItems)
            {
                VRCExpressionsMenu.Control control = menuItem.Control;
                control = func(control);
                menuItem.Control = control;
            }
        }
    }

    public class MaacParameter<T>
    {
        private readonly ModularAvatarParameters parameter;
        private readonly int[] indices;
        private readonly bool _isDummy;

        internal MaacParameter(ModularAvatarParameters parameter, int[] indices)
        {
            this.parameter = parameter;
            this.indices = indices;
        }

        public static MaacParameter<T> Dummy()
        {
            return new MaacParameter<T>();
        }

        private MaacParameter()
        {
            _isDummy = true;
        }

        /// Mark this parameter as not synced. By default, newly created parameters are synced.
        public MaacParameter<T> NotSynced()
        {
            if (_isDummy) return this;
            
            EditParameter(config =>
            {
                config.localOnly = true;
                return config;
            });
            return this;
        }

        /// Mark this parameter as not saved. By default, newly created parameters are saved.
        public MaacParameter<T> NotSaved()
        {
            if (_isDummy) return this;

            EditParameter(config =>
            {
                config.saved = false;
                return config;
            });
            return this;
        }

        /// Set the default value of this parameter. No errors are raised if you try to set values outside legal range.
        public MaacParameter<T> WithDefaultValue(T value)
        {
            if (_isDummy) return this;

            var defaultValue = ConvertValue(value);
            
            EditParameter(config =>
            {
                config.defaultValue = defaultValue;
                return config;
            });
            return this;
        }

        private static float ConvertValue(T value)
        {
            return value is float v ? v : value is int i ? (float)i : value is bool b ? b ? 1f : 0f : throw new ArgumentException($"Unable to resolve default value {value} of type {value.GetType()}");
        }

        private void EditParameter(Func<ParameterConfig, ParameterConfig> action)
        {
            var currentParams = parameter.parameters;
            foreach (var i in indices)
            {
                var edit = action(currentParams[i]);
                currentParams[i] = edit;
            }

            parameter.parameters = currentParams;
        }
    }
}
