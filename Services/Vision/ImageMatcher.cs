using System;
using System.Drawing;
using AutoTool.Models;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace AutoTool.Services.Vision
{
    public class ImageMatcher
    {
        private readonly ScreenRegionService _regionService;

        public ImageMatcher(ScreenRegionService regionService)
        {
            _regionService = regionService;
        }

        public Point? Find(Image<Gray, byte> screen, Image<Gray, byte> template, double threshold = 0.8)
        {
            if (screen == null || template == null)
                return null;

            try
            {
                if (template.Width > screen.Width || template.Height > screen.Height)
                    return null;

                using (var result = screen.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues;
                    double[] maxValues;
                    Point[] minLocations;
                    Point[] maxLocations;

                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    if (maxValues[0] >= threshold)
                    {
                        return new Point(
                            maxLocations[0].X + template.Width / 2,
                            maxLocations[0].Y + template.Height / 2
                        );
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public Point? FindInRegion(
            Image<Gray, byte> screen,
            Image<Gray, byte> template,
            ScreenRegion region,
            double threshold = 0.8)
        {
            if (screen == null || template == null)
                return null;

            Rectangle rect = _regionService.GetRegion(screen, region);
            return FindInRectangle(screen, template, rect, threshold);
        }

        public Point? FindInRectangle(
            Image<Gray, byte> screen,
            Image<Gray, byte> template,
            Rectangle region,
            double threshold = 0.8)
        {
            if (screen == null || template == null)
                return null;

            try
            {
                if (region.Width <= 0 || region.Height <= 0)
                    return null;

                if (template.Width > region.Width || template.Height > region.Height)
                    return null;

                screen.ROI = region;

                using (var result = screen.MatchTemplate(template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues;
                    double[] maxValues;
                    Point[] minLocations;
                    Point[] maxLocations;

                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                    if (maxValues[0] >= threshold)
                    {
                        return new Point(
                            maxLocations[0].X + template.Width / 2 + region.X,
                            maxLocations[0].Y + template.Height / 2 + region.Y
                        );
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                screen.ROI = Rectangle.Empty;
            }

            return null;
        }
    }
}