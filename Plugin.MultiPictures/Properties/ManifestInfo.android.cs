
using Android.App;

#region Camera

[assembly: UsesPermission(Android.Manifest.Permission.Camera)]

[assembly: UsesPermission(Android.Manifest.Permission.Flashlight)]

[assembly: UsesPermission(Android.Manifest.Permission.ReadExternalStorage)]

[assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)]

[assembly: UsesFeature("android.hardware.camera.full")]

#endregion
