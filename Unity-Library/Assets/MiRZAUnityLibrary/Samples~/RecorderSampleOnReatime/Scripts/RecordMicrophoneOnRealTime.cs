// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using Com.Reseul.MiRZAUnityLibrary;

/// <summary>
/// Real-time microphone recording and playback controller.
/// Handles audio buffer management, recording state, and UI updates.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RecordMicrophoneOnRealTime : MonoBehaviour
{
    /// <summary>
    /// Reference to the GlassMicrophoneRecorder component for native recording.
    /// </summary>
    [SerializeField] private GlassMicrophoneRecorder microphoneRecorder;

    /// <summary>
    /// Reference to the UI button text for recording state.
    /// </summary>
    [SerializeField] private TextMeshProUGUI buttonText;

    /// <summary>
    /// Audio source used as input for testing or playback.
    /// </summary>
    [SerializeField] private AudioSource inputSource;

    /// <summary>
    /// Indicates whether recording is currently active.
    /// </summary>
    private bool isRecording = false;

    /// <summary>
    /// The output AudioSource component attached to this GameObject.
    /// </summary>
    private AudioSource audioSource;

    /// <summary>
    /// Main thread context for posting Unity actions.
    /// </summary>
    private SynchronizationContext MainThread;

    /// <summary>
    /// Buffer for storing float audio samples for playback.
    /// </summary>
    private readonly Queue<float> audioBuffer = new Queue<float>();

    /// <summary>
    /// Buffer for storing raw byte audio data (not used in this sample).
    /// </summary>
    private readonly Queue<byte[]> audioSourceBuffer = new Queue<byte[]>();

    #region Unity Standard Methods
    /// <summary>
    /// Unity Start method. Initializes audio source and main thread context.
    /// </summary>
    void Start()
    {
        MainThread = SynchronizationContext.Current;
        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Unity Update method. Handles space key input for testing (enqueues inputSource samples).
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Input.GetKeyDown(KeyCode.Space)");
            float[] samples = new float[inputSource.clip.samples * inputSource.clip.channels];
            inputSource.clip.GetData(samples, 0);
            for (int i = 0; i < samples.Length; i++)
            {
                lock (audioBuffer)
                {
                    audioBuffer.Enqueue(samples[i]);
                }
            }
        }
    }

    /// <summary>
    /// Unity audio filter callback. Feeds audio data from buffer to output.
    /// </summary>
    /// <param name="data">Audio data buffer</param>
    /// <param name="audioChannels">Number of audio channels</param>
    private void OnAudioFilterRead(float[] data, int audioChannels)
    {
        if (audioBuffer == null || audioBuffer.Count == 0) return;

        for (int i = 0; i < data.Length; i += audioChannels)
        {
            float sample = 0;
            for (int sc = 0; sc < microphoneRecorder.Channels; sc++)
            {
                if (audioBuffer.Count == 0) return;
                lock (audioBuffer)
                {
                    sample = audioBuffer.Dequeue();
                }
                data[i + sc] = sample;
            }
            for (int c = microphoneRecorder.Channels; c < audioChannels; c++)
            {
                data[i + c] = sample;
            }
        }
    }
    #endregion

    #region Event Methods
    /// <summary>
    /// Called when the XR Simple Interactable's SelectEntered event is triggered.
    /// Starts or stops recording and updates the button text.
    /// </summary>
    public void OnSelectEntered()
    {
        if (isRecording)
        {
            buttonText.text = "Start Recording";
            microphoneRecorder.StopRecording();
            isRecording = false;
        }
        else
        {
            buttonText.text = "Stop Recording";
            microphoneRecorder.StartRecording();
            isRecording = true;
        }
    }

    /// <summary>
    /// Called when audio data is buffered during recording.
    /// Converts PCM byte buffer to float samples and enqueues them.
    /// </summary>
    /// <param name="buffer">PCM audio buffer</param>
    public void OnRecordBuffering(byte[] buffer)
    {
        if (isRecording)
        {
            int bitDepth = microphoneRecorder.BitDepth;
            int sampleCount = buffer.Length / (bitDepth / 8);
            for (int i = 0; i < sampleCount; i++)
            {
                int sampleIndex = i * (bitDepth / 8);
                float data = 0f;
                int value = 0;
                if (bitDepth == 16)
                {
                    value = BitConverter.ToInt16(buffer, sampleIndex);
                    data = value / 32768f;
                }
                else if (bitDepth == 24)
                {
                    value = (buffer[sampleIndex + 2] << 16) | (buffer[sampleIndex + 1] << 8) | buffer[sampleIndex];
                    if ((value & 0x800000) != 0) value |= unchecked((int)0xFF000000); // Sign extension for 24-bit
                    data = value / 8388608f;
                }
                lock (audioBuffer)
                {
                    audioBuffer.Enqueue(data);
                }
            }
        }
    }

    /// <summary>
    /// Called when recording is completed.
    /// Converts the final PCM byte buffer to float samples and enqueues them.
    /// </summary>
    /// <param name="buffer">PCM audio buffer</param>
    public void OnRecordCompleted(byte[] buffer)
    {
        int bitDepth = microphoneRecorder.BitDepth;
        int sampleCount = buffer.Length / (bitDepth / 8);
        for (int i = 0; i < sampleCount; i++)
        {
            int sampleIndex = i * (bitDepth / 8);
            float data = 0f;
            int value = 0;
            if (bitDepth == 16)
            {
                value = BitConverter.ToInt16(buffer, sampleIndex);
                data = value / 32768f;
            }
            else if (bitDepth == 24)
            {
                value = (buffer[sampleIndex + 2] << 16) | (buffer[sampleIndex + 1] << 8) | buffer[sampleIndex];
                if ((value & 0x800000) != 0) value |= unchecked((int)0xFF000000); // Sign extension for 24-bit
                data = value / 8388608f;
            }
            lock (audioBuffer)
            {
                audioBuffer.Enqueue(data);
            }
        }
    }
    #endregion
}
