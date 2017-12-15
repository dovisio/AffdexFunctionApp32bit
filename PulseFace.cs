using System.Collections.Generic;
using Affdex;
using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using Newtonsoft.Json;

namespace Pulse.SDK
{
    public class Analyzer : Affdex.ProcessStatusListener, Affdex.ImageListener
    {
        Affdex.Detector detector = null;
        public string DataFolder { get; set; }
        public int MaxRecognizedFaces { get; set; }
        public FaceDetectorMode FaceDetectionMode { get; set; }

        private Affdex.Frame LoadFrameFromFile(string fileName)
        {
            Bitmap bitmap = new Bitmap(fileName);

            // Lock the bitmap's bits.
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;

            // Declare an array to hold the bytes of the bitmap. 
            int numBytes = bitmap.Width * bitmap.Height * 3;
            byte[] rgbValues = new byte[numBytes];

            int data_x = 0;
            int ptr_x = 0;
            int row_bytes = bitmap.Width * 3;

            // The bitmap requires bitmap data to be byte aligned.
            // http://stackoverflow.com/questions/20743134/converting-opencv-image-to-gdi-bitmap-doesnt-work-depends-on-image-size

            for (int y = 0; y < bitmap.Height; y++)
            {
                Marshal.Copy(ptr + ptr_x, rgbValues, data_x, row_bytes);//(pixels, data_x, ptr + ptr_x, row_bytes);
                data_x += row_bytes;
                ptr_x += bmpData.Stride;
            }

            bitmap.UnlockBits(bmpData);

            return new Affdex.Frame(bitmap.Width, bitmap.Height, rgbValues, Affdex.Frame.COLOR_FORMAT.BGR);
        }

        public Analyzer(string dataFolder = @".\data", int maxRecognizedFaces = 100, FaceDetectorMode faceDetectionMode = FaceDetectorMode.SMALL_FACES)
        {
            this.DataFolder = dataFolder;
            this.MaxRecognizedFaces = maxRecognizedFaces;
            this.FaceDetectionMode = faceDetectionMode;

            detector =
                 new Affdex.PhotoDetector((uint)MaxRecognizedFaces, (Affdex.FaceDetectorMode)FaceDetectionMode);

            if (detector != null)
            {
                // ProcessVideo videoForm = new ProcessVideo(detector);
                detector.setClassifierPath(DataFolder);
                detector.setDetectAllEmotions(true);
                detector.setDetectAllExpressions(true);
                detector.setDetectAllEmojis(true);
                detector.setDetectAllAppearances(true);
                detector.start();
                System.Console.WriteLine("Face detector mode = " + detector.getFaceDetectorMode().ToString());
                //if (isVideo) ((Affdex.VideoDetector)detector).process(options.Input);
                //else if (isImage)
                detector.setImageListener(this);
                detector.setProcessStatusListener(this);
                //videoForm.ShowDialog(); 
            }
        }

        AutoResetEvent waitHandle;
        private List<PulseFace> facesResult = null;
        public List<PulseFace> Process(string image)
        {
            waitHandle = new AutoResetEvent(false);
            // Pass waitHandle as user state
            //Phone.GetLampMode(btn, waitHandle);
            ((PhotoDetector)detector).process(LoadFrameFromFile(image));

            // Wait for event completion
            waitHandle.WaitOne();

            detector.stop();

            return facesResult;
        }

        public void onProcessingException(AffdexException ex)
        {
            throw ex;

        }

        public void onProcessingFinished()
        {
            //throw new NotImplementedException();
            //waitHandle.Set();
        }

        public void onImageResults(Dictionary<int, Face> faces, Frame frame)
        {
            facesResult = new List<PulseFace>();
            foreach (var face in faces)
            {
                facesResult.Add(new PulseFace(face));
            }

            //throw new NotImplementedException();
            waitHandle.Set();
        }

        public void onImageCapture(Frame frame)
        {
            // throw new NotImplementedException();
            //waitHandle.Set();
        }
    }

    [JsonObject]
    public class PulseFace
    {

        public Face Face { get; set; }
        public int Id { get; set; }

        public PulseFace(KeyValuePair<int, Face> face)
        {
            this.Id = face.Key;

            this.Face = face.Value;
        }

        public override string ToString()
        {
            return $"Id: {Id} , Quality: {Face.FaceQuality.Brightness}, Happiness: {Face.Emotions.Joy} ";
        }
    }
}