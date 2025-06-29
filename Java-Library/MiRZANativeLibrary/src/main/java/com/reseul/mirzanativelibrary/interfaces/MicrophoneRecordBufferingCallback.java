// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

package com.reseul.mirzanativelibrary.interfaces;

/**
 * Callback interface for receiving buffered audio data during microphone recording.
 * This callback is invoked each time a chunk of audio data is available from the microphone.
 * Used in both batch and streaming recording modes.
 */
public interface MicrophoneRecordBufferingCallback {
    /**
     * Called when a chunk of audio data is buffered during recording.
     *
     * @param data The buffered audio data (PCM byte array)
     */
    void OnRecordBuffering(byte[] data);
}
