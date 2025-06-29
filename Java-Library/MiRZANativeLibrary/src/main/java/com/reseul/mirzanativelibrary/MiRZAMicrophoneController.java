// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

package com.reseul.mirzanativelibrary;

import android.content.Context;
import android.media.AudioDeviceInfo;
import android.media.AudioFormat;
import android.media.AudioManager;
import android.media.AudioRecord;
import android.media.MediaRecorder;
import android.util.Log;

import androidx.core.app.ActivityCompat;
import android.Manifest;
import android.content.pm.PackageManager;
import com.reseul.mirzanativelibrary.interfaces.MicrophoneRecordBufferingCallback;
import com.reseul.mirzanativelibrary.interfaces.MicrophoneRecordCompletedCallback;
import java.util.ArrayList;
import java.util.Arrays;

/**
 * Microphone controller class for MiRZA devices.
 * Provides methods to start/stop audio recording, set callbacks, and configure audio parameters.
 * Supports both batch and streaming (sequential) recording modes.
 *
 * @author Takahiro Miyaura
 * @version 0.0.2
 */
public class MiRZAMicrophoneController {

    /**
     * Tag name for logging.
     */
    private static final String TAG = "MiRZAMicrophoneController";

    /**
     * Audio recording instance.
     */
    private AudioRecord audioRecord;
    /**
     * Recording status. true: recording, false: idle.
     */
    private boolean isRecording = false;

    /**
     * Thread used for audio recording.
     */
    private Thread recordingThread;

    /**
     * Buffer to store recorded audio data (used in batch mode).
     */
    private ArrayList<byte[]> audioDataBuffer = new ArrayList<byte[]>();
    /**
     * Audio sampling rate (Hz).
     */
    private int sampleRate = 44100;
    /**
     * Audio channel configuration (e.g., mono or stereo).
     */
    private int channelConfig = AudioFormat.CHANNEL_IN_MONO;

    /**
     * Audio format (e.g., PCM 16bit).
     */
    private int audioFormat = AudioFormat.ENCODING_PCM_16BIT;

    /**
     * Callback invoked when audio data is buffered during recording.
     */
    private MicrophoneRecordBufferingCallback bufferingCallback;
    /**
     * Callback invoked when recording is completed.
     */
    private MicrophoneRecordCompletedCallback completeCallback;
    /**
     * Recording mode: true for batch mode, false for streaming mode.
     */
    private boolean isBatchMode = false;

    /**
     * Retrieves the MiRZA microphone device from Android audio devices.
     *
     * @param context Android context
     * @return AudioDeviceInfo for MiRZA microphone, or null if not found
     */
    public static AudioDeviceInfo getGlassMicDevice(Context context) {
        AudioManager audioManager = (AudioManager) context.getSystemService(Context.AUDIO_SERVICE);
        AudioDeviceInfo[] deviceList = audioManager.getDevices(AudioManager.GET_DEVICES_INPUTS);
        for (AudioDeviceInfo device : deviceList) {
            if (device.getType() == AudioDeviceInfo.TYPE_IP) {
                Log.d(TAG, "MiRZA Microphone detected.");
                return device;
            }
        }
        Log.e(TAG, "MiRZA Microphone is not founded.");
        return null;
    }

    /**
     * Registers a callback to be invoked when audio data is buffered during recording.
     *
     * @param callback Callback to be invoked during recording
     */
    public void SetMicrophoneRecordBufferingCallback(MicrophoneRecordBufferingCallback callback) {
        bufferingCallback = callback;
    }

    /**
     * Registers a callback to be invoked when recording is completed.
     *
     * @param callback Callback to be invoked when recording is finished
     */
    public void SetMicrophoneRecordCompletedCallback(MicrophoneRecordCompletedCallback callback) {
        completeCallback = callback;
    }


    /**
     * Sets the sampling rate for audio recording.
     *
     * @param recordSampleRate Sampling rate in Hz
     */
    public void setSampleRate(int recordSampleRate) {
        sampleRate = recordSampleRate;
    }

