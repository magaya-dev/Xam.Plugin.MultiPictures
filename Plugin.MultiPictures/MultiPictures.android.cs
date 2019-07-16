
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Plugin.MultiPictures.Droid;
using Plugin.MultiPictures.Utils;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Env = Android.OS.Environment;
using PA = Plugin.Permissions.Abstractions;

namespace Plugin.MultiPictures
{
    public class MultiPicturesImplementation : MultiPicturesAbstract
    {
        const string TAG_PIXEL_X_DIMENSION = "PixelXDimension";

        const string TAG_PIXEL_Y_DIMENSION = "PixelYDimension";

        private IList<string> _requestedPermissions;

        private readonly Context _context;

        public override bool IsCameraAvailable { get; }

        public MultiPicturesImplementation()
        {
            _context = Application.Context;

            RequestedRotation = DeviceRotation.Unknown;

            IsCameraAvailable = _context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Gingerbread)
            {
                IsCameraAvailable |= _context.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFront);
            }
        }

        public override string MediaFolderPath(MediaOptions mediaOptions)
        {
            string result;
            if (mediaOptions.UsePublicStorage == false)
            {
                result = Android.App.Application.Context.GetExternalFilesDir(null).AbsolutePath;
            }
            else
            {
                result = Env.GetExternalStoragePublicDirectory(Env.DirectoryPictures).AbsolutePath;
            }

            if (string.IsNullOrWhiteSpace(mediaOptions.Directory) == false)
            {
                result = System.IO.Path.Combine(result, mediaOptions.Directory);
            }

            if (Directory.Exists(result).Equals(false))
            {
                Directory.CreateDirectory(result);
            }

            return result;
        }

        public override async Task ResizeAndRotateAsync(MediaFile file, MediaOptions mediaOptions, DeviceRotation currentRotaion = DeviceRotation.Unknown)
        {
            var originalMetadata = new ExifInterface(file.Path);

            var transformVM = new TransformViewModel
            {
                FilePath = file.Path,
                MediaOptions = mediaOptions,
                Exif = originalMetadata,
                CurrentRotation = currentRotaion
            };

            await ResizeAndRotateAsync(transformVM);

            originalMetadata?.Dispose();

            GC.Collect();
        }

        public override async Task<byte[]> ResizedStream(MediaFile file, MediaOptions mediaOptions)
        {
            try
            {
                var result = await ResizedStream(file.Path, mediaOptions);
                return result?.ToArray() ?? null;
            }
            finally
            {
                GC.Collect();
            }
        }

        protected override async Task<bool> HasCameraPermission()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                return true;
            }

            var status = PermissionStatus.Granted;
            bool checkCamera = HasPermissionInManifest(Android.Manifest.Permission.Camera);

            if (checkCamera == true)
            {
                status = await CrossPermissions.Current.CheckPermissionStatusAsync(PA.Permission.Camera);
            }

            if (status != PermissionStatus.Granted)
            {
                var result = await CrossPermissions.Current.RequestPermissionsAsync(PA.Permission.Camera);

                if (result[PA.Permission.Camera] != PermissionStatus.Granted)
                {
                    return false;
                }

                await Task.Delay(500); // some devices require some time ;-)
            }

            return true;
        }

        protected override async Task<bool> HasStoragePermission()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                return true;
            }

            var status = PermissionStatus.Granted;
            bool checkStorage = HasPermissionInManifest(Android.Manifest.Permission.ReadExternalStorage);
            checkStorage |= HasPermissionInManifest(Android.Manifest.Permission.WriteExternalStorage);

            if (checkStorage == true)
            {
                status = await CrossPermissions.Current.CheckPermissionStatusAsync(PA.Permission.Storage);
            }

            if (status != PermissionStatus.Granted)
            {
                var result = await CrossPermissions.Current.RequestPermissionsAsync(PA.Permission.Storage);
                if (result[PA.Permission.Storage] != PermissionStatus.Granted)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void BindPage(Xamarin.Forms.ContentPage page)
        {
            page.Appearing += delegate
            {
                RequestedRotation = DeviceRotation.Portrait;
            };

            page.Disappearing += delegate
            {
                RequestedRotation = DeviceRotation.Unknown;
            };
        }

        #region Utilities

        private Task<bool> ResizeAndRotateAsync(TransformViewModel transformVM)
        {
            if (string.IsNullOrWhiteSpace(transformVM.FilePath))
            {
                return Task.FromResult(false);
            }

            var photoSize = transformVM.MediaOptions.PhotoSize;
            var customPhotoSize = transformVM.MediaOptions.CustomPhotoSize;
            var quality = transformVM.MediaOptions.CompressionQuality;

            return Task.Run(() =>
            {
                try
                {
                    if (photoSize == PhotoSize.Full && transformVM.CurrentRotation == DeviceRotation.Unknown)
                    {
                        return false;
                    }

                    var percent = 1.0f;
                    switch (photoSize)
                    {
                        case PhotoSize.Large:
                            percent = 0.75f;
                            break;
                        case PhotoSize.Medium:
                            percent = 0.5f;
                            break;
                        case PhotoSize.Small:
                            percent = 0.25f;
                            break;
                        case PhotoSize.Custom:
                            percent = (float)customPhotoSize / 100f;
                            break;
                    }

                    // first decode to just get dimensions
                    var options = new BitmapFactory.Options { InJustDecodeBounds = true };

                    // already on background task
                    BitmapFactory.DecodeFile(transformVM.FilePath, options);

                    var finalWidth = (int)(options.OutWidth * percent);
                    var finalHeight = (int)(options.OutHeight * percent);

                    // set scaled image dimesions
                    transformVM.Exif?.SetAttribute(TAG_PIXEL_X_DIMENSION, Java.Lang.Integer.ToString(finalWidth));
                    transformVM.Exif?.SetAttribute(TAG_PIXEL_Y_DIMENSION, Java.Lang.Integer.ToString(finalHeight));

                    // calculate sample size
                    options.InSampleSize = CalculateInSampleSize(options, finalWidth, finalHeight);

                    // turn off decode
                    options.InJustDecodeBounds = false;

                    //this now will return the requested width/height from file, so no longer need to scale
                    using (var originalImage = BitmapFactory.DecodeFile(transformVM.FilePath, options))
                    {
                        // rotate the image
                        var matrix = new Matrix();
                        switch (transformVM.CurrentRotation)
                        {
                            case DeviceRotation.Portrait:
                                matrix.PreRotate(90);
                                break;
                            case DeviceRotation.Landscape:
                                matrix.PreRotate(180);
                                break;
                            case DeviceRotation.ReversePortrait:
                                matrix.PreRotate(270);
                                break;
                            case DeviceRotation.ReverseLandscape:
                                break;
                        }

                        var rotated = Bitmap.CreateBitmap(originalImage, 0, 0, originalImage.Width, originalImage.Height, matrix, false);

                        matrix.Dispose();
                        matrix = null;

                        // always need to compress to save back to disk
                        using (var stream = File.Open(transformVM.FilePath, FileMode.Create, FileAccess.ReadWrite))
                        {
                            rotated.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                            stream.Close();
                        }

                        originalImage.Recycle();
                        rotated.Recycle();

                        if (transformVM.MediaOptions.SaveMetaData && IsValidExif(transformVM.Exif))
                        {
                            try
                            {
                                transformVM.Exif?.SaveAttributes();
                            }
                            catch (Exception ex)
                            {
#if DEBUG
                                Console.WriteLine($"Unable to save exif {ex}");
#endif
                            }
                        }

                        // Dispose of the Java side bitmap
                        GC.Collect();
                        return true;
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.StackTrace);
                    throw ex;
#else
                    return Task.FromResult(false);
#endif
                }
            });
        }

        private Task<MemoryStream> ResizedStream(string filePath, MediaOptions mediaOptions)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return Task.FromResult(System.IO.Stream.Null as MemoryStream);
            }

            var photoSize = mediaOptions.PhotoSize;
            var customPhotoSize = mediaOptions.CustomPhotoSize;
            var quality = mediaOptions.CompressionQuality;

            return Task.Run(() =>
            {
                try
                {
                    if (photoSize == PhotoSize.Full)
                    {
                        return null;
                    }

                    var percent = 1.0f;
                    switch (photoSize)
                    {
                        case PhotoSize.Large:
                            percent = 0.75f;
                            break;
                        case PhotoSize.Medium:
                            percent = 0.5f;
                            break;
                        case PhotoSize.Small:
                            percent = 0.25f;
                            break;
                        case PhotoSize.Custom:
                            percent = (float)customPhotoSize / 100f;
                            break;
                    }

                    // first decode to just get dimensions
                    var options = new BitmapFactory.Options { InJustDecodeBounds = true };

                    // already on background task
                    BitmapFactory.DecodeFile(filePath, options);

                    var finalWidth = (int)(options.OutWidth * percent);
                    var finalHeight = (int)(options.OutHeight * percent);

                    // calculate sample size
                    //options.InSampleSize = CalculateInSampleSize(options, finalWidth, finalHeight);

                    // turn off decode
                    options.InJustDecodeBounds = false;

                    //this now will return the requested width/height from file, so no longer need to scale
                    using (var originalImage = BitmapFactory.DecodeFile(filePath, options))
                    {
                        var result = new MemoryStream();
                        var resizedImage = Bitmap.CreateScaledBitmap(originalImage, finalWidth, finalHeight, true);

                        resizedImage.Compress(Bitmap.CompressFormat.Jpeg, quality, result);

                        // always need to compress to save back to disk
                        //using (var stream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite))
                        //{

                        //    originalImage.Compress(Bitmap.CompressFormat.Jpeg, quality, stream);
                        //    stream.Close();
                        //}

                        originalImage.Recycle();
                        resizedImage.Recycle();

                        // Dispose of the Java side bitmap
                        GC.Collect();
                        return result;
                    }
                }
                catch (Exception ex)
                {
#if DEBUG
                    Console.WriteLine(ex.StackTrace);
                    throw ex;
#else
                    return Task.FromResult(System.IO.Stream.Null);
#endif
                }
            });
        }

        private int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
        {
            // Raw height and width of image
            var height = options.OutHeight;
            var width = options.OutWidth;
            var inSampleSize = 1;

            if (height > reqHeight || width > reqWidth)
            {

                var halfHeight = height / 2;
                var halfWidth = width / 2;

                // Calculate the largest inSampleSize value that is a power of 2 and keeps both
                // height and width larger than the requested height and width.
                while ((halfHeight / inSampleSize) >= reqHeight
                        && (halfWidth / inSampleSize) >= reqWidth)
                {
                    inSampleSize *= 2;
                }
            }

            return inSampleSize;
        }

        private bool IsValidExif(ExifInterface exif)
        {
            // if null, then not falid
            if (exif == null)
            {
                return false;
            }

            try
            {
                // if has thumb, but is <= 0, then not valid
                if (exif.HasThumbnail && (exif.GetThumbnail()?.Length ?? 0) <= 0)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("Unable to get thumbnail exif: " + ex);
#endif
                return false;
            }

            return true;
        }

        private bool HasPermissionInManifest(string permission)
        {
            try
            {
                if (_requestedPermissions != null)
                {
                    return _requestedPermissions.Any(p => p.Equals(permission, StringComparison.InvariantCultureIgnoreCase));
                }

                if (_context == null)
                {
#if DEBUG
                    Console.WriteLine(@"
                                        Unable to detect current Activity or App Context. Ensure Plugin.CurrentActivity is installed in your
                                        Android project and your Application class is registering with Application.IActivityLifecycleCallbacks.
                                       ");
#endif
                    return false;
                }

                var info = _context.PackageManager.GetPackageInfo(_context.PackageName, PackageInfoFlags.Permissions);
                if (info == null)
                {
#if DEBUG
                    Console.WriteLine("Unable to get Package info, will not be able to determine permissions to request.");
#endif
                    return false;
                }

                _requestedPermissions = info.RequestedPermissions;

                if (_requestedPermissions == null)
                {
#if DEBUG
                    Console.WriteLine("There are no requester permissions. Check out to ensure you have specified permissions you want to request.");
#endif
                    return false;
                }

                return _requestedPermissions.Any(p => p.Equals(permission, StringComparison.InvariantCultureIgnoreCase));
            }
            catch (Exception ex)
            {
#if DEBUG
                Console.WriteLine("Unable to check out Manifest file for permissions: {0}", ex);
#endif
                return false;
            }
        }

        #endregion
    }
}
