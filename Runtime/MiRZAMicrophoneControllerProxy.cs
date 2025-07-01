// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace Com.Reseul.MiRZAUnityLibrary
{
    /// <summary>
    /// Proxy controller class for microphone recording on MiRZA devices.
    /// Provides methods to set sample rate, channel count, bit depth, start/stop recording, and check buffer status.
    /// </summary>
    public class MiRZAMicrophoneControllerProxy
    {
        /// <summary>
        /// Singleton instance of the proxy controller.
        /// </summary>
        public static MiRZAMicrophoneControllerProxy Instance { get; } = new MiRZAMicrophoneControllerProxy();

        /// <summary>
        /// Android Java object for native microphone controller.
        /// </summary>
        private AndroidJavaObject mirzaMicrophoneController;

        /// <summary>
        /// Private constructor to enforce singleton pattern. Initializes the native controller on Android.
        /// </summary>
        private MiRZAMicrophoneControllerProxy()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            mirzaMicrophoneController = new AndroidJavaObject("com.reseul.mirzanativelibrary.MiRZAMicrophoneController");
#endif
        }

        /// <summary>
        /// Sets the sample rate for audio recording.
        /// </summary>
        /// <param name="sampleRate">Sample rate in Hz</param>
        public void SetSampleRate(int sampleRate)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            mirzaMicrophoneController.Call("setSampleRate", sampleRate);
#endif
        }

        /// <summary>
        /// Sets the channel configuration for audio recording.
        /// </summary>
        /// <param name="channels">Number of channels (1: Mono, 2: Stereo)</param>
        public void SetChannelConfig(int channels)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            mirzaMicrophoneController.Call("setChannelConfig", channels);
#endif
        }

        /// <summary>
        /// Sets the audio format (bit depth) for recording.
        /// </summary>
        /// <param name="bitDepth">Bit depth (e.g., 16 or 24)</param>
        public void SetAudioFormat(int bitDepth)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            mirzaMicrophoneController.Call("setAudioFormat", bitDepth);
#endif
        }

        /// <summary>
        /// Dequeues the next available audio buffer from the native side.
        /// </summary>
        /// <returns>PCM audio data as a byte array</returns>
        public byte[] Dequeue()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            using (AndroidJavaObject unityPlayer = mirzaMicrophoneController.Call<AndroidJavaObject>("dequeue"))
            {
                return AndroidJNIHelper.ConvertFromJNIArray<byte[]>(unityPlayer.GetRawObject());
            }
#endif
            return new byte[0];
        }

        /// <summary>
        /// Starts audio recording.
        /// </summary>
        /// <returns>True if recording started successfully, otherwise false</returns>
        public bool StartRecording()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                return mirzaMicrophoneController.Call<bool>("startRecording", unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"));
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Stops audio recording.
        /// </summary>
        /// <returns>True if recording stopped successfully, otherwise false</returns>
        public bool StopRecording()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return mirzaMicrophoneController.Call<bool>("stopRecording");
#else
            return false;
#endif
        }

        /// <summary>
        /// Checks if the audio buffer is empty.
        /// </summary>
        /// <returns>True if buffer is empty, otherwise false</returns>
        public bool IsEmpty()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            return mirzaMicrophoneController.Call<bool>("isEmpty");
#else
            return true;
#endif
        }
    }
}