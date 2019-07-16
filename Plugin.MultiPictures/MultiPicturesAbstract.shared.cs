
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plugin.MultiPictures.Utils;
using Plugin.MultiPictures.Views;
using PA = Plugin.Permissions.Abstractions;

namespace Plugin.MultiPictures
{
    public abstract class MultiPicturesAbstract : IMultiPictures
    {
        protected CameraPreview camera;

        protected DeviceRotation requestedRotation;

        public CameraPreview Camera
        {
            get { return camera; }
            set
            {
                camera = value;
            }
        }

        public virtual bool IsCameraAvailable { get; }

        public DeviceRotation RequestedRotation
        {
            get => requestedRotation;
            set
            {
                requestedRotation = value;
                Xamarin.Forms.MessagingCenter.Send<IMultiPictures, DeviceRotation>(this, "requestedRotation", requestedRotation);
            }
        }

        public void CloseCamera()
        {
            if (IsCameraAvailable == true)
            {
                Camera?.StopRecording?.Invoke();
            }
        }

        public async Task<CameraView> CameraView(MediaOptions mediaOptions = null)
        {
            if (IsCameraAvailable == false)
            {
                throw new NotSupportedException("Current device has no camera support");
            }

            var permissions = new List<PA.Permission>() { PA.Permission.Camera };
            if (mediaOptions.UsePublicStorage == true)
            {
                permissions.Add(PA.Permission.Storage);
            }

            try
            {
                await HasPermissions(permissions.ToArray());
            }
            catch (MediaPermissionException ex)
            {
                throw ex;
            }

            RequestedRotation = DeviceRotation.Portrait; // fix content page orientation
            var cameraView = new CameraView(mediaOptions);
            var portraitPage = new Xamarin.Forms.ContentPage() { Content = cameraView };

            await Xamarin.Forms.Application.Current.MainPage.Navigation.PushModalAsync(portraitPage);

            BindPage(portraitPage);

            return cameraView;
        }

        public async Task HasPermissions(params PA.Permission[] permissions)
        {
            var notGrantedPermissions = new List<PA.Permission>();
            foreach (var permission in permissions)
            {
                bool hasPermission = false;
                switch (permission)
                {
                    case PA.Permission.Camera:
                        hasPermission = await HasCameraPermission();
                        break;
                    case PA.Permission.Storage:
                        hasPermission = await HasStoragePermission();
                        break;
                }

                if (hasPermission == false)
                {
                    notGrantedPermissions.Add(permission);
                }
            }

            if (notGrantedPermissions.Count == 0)
            {
                return;
            }

            throw new MediaPermissionException(notGrantedPermissions.ToArray());
        }

        #region Abstract methods

        protected abstract void BindPage(Xamarin.Forms.ContentPage page);

        public abstract string MediaFolderPath(MediaOptions mediaOptions);

        public abstract Task ResizeAndRotateAsync(MediaFile file, MediaOptions mediaOptions, DeviceRotation currentRotaion = DeviceRotation.Unknown);

        public abstract Task<byte[]> ResizedStream(MediaFile file, MediaOptions mediaOptions);

        protected abstract Task<bool> HasCameraPermission();

        protected abstract Task<bool> HasStoragePermission();

        #endregion
    }
}
