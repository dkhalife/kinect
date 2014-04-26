using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //KinectSensor _sensor;
        Skeleton[] allSkeletons = new Skeleton[6];

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            /*if (KinectSensor.KinectSensors.Count > 0)
            {
                _sensor = KinectSensor.KinectSensors[0];
                if (_sensor.Status == KinectStatus.Connected)
                {
                    _sensor.ColorStream.Enable();
                    _sensor.DepthStream.Enable();
                    _sensor.SkeletonStream.Enable();
                    _sensor.AllFramesReady += _sensor_AllFramesReady;
                    _sensor.Start();
                }
            }*/

            kinectSensorChooser1.KinectSensorChanged += kinectSensorChooser1_KinectSensorChanged;
        }

        void kinectSensorChooser1_KinectSensorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            KinectSensor oldSensor = (KinectSensor)e.OldValue;
            StopKinect(oldSensor);

            KinectSensor newSensor = (KinectSensor)e.NewValue;
            newSensor.ColorStream.Enable();
            newSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            newSensor.SkeletonStream.Enable();
            newSensor.AllFramesReady += _sensor_AllFramesReady;

            try
            {
                newSensor.Start();
                //newSensor.ElevationAngle = 10;
                //angle.Content = newSensor.ElevationAngle;
            }
            catch (System.IO.IOException)
            {
                kinectSensorChooser1.AppConflictOccurred();
            }
        }

        void _sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            /*DepthImageFrame depthFrame = e.OpenDepthImageFrame();
            if (depthFrame == null)
            {
                return;
            }

            byte[] pixels1 = GenerateColoredBytes(depthFrame);
            int stride1 = depthFrame.Width * 4;
            Image1.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels1, stride1);

            ColorImageFrame colorFrame = e.OpenColorImageFrame();
            if (colorFrame == null)
            {
                return;
            }

            byte[] pixels2 = new byte[colorFrame.PixelDataLength];
            colorFrame.CopyPixelDataTo(pixels2);
            int stride2 = colorFrame.Width * 4;
            Image2.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels2, stride2);
            */
            Skeleton first = GetFirstSkeleton(e);
            if (first == null) return;
            
            GetCameraPoint(first, e);

            // set scaled position
            //ScalePosition(left, first.Joints[JointType.HandLeft]);
            //ScalePosition(right, first.Joints[JointType.HandRight]);
            //ScalePosition(head, first.Joints[JointType.Head]);
        }

        private byte[] GenerateColoredBytes(DepthImageFrame depthFrame)
        {
            short[] rawDepthDate = new short[depthFrame.PixelDataLength];
            depthFrame.CopyPixelDataTo(rawDepthDate);

            // use depthFrame to create the image to display on-screen
            // depthFrame contains color information for all the pixels in image
            // Height * Width * 4 (RGBE)
            Byte[] pixels = new byte[depthFrame.Height * depthFrame.Width * 4];

            // BGR32 = BGRE
            // BGRA32 = BGRA // In that case we need to set the transparency because .net defaults to 0 which is transparent

            // data locations
            const int BlueIndex = 0;
            const int GreenIndex = 1;
            const int RedIndex = 2;

            for (int depthIndex = 0, colorIndex = 0; depthIndex < rawDepthDate.Length && colorIndex < pixels.Length; ++depthIndex, colorIndex += 4)
            {
                // Formula for player
                int player = rawDepthDate[depthIndex] & DepthImageFrame.PlayerIndexBitmask;
                // Formula for depth
                int depth = rawDepthDate[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                // .9m
                if (depth <= 900)
                {
                    pixels[colorIndex + BlueIndex] = 255;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 0;
                }
                // .9 - 2.0
                else if (depth > 900 && depth < 2000)
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 255;
                    pixels[colorIndex + RedIndex] = 0;
                }
                else if (depth > 2000)
                {
                    pixels[colorIndex + BlueIndex] = 0;
                    pixels[colorIndex + GreenIndex] = 0;
                    pixels[colorIndex + RedIndex] = 255;
                }

                // equal coloring for monochromatic histogram
                byte intensity = CalculateIntensityFromDepth(depth);
                pixels[colorIndex + BlueIndex] = intensity;
                pixels[colorIndex + GreenIndex] = intensity;
                pixels[colorIndex + RedIndex] = intensity;

                // Color all players gold
                if (player > 0) // There is a player
                {
                    pixels[colorIndex + BlueIndex] = Colors.Gold.B;
                    pixels[colorIndex + GreenIndex] = Colors.Gold.G;
                    pixels[colorIndex + RedIndex] = Colors.Gold.R;
                }
            }

            return pixels;
        }

        private byte CalculateIntensityFromDepth(int distance)
        {
            int MinDepthDistance = 50;
            int MaxDepthDistanceOffset = 1000;
            return (byte)(255 - (255 * Math.Max(distance - MinDepthDistance, 0) / (MaxDepthDistanceOffset)));
        }

        void StopKinect(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.AudioSource.Stop();
            }
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            StopKinect(kinectSensorChooser1.Kinect);
        }

        private void kinectSensorChooser1_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void KinectSensorChooser_Loaded_1(object sender, RoutedEventArgs e)
        {

        }

        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                    return null;
                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                // get the firsdt skeleton
                return (from s in allSkeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
            }
        }

        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || kinectSensorChooser1.Kinect == null)
                {
                    return;
                }

                // Map a joint location to a point on the depth map
                // head
                DepthImagePoint headDepthPoint = kinectSensorChooser1.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(first.Joints[JointType.Head].Position, DepthImageFormat.Resolution320x240Fps30);
                // Left
                DepthImagePoint leftDepthPoint = kinectSensorChooser1.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(first.Joints[JointType.HandLeft].Position, DepthImageFormat.Resolution320x240Fps30);
                // Right
                DepthImagePoint rightDepthPoint = kinectSensorChooser1.Kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(first.Joints[JointType.HandRight].Position, DepthImageFormat.Resolution320x240Fps30);

                // Map a depth point to a point on the color image
                // head
                ColorImagePoint headColorPoint = kinectSensorChooser1.Kinect.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution320x240Fps30, headDepthPoint, ColorImageFormat.RgbResolution640x480Fps30);
                // left
                ColorImagePoint leftColorPoint = kinectSensorChooser1.Kinect.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution320x240Fps30, leftDepthPoint, ColorImageFormat.RgbResolution640x480Fps30);
                // right
                ColorImagePoint rightColorPoint = kinectSensorChooser1.Kinect.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution320x240Fps30, rightDepthPoint, ColorImageFormat.RgbResolution640x480Fps30);

                // Set location
                CameraPosition(head, headColorPoint);
                CameraPosition(left, leftColorPoint);
                CameraPosition(right, rightColorPoint);
            }
        }

        private void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            Canvas.SetLeft(element, point.X - element.Width / 2);
            Canvas.SetLeft(element, point.Y - element.Height / 2);
        }

        private void ScalePosition(FrameworkElement element, Joint joint)
        {
            //Joint scaledJoint = joint.ScaleTo(1280, 720);
        }
    }
}
