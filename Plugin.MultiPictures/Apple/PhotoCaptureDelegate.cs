
using System;
using AVFoundation;
using CoreMedia;
using Foundation;

namespace Plugin.MultiPictures.Apple
{
    internal class PhotoCaptureDelegate : AVCapturePhotoCaptureDelegate
    {
        private NSData _photoData;

        public event EventHandler<NSData> PictureTaken;

        public override void DidFinishProcessingPhoto(AVCapturePhotoOutput captureOutput,
                                                      CMSampleBuffer photoSampleBuffer,
                                                      CMSampleBuffer previewPhotoSampleBuffer,
                                                      AVCaptureResolvedPhotoSettings resolvedSettings,
                                                      AVCaptureBracketedStillImageSettings bracketSettings,
                                                      NSError error)
        {
            if (photoSampleBuffer != null)
            {
                _photoData = AVCapturePhotoOutput.GetJpegPhotoDataRepresentation(photoSampleBuffer, previewPhotoSampleBuffer);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error capturing photo: {0}", error.LocalizedDescription));
            }

            PictureTaken?.Invoke(this, _photoData);
        }
    }
}
