using Emgu.CV;
using Emgu.CV.Structure;

namespace AutoTool.Models
{
    public class MatchTarget
    {
        public string Name { get; set; } = "";
        public Image<Gray, byte> Template { get; set; }
        public ScreenRegion Region { get; set; } = ScreenRegion.Full;
        public double Threshold { get; set; } = 0.8;
    }
}