
using System;
using Foundation;
using Plugin.MultiPictures.Utils;
using UIKit;

namespace Plugin.MultiPictures.Apple
{
    public class OrientationListener : IOrientationListener
    {
        public DeviceRotation DeviceRotation { get; set; }

        public event EventHandler<DeviceRotation> OrientationChanged;

        public OrientationListener()
        {
            DeviceRotation = DeviceRotation.Unknown;
            UIDevice.Notifications.ObserveOrientationDidChange(OnOrientationChanged);
        }

        protected void OnOrientationChanged(object sender, NSNotificationEventArgs args)
        {
            var rotaion = DeviceRotation;
            switch (UIDevice.CurrentDevice.Orientation)
            {
                case UIDeviceOrientation.Portrait:
                    DeviceRotation = DeviceRotation.Portrait;
                    break;
                case UIDeviceOrientation.LandscapeRight:
                    DeviceRotation = DeviceRotation.Landscape;
                    break;
                case UIDeviceOrientation.PortraitUpsideDown:
                    DeviceRotation = DeviceRotation.ReversePortrait;
                    break;
                case UIDeviceOrientation.LandscapeLeft:
                    DeviceRotation = DeviceRotation.ReverseLandscape;
                    break;
            }

            if (rotaion != DeviceRotation)
            {
                OrientationChanged?.Invoke(this, DeviceRotation);
            }
        }
    }

    public interface IOrientationListener
    {
        DeviceRotation DeviceRotation { get; set; }

        event EventHandler<DeviceRotation> OrientationChanged;
    }
}
