// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using com.nttqonoq.devices.android.mirzalibrary;
#if !UNITY_EDITOR && UNITY_ANDROID
using Qualcomm.Snapdragon.Spaces;
using UnityEngine.Android;
#endif

namespace Com.Reseul.MiRZAUnityLibrary
{
    [Serializable]
    public class BufferingEvent : UnityEvent<byte[]> { }
    [Serializable]
    public class CompletedEvent : UnityEvent<byte[]> { }

    /// <summary>
    /// Microphone recording controller class for MiRZA devices.
    /// Provides settings for sample rate, channel count, bit depth, and methods for starting/stopping recording and switching microphone modes.
    /// </summary>
    public class GlassMicrophoneRecorder : MonoBehaviour
    {

        private string filePath;
        private bool isRecording;
        private SynchronizationContext mainThread;
        private MirzaPlugin mirzaPlugin;

        [Header("Mode")]
        [SerializeField]
        private bool batchMode = true;

        [Header("Audio Settings")]
        public int SampleRate = 44100;
        public int Channels = 1;
        public int BitDepth = 16;

        [Header("Event Settings")]
        [SerializeField]
        private BufferingEvent onBuffering = new BufferingEvent();
        [SerializeField]
        private CompletedEvent onCompleted = new CompletedEvent();


        private readonly Queue<byte[]> audioDataBuffer = new Queue<byte[]>();

        /// <summary>
        /// Sets the sample rate, channel count, and bit depth, and applies them to the native side.
        /// </summary>
        /// <param name="sampleRate">Sample rate (Hz)</param>
        /// <param name="channels">Number of channels (1: Mono, 2: Stereo)</param>
        /// <param name="bitDepth">Bit depth (16 or 24)</param>
        public void SetAudioConfig(int sampleRate, int channels, int bitDepth)
        {
            this.SampleRate = sampleRate;
            this.Channels = channels;
            this.BitDepth = bitDepth;
            MiRZAMicrophoneControllerProxy.Instance.SetSampleRate(SampleRate);
            MiRZAMicrophoneControllerProxy.Instance.SetChannelConfig(Channels);
            MiRZAMicrophoneControllerProxy.Instance.SetAudioFormat(BitDepth);
        }

        /// <summary>
        /// Initialization process. Checks/requests microphone permission, creates the native controller, and applies Inspector values.
        /// </summary>
        private void Start()
        {
            mainThread = SynchronizationContext.Current;
#if !UNITY_EDITOR && UNITY_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var nativeXRSupportChecker =
                    new AndroidJavaClass("com.qualcomm.snapdragon.spaces.serviceshelper.NativeXRSupportChecker");
                if (nativeXRSupportChecker.CallStatic<bool>("CanShowPermissions") ||
                    FeatureUseCheckUtility.IsFeatureEnabled(FeatureUseCheckUtility.SpacesFeature.Fusion))
                {
                    Permission.RequestUserPermission(Permission.Microphone);
                }
            }
#endif
            MiRZAMicrophoneControllerProxy.Instance.SetSampleRate(SampleRate);
            MiRZAMicrophoneControllerProxy.Instance.SetChannelConfig(Channels);
            MiRZAMicrophoneControllerProxy.Instance.SetAudioFormat(BitDepth);

        }

        private void Update()
        {
            if (isRecording)
            {
                bool isEmpty = MiRZAMicrophoneControllerProxy.Instance.IsEmpty();
                if (!isEmpty)
                {
                    var audioData = MiRZAMicrophoneControllerProxy.Instance.Dequeue();
                    audioDataBuffer.Enqueue(audioData);
                    if (!batchMode)
                    {
                        onBuffering?.Invoke(audioData);
                    }
                }
            }
        }

        /// <summary>
        /// Starts audio recording.
        /// </summary>
        public void StartRecording()
        {
            if (isRecording)
            {
                return;
            }

            isRecording = MiRZAMicrophoneControllerProxy.Instance.StartRecording();
        }

        /// <summary>
        /// Stops audio recording and returns the recorded data (PCM byte array).
        /// </summary>
        public bool StopRecording()
        {
            byte[] audioData = new byte[0];
            if (!isRecording)
            {
                return false;
            }
            MiRZAMicrophoneControllerProxy.Instance.StopRecording();
            mainThread.Post(_ =>
            {
                Debug.Log("GlassMicrophoneRecorder.StopRecording");
            }, null);
            while (audioDataBuffer.Count > 0)
            {
                audioData = audioData.Concat(audioDataBuffer.Dequeue()).ToArray();
            }
            onCompleted?.Invoke(audioData);
           
            isRecording = false;
            return isRecording;
        }

        /// <summary>
        /// Switches the microphone mode. Cannot be changed while recording.
        /// </summary>
        /// <param name="mode">Microphone mode to switch to</param>
        public void SetMicMode(MicMode mode)
        {
            if (isRecording)
            {
                Debug.LogError("GlassMicrophoneRecorder.SetMicMode: Recording is in progress");
                return;
            }
            if (mirzaPlugin == null)
            {
                mirzaPlugin = new MirzaPlugin();
            }
            mirzaPlugin.SwitchMicrophone(mode, mode, 0);
        }
    }
}