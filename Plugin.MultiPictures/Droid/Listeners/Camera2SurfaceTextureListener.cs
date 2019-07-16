using Android.Graphics;
using Android.Views;

namespace Plugin.MultiPictures.Droid.Listeners
{
    public class Camera2SurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        private readonly ICamera2 _camera2;

        public Camera2SurfaceTextureListener(ICamera2 owner)
        {
            this._camera2 = owner ?? throw new System.ArgumentNullException("camera2");
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _camera2.OpenCamera(width, height);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            _camera2.CameraDevice?.Close();
            _camera2.CameraDevice = null;
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            _camera2.ConfigureTransform(width, height);
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {

        }
    }
}