# Plugin.MultiPictures

Cross platform plugin for taking/picking several pictures.

_This package stills under development and it may contains some issues. Feel free to use `as it is` and if you want make some improvement just do it and create a PR._

__Platform Support__

| Platform | Version |
| - | - |
| Xamarin.Android | API 25+ |
| Xamarin.iOS | iOS 10+ |

## Setup

* Install into PCL/.NetStandard project and client projects.
* Read additional setup for each platform

Picking pictures feature uses [Collection View](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/collectionview/introduction). Read additional setup for this visual element.

### Dependencies

* You must install and setup the followings dependencies into your PCL/.NetStandard project and client projects:
  * [Permissions Plugin](https://github.com/jamesmontemagno/PermissionsPlugin)

### Android Setup

This API uses `CAMERA`, `READ_EXTERNAL_STORAGE` and `WRITE_EXTERNAL_STORAGE` permissions but it will automatically add these for you. You must add the __Permission Plugin__ code into your __Main Activity__:

```C#
public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
{
    Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
}
```

__Note__: After creating a Xamarin Android project, you may see a default implementation of `OnRequestPermissionsResult` method into your __MainActivity__ class. Please be sure to override it with the sample code provided above or, if necessary, keep any code you added there and add the two lines mentioned earlier.

#### Android Dependencies

 Follow installation and initialization steps for each dependency.

* [Current Activity Plugin](https://github.com/jamesmontemagno/CurrentActivityPlugin/blob/master/README.md).

### iOS Setup

This API uses `NSCameraUsageDescription` and `NSPhotoLibraryUsageDescription` permissions. Make sure to add this entries into your __into.plist__ file. If you want to save photos into gallery by using __"UsePublicStorage"__ property then you must add  `NSPhotoLibraryAddUsageDescription` entry too. The string you provide for each of these entries will be displayed to the user when they are prompted to provide permission to access these device features. [Read more about iOS permissions](https://devblogs.microsoft.com/xamarin/new-ios-10-privacy-permission-settings/) 

Sample:

```XML
<key>NSCameraUsageDescription</key>
<string>This app needs access to the camera to take photos.</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>This app needs access to photos.</string>
<key>NSMicrophoneUsageDescription</key>
<string>This app needs access to microphone.</string>
<key>NSPhotoLibraryAddUsageDescription</key>
<string>This app needs access to the photo gallery.</string>
```

If you want the dialogs to be translated you must support the specific languages in your app. Read the [iOS Localization Guide](https://docs.microsoft.com/en-us/xamarin/ios/app-fundamentals/localization/)

In order to show the camera view on _Portrait_ orientation regardless device current rotation it is necessary to add some code into your __AppDelegate__ file. Make sure to override `GetSupportedInterfaceOrientations` function with following code:

```C#
public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, [Transient] UIWindow forWindow)
{
    if (CrossMultiPictures.Current.RequestedRotation == DeviceRotation.Unknown)
    {
        return UIInterfaceOrientationMask.All;
    }

    return UIInterfaceOrientationMask.Portrait;
}
``` 

## How to use MultiPictures API

Call __CrossMultiPictures__ from any project to gain access to API. It is recommendable to check if current device supports camera by doing the following:

```C#
if(CrossMultiPictures.Current.IsCameraAvailable == false)
{
    await DisplayAlert("Not Available", "Camere is not available :((", "OK");
}
```

### Taking Pictures

For taking new pictures you can use __CameraView()__ render instance and subscribe to __PicturesTaken__ event as shown bellow. You may provide an instance of __MediaOptions__ as an optional parameter. If you call __CameraView()__ without checking `IsCameraAvailable` value first, a __NotSupportedException__ will be thrown.

```C#
void TakePictures() 
{
    try 
    {
        var mediaOptions = new MediaOptions
        {
            CompressionQuality = 90,
            PhotoSize = PhotoSize.Medium,
            SaveMetaData = true,
            UsePublicStorage = false,
            Directory = "Sample"
        };

        var cameraView = await CrossMultiPictures.Current.CameraView(mediaOptions);
        cameraView.PicturesTaken += (s, e) =>
        {
            // do whatever you want with taken pictures
        };
    }
    catch(NotSupportedException ex)
    {
        await DisplayAlert("Not Available", "Camera is not available in this device", "OK");
    }
    catch (MediaPermissionException ex)
    {
        await DisplayAlert("Permission Denied", ex.Message, "OK");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine("Something went wrong: {0}", ex.StackTrace);
    }
}
```
The initialization values for this sample instance of `MediaOptions` are the default values when using __CameraView__ custom view renderer with a null value as parameter. If you need to use a different renderer you can build your own. Just go deep into __CameraView__ implementation provided with this plugin for having a better idea about how to build it.

### Picking Pictures

For picking pictures you just need to show an instance of __PickPhotosPage__ and receive selected pictures on __PhotosSelected__ event as shown in next sample code. Just make sure to specify the directory where you  want to pick photos from. 

```C#
var directory = CrossMultiPictures.Current.MediaFolderPath(new MediaOptions { Directory = "Sample" });
var pickPage = new PickPhotosPage(directory);
pickPage.PhotosSelected += (s, e) =>
{
    // do whatever you want with picked pictures.
};
```





