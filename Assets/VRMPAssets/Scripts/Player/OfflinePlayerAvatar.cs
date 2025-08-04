using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents the offline player avatar.
    /// </summary>
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

        /// <summary>
        /// Gets or sets a value indicating whether the player is muted.
        /// </summary>
        public static bool muted
        {
            get => s_Muted;
            set
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    s_Muted = value;
            }
        }

        /// <summary>
        /// A value indicating whether the player is muted.
        /// </summary>
        static bool s_Muted;

        /// <summary>
        /// The head transform.
        /// </summary>
        [SerializeField] Transform m_HeadTransform;

        /// <summary>
        /// The head renderer.
        /// </summary>
        [SerializeField] SkinnedMeshRenderer m_HeadRend;

        /// <summary>
        /// The voice amplitude curve.
        /// </summary>
        [SerializeField] AnimationCurve m_VoiceCurve;

        /// <summary>
        /// The head origin.
        /// </summary>
        Transform m_HeadOrigin;

        /// <summary>
        /// The mouth blend smoothing.
        /// </summary>
        [SerializeField] float m_MouthBlendSmoothing = 5.0f;

        /// <summary>
        /// The microphone loudness.
        /// </summary>
        float m_MicLoudness;

        /// <summary>
        /// The microphone device name.
        /// </summary>
        string m_Device;

        /// <summary>
        /// The sample window.
        /// </summary>
        int m_SampleWindow = 128;

        /// <summary>
        /// The clip record.
        /// </summary>
        AudioClip m_ClipRecord;

        /// <summary>
        /// The voice destination volume.
        /// </summary>
        float m_VoiceDestinationVolume;

        bool m_MicInitialized = false;

        /// <inheritdoc/>
        void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            m_HeadOrigin = rig.Camera.transform;

        }

        void OnEnable()
        {
            XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
            XRINetworkGameManager.Connected.Subscribe(connected =>
            {
                gameObject.SetActive(!connected);
            });
        }

        void OnDisable()
        {
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
            StopMicrophone();
            XRINetworkGameManager.Connected.Unsubscribe(connected =>
            {
                gameObject.SetActive(!connected);
            });
        }

        /// <inheritdoc/>
        private void LateUpdate()
        {
            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        /// <inheritdoc/>
        void Update()
        {
            if (!s_Muted)
            {
                m_MicLoudness = LevelMax();

                m_VoiceDestinationVolume = Mathf.Clamp01(Mathf.Lerp(m_VoiceDestinationVolume, m_MicLoudness, Time.deltaTime * m_MouthBlendSmoothing));

                float appliedCurve = m_VoiceCurve.Evaluate(m_VoiceDestinationVolume);
                voiceAmp.Value = appliedCurve;
                m_HeadRend.SetBlendShapeWeight(0, 100 - appliedCurve * 100);
            }
            else
            {
                voiceAmp.Value = 0.0f;
            }
        }

        void MicrophonePermissionGranted(bool granted)
        {
            if (granted)
            {
                InitMic();
            }
        }

        void UpdatePlayerColor(Color color)
        {
            m_HeadRend.materials[2].color = color;
        }

        /// <summary>
        /// Initializes the microphone, called from <see cref="VoiceChatManager.s_HasMicrophonePermission" callback/>.
        /// </summary>
        void InitMic()
        {
            m_MicInitialized = true;
            m_Device ??= Microphone.devices[0];
            m_ClipRecord = Microphone.Start(m_Device, true, 999, 44100);
        }

        /// <summary>
        /// Stops the microphone.
        /// </summary>
        void StopMicrophone()
        {
            m_MicInitialized = false;
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Microphone.End(m_Device);
            }
            else
            {
                s_Muted = true;
            }
        }

        /// <summary>
        /// Gets the maximum level of the microphone input.
        /// </summary>
        /// <returns>The maximum level of the microphone input.</returns>
        float LevelMax()
        {
            if (!m_MicInitialized) return 0;
            float levelMax = 0;
            float[] waveData = new float[m_SampleWindow];
            int micPosition = Microphone.GetPosition(null) - (m_SampleWindow + 1); // null means the first microphone
            if (micPosition < 0) return 0;
            m_ClipRecord.GetData(waveData, micPosition);
            // Getting a peak on the last 128 samples
            for (int i = 0; i < m_SampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }
            return levelMax;
        }
    }
}
