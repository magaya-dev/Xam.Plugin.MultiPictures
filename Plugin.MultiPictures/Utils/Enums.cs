
namespace Plugin.MultiPictures.Utils
{
    public enum DeviceRotation
    {
        Unknown,
        Portrait,
        Landscape,
        ReversePortrait,
        ReverseLandscape
    }

    public enum Camera2State : int
    {
        // Camera state: Showing camera preview.
        Preview = 0,

        // Camera state: Waiting for the focus to be locked.
        WaitingLock = 1,

        // Camera state: Waiting for the exposure to be precapture state.
        WaitingPrecapture = 2,

        //Camera state: Waiting for the exposure state to be something other than precapture.
        WaitingNonPrecapture = 3,

        // Camera state: Picture was taken.
        PictureTaken = 4
    }

    public enum CameraOptions
    {
        Rear,
        Front
    }

    public enum PhotoSize
    {
        Small,
        Medium,
        Large,
        Full,
        Custom
    }
}
