# MiRZANativeLibrary
This project is a Java library that adapts MiRZALibrary for Unity.

|Support MiRZA Library Version|
|:-:|
|v1.1.5|

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
