﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Structure;

namespace AP.Logic
{
    public class FaceProcessor : IFaceProcessor
    {
        private CascadeClassifier Face { get; set; }
        private CascadeClassifier Eye { get; set; }
        private CascadeClassifier EyePair { get; set; }

        public FaceProcessor()
        {
            const string faceCascade = "haarcascade_frontalface_alt2.xml";
            const string eyeCascade = "haarcascade_eye.xml";
            const string eyePairCascade = "haarcascade_mcs_eyepair_big.xml";

            Face = new CascadeClassifier(faceCascade);
            Eye = new CascadeClassifier(eyeCascade);
            EyePair = new CascadeClassifier(eyePairCascade);
        }

        public IList<Rectangle> GetEyes(Image<Bgr, byte> image, Rectangle face)
        {
            image.ROI = face;
            using (var gray = image.Convert<Gray, Byte>())
            {
                var pairs = EyePair.DetectMultiScale(gray, 1.1, 10, new Size(20, 20), Size.Empty);
                var pair = pairs.FirstOrDefault();
                if (!pair.Size.IsEmpty)
                {
                    var eyes = Eye.DetectMultiScale(gray, 1.1, 10, new Size(20, 20), Size.Empty);
                    if (eyes.Length < 2)
                    {
                        image.ROI = Rectangle.Empty;
                        return new List<Rectangle> { new Rectangle(), new Rectangle() };
                    }
                    var filteredEyes = eyes.Where(_ => ContainEye(pair, _)).ToList();
                    var offsetedEyes = filteredEyes.Select(_ => OffsetEye(face, _)).ToList();
                    image.ROI = Rectangle.Empty;
                    return offsetedEyes;
                }
                image.ROI = Rectangle.Empty;
                return new List<Rectangle>{new Rectangle(), new Rectangle()};
            }
        }

        private static Rectangle OffsetEye(Rectangle face, Rectangle eye)
        {
            return new Rectangle(face.X + eye.X, face.Y + eye.Y, eye.Width, eye.Height);
        }


        private bool ContainEye(Rectangle pair, Rectangle eye)
        {
            var eyeCenter = new Point(eye.X + eye.Width/2, eye.Y + eye.Height/2);
            return pair.Contains(eyeCenter);
        }

        public IList<Rectangle> GetFaces(Image<Bgr, Byte> image)
        {
            var faces = new List<Rectangle>();

            using (var gray = image.Convert<Gray, Byte>())
            {
                gray._EqualizeHist();
                var facesDetected = Face.DetectMultiScale(
                            gray,
                            1.3,
                            5,
                            new Size(20, 20),
                            Size.Empty);
                faces.AddRange(facesDetected);
            }

            return faces;
        }

        public Image<Bgr, Byte> GetRectFromImage(Image<Bgr, Byte> image, Rectangle rectangle)
        {
            return image.GetSubRect(rectangle);
        }

    }
}