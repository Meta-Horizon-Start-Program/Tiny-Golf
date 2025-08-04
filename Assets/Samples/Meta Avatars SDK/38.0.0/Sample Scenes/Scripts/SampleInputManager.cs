/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

#if USING_XR_MANAGEMENT && (USING_XR_SDK_OCULUS || USING_XR_SDK_OPENXR) && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

#if UNITY_EDITOR && USING_XR_SDK_OPENXR && USING_XR_SDK
#define UNITY_EDITOR_OPENXR
#endif

#if UNITY_EDITOR && USING_XR_SDK
#define UNITY_EDITOR_XR_SDK
#endif

using System.Collections.Generic;
using System.Reflection;
using Oculus.Avatar2;
using UnityEngine;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
#endif

using Node = UnityEngine.XR.XRNode;


public abstract class BaseHeadsetInputSimulator : MonoBehaviour
{
    protected OvrAvatarInputTrackingDelegate? _inputTrackingDelegate;
    public virtual OvrAvatarInputTrackingDelegate? InputTrackingDelegate => _inputTrackingDelegate;
}

/* This is an example class for how to send input and IK transforms to the sdk from any source
 * InputTrackingDelegate and InputControlDelegate are set on BodyTracking.
 */
public class SampleInputManager : OvrAvatarInputManager
{
    private const string logScope = "sampleInput";

    [SerializeField]
    [Tooltip("Optional. If added, it will use input directly from OVRCameraRig instead of doing its own calculations.")]
#if USING_XR_SDK
    private OVRCameraRig? _ovrCameraRig = null;
#endif
    private bool _useOvrCameraRig;

    // Only used in editor, produces warnings when packaging
#pragma warning disable CS0414 // is assigned but its value is never used
    [SerializeField]
    private bool _debugDrawTrackingLocations = false;
#pragma warning restore CS0414 // is assigned but its value is never used

#if UNITY_EDITOR
    [SerializeField]
    public BaseHeadsetInputSimulator? HeadsetInputSimulator;
#if USING_XR_SDK
    [SerializeField]
    [Tooltip("Optional. If set to true, when played in Editor, all instances of SampleSceneLocomotion are disabled, " +
             "so that the movements don't override or interact in unwanted ways if applied to the same avatar.")]
    private bool disableSampleSceneLocomotion = false;

    private OvrAvatarInputTrackingProviderBase? _simulatedTrackingProvider;
    private OvrAvatarInputTrackingProviderBase? _deviceDrivenTrackingProvider;
#endif
#endif

    public OvrAvatarBodyTrackingMode BodyTrackingMode
    {
        get => _bodyTrackingMode;
        set
        {
            _bodyTrackingMode = value;
            InitializeBodyTracking();
        }
    }

    protected void Awake()
    {
#if USING_XR_SDK
        _useOvrCameraRig = _ovrCameraRig != null;
#endif

        // Debug Drawing
#if UNITY_EDITOR
        SceneView.duringSceneGui += OnSceneGUI;
#endif
    }

#if USING_XR_SDK
    private void Start()
    {
        // If OVRCameraRig doesn't exist, we should set tracking origin ourselves
        if (!_useOvrCameraRig)
        {

            if (OVRManager.instance == null)
            {
                OvrAvatarLog.LogDebug("Creating OVRManager, as one doesn't exist yet.", logScope, this);
                var go = new GameObject("OVRManager");
                var manager = go.AddComponent<OVRManager>();
                manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }
            else
            {
                OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }

            OvrAvatarLog.LogInfo("Setting Tracking Origin to FloorLevel", logScope, this);

            var instances = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(instances);
            foreach (var instance in instances)
            {
                instance.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            }
        }
    }
#endif

#if UNITY_EDITOR_OPENXR
    private void Update()
    {
        if (!_trackingInitialized)
        {
            return;
        }

        // Use simulated tracking provider when headset data is not available
        if (!OvrAvatarUtility.IsHeadsetActive() && _inputTrackingProvider != _simulatedTrackingProvider)
        {
            if (disableSampleSceneLocomotion)
            {
                SetEnableSampleSceneLocomotion(false);
            }

            if (_simulatedTrackingProvider == null)
            {
                InitializeSimulatedTrackingProvider();
            }

            _inputTrackingProvider = _simulatedTrackingProvider;
            OnBodyTrackingContextContextChanged?.Invoke(this);
        }

        // Use device tracking provider when headset is detected (Quest link)
        if (OvrAvatarUtility.IsHeadsetActive() && _inputTrackingProvider != _deviceDrivenTrackingProvider)
        {
            SetEnableSampleSceneLocomotion(true);
            if (_deviceDrivenTrackingProvider == null)
            {
                InitializeDeviceTrackingProvider();
            }

            _inputTrackingProvider = _deviceDrivenTrackingProvider;
            OnBodyTrackingContextContextChanged?.Invoke(this);
        }
    }
#endif

