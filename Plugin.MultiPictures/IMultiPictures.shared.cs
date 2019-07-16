using System.Threading.Tasks;
using Plugin.MultiPictures.Utils;
using Plugin.MultiPictures.Views;

namespace Plugin.MultiPictures
{
    public interface IMultiPictures
    {
        bool IsCameraAvailable { get; }

        CameraPreview Camera { get; set; }

        DeviceRotation RequestedRotation { get; set; }

        void CloseCamera();

        Task HasPermissions(params Plugin.Permissions.Abstractions.Permission[] permissions);

        string MediaFolderPath(MediaOptions mediaOptions);

        Task<CameraView> CameraView(MediaOptions mediaOptions = null);

        Task ResizeAndRotateAsync(MediaFile file, MediaOptions mediaOptions, DeviceRotation currentRotaion = DeviceRotation.Unknown);

        Task<byte[]> ResizedStream(MediaFile file, MediaOptions mediaOptions);
    }
}
