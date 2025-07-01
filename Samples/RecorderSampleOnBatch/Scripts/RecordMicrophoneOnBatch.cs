// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

using System;
using System.IO;
using System.Threading;
using UnityEngine;
using TMPro;
using Com.Reseul.MiRZAUnityLibrary;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Batch microphone recording controller.
/// Handles recording, saving, and playback of audio as WAV files.
/// </summary>
public class RecordMicrophone : MonoBehaviour
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
    /// Indicates whether recording is currently active.
    /// </summary>
    private bool isRecording = false;

    /// <summary>
    /// Stores the file path of the last saved WAV file.
    /// </summary>
    private string lastSavedFilePath = null;

    /// <summary>
    /// The output AudioSource component attached to this GameObject.
    /// </summary>
    private AudioSource audioSource;

    /// <summary>
    /// Audio source used as input for debug save/playback.
    /// </summary>
    [SerializeField] private AudioSource inputAudioSource;

    /// <summary>
    /// Main thread context for posting Unity actions.
    /// </summary>
    private SynchronizationContext MainThread;

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
    /// Unity Update method. (Currently unused, placeholder for future logic.)
    /// </summary>
    void Update()
    {
        // (Optional: update logic if needed)
    }
    #endregion

    #region Event Methods
    /// <summary>
    /// Called when recording is completed. Saves the audio data as a WAV file and plays it back.
    /// </summary>
    /// <param name="audioData">PCM audio data</param>
    public void OnCompleted(byte[] audioData)
    {
        string filePath = SaveWavFile(audioData, microphoneRecorder.SampleRate, microphoneRecorder.Channels, microphoneRecorder.BitDepth);
        lastSavedFilePath = filePath;
        PlayLastSavedAudio();
    }

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
            DeleteOldFiles();
            microphoneRecorder.StartRecording();
            isRecording = true;
        }
    }

    /// <summary>
    /// Debug: Save the input audio source as a WAV file and play it back.
    /// </summary>
    /// <param name="args">XR interaction event arguments</param>
    public void OnSelectDebug(SelectEnterEventArgs args)
    {
        SaveInputSourceAsWav();
        PlayLastSavedAudio();
    }

    /// <summary>
    /// Plays the last saved WAV audio file using the attached AudioSource.
    /// </summary>
    public void PlayLastSavedAudio()
    {
        if (string.IsNullOrEmpty(lastSavedFilePath) || !File.Exists(lastSavedFilePath))
        {
            Debug.LogWarning("No audio file to play.");
            return;
        }
        byte[] wavData = File.ReadAllBytes(lastSavedFilePath);
        AudioClip clip = WavToAudioClip(wavData, microphoneRecorder.SampleRate, microphoneRecorder.Channels);
        if (clip != null && audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Deletes all old WAV files in the temporary cache directory.
    /// </summary>
    private void DeleteOldFiles()
    {
        string dir = Application.temporaryCachePath;
        var files = Directory.GetFiles(dir, "*.wav");
        foreach (var file in files)
        {
            try { File.Delete(file); } catch { }
        }
    }

    /// <summary>
    /// Saves PCM audio data as a WAV file with the specified format.
    /// </summary>
    /// <param name="pcmData">PCM audio data</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="bitDepth">Bit depth (16 or 24)</param>
    /// <returns>File path of the saved WAV file</returns>
    private string SaveWavFile(byte[] pcmData, int sampleRate, int channels, int bitDepth)
    {
        string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
        string filePath = Path.Combine(Application.temporaryCachePath, fileName);
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        using (var bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * channels * (bitDepth / 8);
            int blockAlign = channels * (bitDepth / 8);
            int subChunk2Size = pcmData.Length;
            int chunkSize = 36 + subChunk2Size;
            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(chunkSize);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            // fmt chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // Subchunk1 size
            bw.Write((short)1); // PCM format
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitDepth);
            // data chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subChunk2Size);
            bw.Write(pcmData);
        }
        Debug.Log($"Saved WAV: {filePath},{pcmData.Length}");
        return filePath;
    }

    /// <summary>
    /// Converts a WAV byte array to an AudioClip for playback.
    /// </summary>
    /// <param name="wavData">WAV file data</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <returns>AudioClip created from the WAV data</returns>
    private AudioClip WavToAudioClip(byte[] wavData, int sampleRate, int channels)
    {
        // Skip 44-byte WAV header and convert PCM data to float array
        int headerSize = 44;
        int bitDepth = microphoneRecorder.BitDepth;
        int sampleCount = (wavData.Length - headerSize) / (bitDepth / 8);
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            int sampleIndex = headerSize + i * (bitDepth / 8);
            int value = 0;
            if (bitDepth == 16)
            {
                value = BitConverter.ToInt16(wavData, sampleIndex);
                samples[i] = value / 32768f;
            }
            else if (bitDepth == 24)
            {
                value = (wavData[sampleIndex + 2] << 16) | (wavData[sampleIndex + 1] << 8) | wavData[sampleIndex];
                if ((value & 0x800000) != 0) value |= unchecked((int)0xFF000000); // Sign extension for 24-bit
                samples[i] = value / 8388608f;
            }
        }
        AudioClip clip = AudioClip.Create("RecordedAudio", sampleCount / channels, channels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// Save the given AudioClip as a WAV file using the same rules as microphone recording (for debug).
    /// </summary>
    /// <param name="inputSource">AudioClip to save (if null, uses inputAudioSource.clip)</param>
    public void SaveInputSourceAsWav(AudioClip inputSource = null)
    {
        if (inputSource == null)
        {
            if (inputAudioSource == null || inputAudioSource.clip == null)
            {
                Debug.LogWarning("InputSource is null and inputAudioSource/clip is not set.");
                return;
            }
            inputSource = inputAudioSource.clip;
        }
        DeleteOldFiles();
        float[] samples = new float[inputSource.samples * inputSource.channels];
        inputSource.GetData(samples, 0);
        byte[] pcmData = null;
        int bitDepth = microphoneRecorder.BitDepth;
        if (bitDepth == 16)
        {
            pcmData = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)Mathf.Clamp(samples[i] * 32767f, short.MinValue, short.MaxValue);
                byte[] bytes = BitConverter.GetBytes(val);
                pcmData[i * 2] = bytes[0];
                pcmData[i * 2 + 1] = bytes[1];
            }
        }
        else if (bitDepth == 24)
        {
            pcmData = new byte[samples.Length * 3];
            for (int i = 0; i < samples.Length; i++)
            {
                int val = (int)Mathf.Clamp(samples[i] * 8388607f, -8388608f, 8388607f);
                pcmData[i * 3] = (byte)(val & 0xFF);
                pcmData[i * 3 + 1] = (byte)((val >> 8) & 0xFF);
                pcmData[i * 3 + 2] = (byte)((val >> 16) & 0xFF);
            }
        }
        else
        {
            Debug.LogError("Unsupported bit depth for debug save: " + bitDepth);
            return;
        }
        string filePath = SaveWavFile(pcmData, inputSource.frequency, inputSource.channels, bitDepth);
        lastSavedFilePath = filePath;
        Debug.Log($"Saved debug input source as WAV: {filePath}");
    }
    #endregion
}