    protected override void OnTrackingInitialized()
    {
#if USING_XR_SDK
        // On Oculus SDK version >= v46 Eye tracking and Face tracking need to be explicitly started by the application
        // after permission has been requested.
        OvrPluginInvoke("StartFaceTracking");
        OvrPluginInvoke("StartEyeTracking");
#endif

#if USING_XR_SDK
#if UNITY_EDITOR
        // Use SampleAvatarHeadsetInputSimulator if no headset connected via link
        if (!OvrAvatarUtility.IsHeadsetActive())
        {
            if (disableSampleSceneLocomotion)
            {
                SetEnableSampleSceneLocomotion(false);
            }

            InitializeSimulatedTrackingProvider();
            _inputTrackingProvider = _simulatedTrackingProvider;
        }
        else
        {
            InitializeDeviceTrackingProvider();
            _inputTrackingProvider = _deviceDrivenTrackingProvider;
        }
#else // !UNITY_EDITOR
        var inputTrackingDelegate = new SampleInputTrackingDelegate(_ovrCameraRig);
        _inputTrackingProvider = new OvrAvatarInputTrackingDelegatedProvider(inputTrackingDelegate);
#endif // !UNITY_EDITOR
#endif // USING_XR_SDK
        var inputControlDelegate = new SampleInputControlDelegate();
        _inputControlProvider = new OvrAvatarInputControlDelegatedProvider(inputControlDelegate);
    }

#if UNITY_EDITOR_XR_SDK
    private void InitializeSimulatedTrackingProvider()
    {
        IOvrAvatarInputTrackingDelegate? inputTrackingDelegate = null;
        if (HeadsetInputSimulator is not null)
        {
            inputTrackingDelegate = HeadsetInputSimulator.InputTrackingDelegate;
        }
        else
        {
            inputTrackingDelegate = new SampleAvatarHeadsetInputSimulator();
        }

        _simulatedTrackingProvider = new OvrAvatarInputTrackingDelegatedProvider(inputTrackingDelegate);
    }

    private void InitializeDeviceTrackingProvider()
    {
        var inputTrackingDelegate = new SampleInputTrackingDelegate(_ovrCameraRig);
        _deviceDrivenTrackingProvider = new OvrAvatarInputTrackingDelegatedProvider(inputTrackingDelegate);
    }

    private void SetEnableSampleSceneLocomotion(bool doEnable)
    {
        var locomotionScripts = FindObjectsOfType<SampleSceneLocomotion>();
        if (locomotionScripts != null)
        {
            foreach (var locomotionScript in locomotionScripts)
            {
                locomotionScript.enabled = doEnable;
                var enable = doEnable ? "enabled" : "disabled";
                OvrAvatarLog.LogWarning($"Found and {enable} SampleSceneLocomotion on object: {locomotionScript.gameObject.name}.", logScope);
            }
        }
    }
#endif


#if USING_XR_SDK
    // We use reflection here so that there are not compiler errors when using Oculus SDK v45 or below.
    private static void OvrPluginInvoke(string method, params object[] args)
    {
        typeof(OVRPlugin).GetMethod(method, BindingFlags.Public | BindingFlags.Static)?.Invoke(null, args);
    }
#endif

    protected override void OnDestroyCalled()
    {
#if UNITY_EDITOR
        SceneView.duringSceneGui -= OnSceneGUI;
#endif

        base.OnDestroyCalled();
    }

#if UNITY_EDITOR
    #region Debug Drawing

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_debugDrawTrackingLocations)
        {
            DrawTrackingLocations();
        }
    }

    private void DrawTrackingLocations()
    {
        if (InputTrackingProvider == null)
        {
            return;
        }

        var inputTrackingState = InputTrackingProvider.State;

        float radius = 0.2f;
        Quaternion orientation;
        float outerRadius() => radius + 0.25f;
        Vector3 forward() => orientation * Vector3.forward;

        Handles.color = Color.blue;
        Handles.RadiusHandle(Quaternion.identity, inputTrackingState.headset.position, radius);

        orientation = inputTrackingState.headset.orientation;
        Handles.DrawLine((Vector3)inputTrackingState.headset.position + forward() * radius,
            (Vector3)inputTrackingState.headset.position + forward() * outerRadius());

        radius = 0.1f;
        Handles.color = Color.yellow;
        Handles.RadiusHandle(Quaternion.identity, inputTrackingState.leftController.position, radius);

        orientation = inputTrackingState.leftController.orientation;
        Handles.DrawLine((Vector3)inputTrackingState.leftController.position + forward() * radius,
            (Vector3)inputTrackingState.leftController.position + forward() * outerRadius());

        Handles.color = Color.yellow;
        Handles.RadiusHandle(Quaternion.identity, inputTrackingState.rightController.position, radius);

        orientation = inputTrackingState.rightController.orientation;
        Handles.DrawLine((Vector3)inputTrackingState.rightController.position + forward() * radius,
            (Vector3)inputTrackingState.rightController.position + forward() * outerRadius());
    }

    #endregion
#endif // UNITY_EDITOR
}