    /**
     * Sets the channel configuration for audio recording.
     *
     * @param recordChannelConfig Channel configuration (e.g., AudioFormat.CHANNEL_IN_MONO)
     */
    public void setChannelConfig(int recordChannelConfig) {
        channelConfig = recordChannelConfig;
    }

    /**
     * Sets the audio format for recording.
     *
     * @param recordChannelConfig Audio format (e.g., AudioFormat.ENCODING_PCM_16BIT)
     * @see android.media.AudioFormat
     */
    public void setAudioFormat(int recordChannelConfig) {
        audioFormat = recordChannelConfig;
    }

    /**
     * Starts audio recording. If microphone permission is not granted or the MiRZA microphone is not found, recording will not start.
     *
     * You can select the recording mode:
     *   - Batch mode: All audio data is accumulated and returned at the end of recording (higher memory usage).
     *   - Streaming mode: Audio data is delivered in real-time via callback, not accumulated.
     *
     * @param context Android context
     * @param batchMode true for batch mode (accumulate all data), false for streaming mode (process data in real-time)
     */
    public void startRecording(Context context,boolean batchMode) {
        isBatchMode = batchMode;
        if (ActivityCompat.checkSelfPermission(context, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
            Log.e(TAG, "Recording is not permitted.");
            return;
        }

        AudioDeviceInfo glassMicDevice = getGlassMicDevice(context);
        if (glassMicDevice == null) {
            Log.e(TAG, "MiRZA Microphone is not founded.");
            return;
        }

        audioDataBuffer.clear();

        int bufferSize = AudioRecord.getMinBufferSize(sampleRate, channelConfig, audioFormat);

        audioRecord = new AudioRecord.Builder()
                .setAudioSource(MediaRecorder.AudioSource.DEFAULT)
                .setAudioFormat(new AudioFormat.Builder()
                        .setEncoding(audioFormat)
                        .setSampleRate(sampleRate)
                        .setChannelMask(channelConfig)
                        .build())
                .setBufferSizeInBytes(bufferSize)
                .build();

        audioRecord.startRecording();
        isRecording = true;
        recordingThread = new Thread(() -> readAudioData(bufferSize), "AudioRecorder Thread");
        recordingThread.start();
    }

    /**
     * Stops audio recording, releases resources, and returns the recorded data.
     *
     * @return Recorded audio data. In batch mode, returns all accumulated data. In streaming mode, returns only the first chunk.
     */
    public byte[] stopRecording() {
        if (audioRecord != null)
            isRecording = false;
        audioRecord.stop();
        audioRecord.release();
        recordingThread = null;
        int size = 0;
        for (byte[] chunk : audioDataBuffer) {
            size += chunk.length;
        }
        byte[] allData = new byte[size];
        int offset = 0;
        for (byte[] chunk : audioDataBuffer) {
            System.arraycopy(chunk, 0, allData, offset, chunk.length);
            offset += chunk.length;
        }
        if (completeCallback != null)
            completeCallback.OnRecordCompleted(allData);
        Log.d(TAG, "record is stopped");
        return allData;
    }

    /**
     * Reads audio data from the microphone. In both batch and streaming modes, buffered data is delivered via the MicrophoneRecordBufferingCallback.
     * In batch mode, all data is accumulated in memory until recording stops. In streaming mode, data is not accumulated.
     *
     * @param bufferSize Buffer size for reading audio data
     */
    private void readAudioData(int bufferSize) {
        byte[] buffer = new byte[bufferSize];
        while (isRecording) {
            int read = audioRecord.read(buffer, 0, buffer.length);
            if (read > 0) {
                byte[] data = Arrays.copyOf(buffer, read);
                if(isBatchMode)
                    audioDataBuffer.add(data);
                if (bufferingCallback != null) {
                    bufferingCallback.OnRecordBuffering(data);
                }
            }
        }
    }
}

