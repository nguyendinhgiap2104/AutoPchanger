using System.Drawing;
using AutoTool.Models;
using Emgu.CV;
using Emgu.CV.Structure;

namespace AutoTool.Services.Vision
{
    public class ScreenRegionService
    {
        public Rectangle GetRegion(Image<Gray, byte> screen, ScreenRegion regionType)
        {
            int w = screen.Width;
            int h = screen.Height;

            switch (regionType)
            {
                case ScreenRegion.LeftHalf:
                    return new Rectangle(0, 0, w / 2, h);

                case ScreenRegion.RightHalf:
                    return new Rectangle(w / 2, 0, w / 2, h);

                case ScreenRegion.TopHalf:
                    return new Rectangle(0, 0, w, h / 2);

                case ScreenRegion.BottomHalf:
                    return new Rectangle(0, h / 2, w, h / 2);

                case ScreenRegion.BottomRight:
                    return new Rectangle(w / 2, h / 2, w / 2, h / 2);

                case ScreenRegion.BottomLeft:
                    return new Rectangle(0, h / 2, w / 2, h / 2);

                case ScreenRegion.MiddleThird:
                    return new Rectangle(0, h / 3, w, h / 3);

                case ScreenRegion.Full:
                default:
                    return new Rectangle(0, 0, w, h);
            }
        }
    }
}