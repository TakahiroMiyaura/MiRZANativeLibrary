// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

package com.reseul.mirzanativelibrary.interfaces;

/**
 * Callback interface for receiving the final audio data when microphone recording is completed.
 * This callback is invoked once when recording stops, delivering all accumulated audio data.
 * In batch mode, the entire recorded data is provided. In streaming mode, null is returned.
 */
public interface MicrophoneRecordCompletedCallback {
    /**
     * Called when microphone recording is completed.
     *
     * @param data The final recorded audio data (PCM byte array)
     */
    void OnRecordCompleted(byte[] data);
}
