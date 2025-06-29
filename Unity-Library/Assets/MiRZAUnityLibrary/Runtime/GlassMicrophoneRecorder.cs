// Copyright (c) 2025 Takahiro Miyaura
// Released under the Boost Software License 1.0
// https://opensource.org/license/bsl-1-0

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Android;
using Qualcomm.Snapdragon.Spaces;
using System.Collections.ObjectModel;
using System.Threading;
using com.nttqonoq.devices.android.mirzalibrary;

namespace Com.Reseul.MiRZAUnityLibrary{

public class GlassMicrophoneRecorder : MonoBehaviour
{

    private AndroidJavaObject MiRZAMicrophoneController;
    private string filePath;
    private bool isRecording;
    private SynchronizationContext MainThread;
    private MirzaPlugin mirzaPlugin;

    private void Start()
    {
    
        MainThread = SynchronizationContext.Current;
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
        
        MiRZAMicrophoneController = new AndroidJavaObject("com.reseul.mirzanativelibrary.MiRZAMicrophoneController");
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            return;
        }

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            MiRZAMicrophoneController.Call("startRecording",unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"));
            isRecording = true;
            MainThread.Post(__ => {
                Debug.Log("GlassMicrophoneRecorder.StartRecording");
            },null);
        }
    }

    public byte[] StopRecording()
    {
        byte[] audioData = new byte[0];
        if (!isRecording)
        {
            return new byte[0];
        }

        isRecording = false;

        if (MiRZAMicrophoneController != null)
        {
            audioData = MiRZAMicrophoneController.Call<byte[]>("stopRecording");
            MainThread.Post(__ => {
                Debug.Log("GlassMicrophoneRecorder.StopRecording");
            },null);
        }
        return audioData;
    }

    public void SetMicMode(MicMode mode){
        if(isRecording){
            Debug.LogError("GlassMicrophoneRecorder.SetMicMode: Recording is in progress");
            return;
        }
        if(mirzaPlugin == null){
            mirzaPlugin = new MirzaPlugin();
        }
        mirzaPlugin.SwitchMicrophone(mode,mode,0);
    }

}
}