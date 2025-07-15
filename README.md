# MiRZANativeLibrary
This project is a Java library that adapts MiRZALibrary for Unity.

|Support MiRZA Library Version|
|:-:|
|v1.1.5|

## Import Unity Package

To use this package, follow these steps.
1. Create a Unity project.
2. Import the MiRZA Library.
3. Import the Snapdragon Spaces SDK.
- Not required if you are not using the sample.
4. Import this library.
- Open Package Manager, press + in the upper left corner, and enter the following URL in ‘Import Package from git URL’.
  ```
  https://github.com/TakahiroMiyaura/MiRZANativeLibrary.git#upm
  ```
5. Create Empty Game Object,and add Component 'Glass Microphone Controller'

### How To Use
The 'Glass Microphone Controller' component can change the data acquisition method by changing the batch mode.

- Batch Mode:
    - true : Get audio data all at once when recording is completed
    - false : Stream audio data periodically from the start of recording.
- Events
    - OnBuffering - An event that occurs at regular intervals to retrieve the difference in audio data since the previous call.
    - OnComplete - An event that occurs when recording is complete to save the recorded data in bulk.


**Start Recording**
```csharp
 void Start()
 {
    var microphoneRecorder = GetComponent<GlassMicrophoneRecorder>();
    microphoneRecorder.StartRecording();
 }
```

**Stop Recording**
```csharp
public void OnClicked()
{
    microphoneRecorder.StopRecording();
}
```

**implementation example - On Buffering Event**
```csharp
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
```
**implementation example - On Completed Event**
```csharp
 public void OnCompleted(byte[] audioData)
 {
     string filePath = SaveWavFile(audioData, microphoneRecorder.SampleRate, microphoneRecorder.Channels, microphoneRecorder.BitDepth);
     lastSavedFilePath = filePath;
     PlayLastSavedAudio();
 }
```
## About build this project

### Android Java project
1. get required libraries:
    * classes.jar(From Unity Editor. Path:<Path-to-Unity-Hub-insstalls-location>\<UNITY-VERSION>\Editor\Data\PlaybackEngines\AndroidPlayer\Variations\il2cpp\Release\Classes)
    * mirzalibrary-1.1.5-release.aar
2. clone this project
3. copy libraries to libs
4. Open this project(ex:android studio)
5. Build this project(if you execute grqdle:assemble,this library copy to Unity-Library\Assets\MiRZAUnityLibrary\Plugins\MiRZANativeLibrary).

### Unity Project
1. get required libraries:
    * mirza_library-1.1.5.tgz
    * com.qualcomm.snapdragon.spaces-1.0.2.tgz
2. clone this project
3. copy libraries the below dirstory
    * mirza_library-1.1.5.tgz : Unity-Library\Packages\MiRZA\
    * com.qualcomm.snapdragon.spaces-1.0.2.tgz : Unity-Library\Packages\SnapdragonSpaces\
4. Open this project.
