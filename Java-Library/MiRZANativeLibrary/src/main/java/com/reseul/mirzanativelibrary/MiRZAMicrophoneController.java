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
import java.util.concurrent.ConcurrentLinkedQueue;
import java.util.Arrays;

/**
 * Microphone controller class for MiRZA devices.
 * Provides methods to start/stop audio recording, set callbacks, and configure audio parameters.
 * Supports both batch and streaming recording modes.
 *
 * @author Takahiro Miyaura
 * @version 0.0.3
 */
public class MiRZAMicrophoneController {

    /** Tag for logging */
    private static final String TAG = "MiRZAMicrophoneController";

    /** AudioRecord instance */
    private AudioRecord audioRecord;
    /** Recording flag */
    private boolean isRecording = false;
    /** Thread for recording */
    private Thread recordingThread;
    /** Buffer for batch mode */
    private final ConcurrentLinkedQueue<byte[]> audioDataBuffer = new ConcurrentLinkedQueue<>();
    private int audioDataBufferSize = 0;
    /** Sampling rate (Hz) */
    private int sampleRate = 44100;
    /** Channel configuration */
    private int channelConfig = AudioFormat.CHANNEL_IN_MONO;
    /** Audio format */
    private int audioFormat = AudioFormat.ENCODING_PCM_16BIT;

    /**
     * Get the MiRZA microphone device.
     * @param context Android context
     * @return AudioDeviceInfo for MiRZA microphone, or null if not found.
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
     * Set the sampling rate.
     * @param recordSampleRate Sampling rate (Hz)
     */
    public void setSampleRate(int recordSampleRate) {
        sampleRate = recordSampleRate;
    }

    /**
     * Set the channel configuration.
     * @param recordChannelConfig 1: Mono, 2: Stereo
     */
    public void setChannelConfig(int recordChannelConfig) {
        if (recordChannelConfig == 1)
            channelConfig = AudioFormat.CHANNEL_IN_MONO;
        else if (recordChannelConfig == 2)
            channelConfig = AudioFormat.CHANNEL_IN_STEREO;
        else
            channelConfig = recordChannelConfig;
    }

    /**
     * Set the audio format.
     * @param recordAudioFormat 16: PCM16bit, 24: PCM24bit
     */
    public void setAudioFormat(int recordAudioFormat) {
        if (recordAudioFormat == 16)
            audioFormat = AudioFormat.ENCODING_PCM_16BIT;
        else if (recordAudioFormat == 24)
            audioFormat = AudioFormat.ENCODING_PCM_24BIT_PACKED;
        else
            audioFormat = recordAudioFormat;
    }

    /**
     * Start recording. If permission or device is missing, recording will not start.
     * Batch: All data is accumulated in the buffer. Streaming: Data is delivered sequentially via callback.
     * @param context Android context
     * @return true if successful
     */
    public boolean startRecording(Context context) {
        try {
            if (ActivityCompat.checkSelfPermission(context, Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
                Log.e(TAG, "Recording is not permitted.");
                return false;
            }
            AudioDeviceInfo glassMicDevice = getGlassMicDevice(context);
            if (glassMicDevice == null) {
                Log.e(TAG, "MiRZA Microphone is not founded.");
                return false;
            }
            audioDataBuffer.clear();
            audioDataBufferSize = 0;
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
            recordingThread = new Thread(new Runnable() {
                @Override
                public void run() {
                    while (isRecording) {
                        byte[] buffer = new byte[bufferSize / 2];
                        int read = audioRecord.read(buffer, 0, buffer.length);
                        if (read > 0) {
                            byte[] data = Arrays.copyOf(buffer, read);
                            audioDataBufferSize += read;
                            audioDataBuffer.add(data);
                        }
                    }
                }
            }, "Recording Thread");
            recordingThread.start();
        } catch (Exception e) {
            return false;
        }
        return true;
    }

    /**
     * Check if the buffer is empty.
     * @return true if empty
     */
    public boolean isEmpty() {
        return audioDataBuffer.isEmpty();
    }

    /**
     * Dequeue one chunk of data from the buffer.
     * @return audio data
     */
    public byte[] dequeue() {
        if (!audioDataBuffer.isEmpty()) {
            return audioDataBuffer.poll();
        }
        return new byte[0];
    }

    /**
     * Stop recording and release resources.
     * @return true if successful
     */
    public boolean stopRecording() {
        if (audioRecord == null) return false;
        try {
            isRecording = false;
            audioRecord.stop();
            audioRecord.release();
            recordingThread = null;
            Log.d(TAG, "record is stopped");
        } catch (Exception e) {
            return false;
        }
        return true;
    }
}





