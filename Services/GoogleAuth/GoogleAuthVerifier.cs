using System;
using System.Drawing;
using AutoTool.Models;
using AutoTool.Services.Vision;
using Emgu.CV;
using Emgu.CV.Structure;

namespace AutoTool.Services.GoogleAuth
{
    public class GoogleAuthVerifier
    {
        private readonly ScreenCaptureService _screenCapture;
        private readonly ImageMatcher _imageMatcher;
        private readonly OcrTextFinder _ocr;
        private readonly GoogleAuthTemplateStore _templates;
        private readonly GoogleAuthOptions _options;

        public GoogleAuthVerifier(
            ScreenCaptureService screenCapture,
            ImageMatcher imageMatcher,
            OcrTextFinder ocr,
            GoogleAuthTemplateStore templates,
            GoogleAuthOptions options)
        {
            _screenCapture = screenCapture;
            _imageMatcher = imageMatcher;
            _ocr = ocr;
            _templates = templates;
            _options = options;
        }

        public byte[] CaptureBytes(string deviceId, Action<string> log = null)
        {
            return _screenCapture.GetScreenBytes(deviceId, log);
        }

        public Image<Gray, byte> CaptureMat(string deviceId, Action<string> log = null)
        {
            return _screenCapture.GetScreenMat(deviceId, log);
        }
        public Point? FindLaterButton(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.Later,
                ScreenRegion.BottomLeft,
                _options.DefaultImageThreshold);
        }
        public Point? FindBackupFeatureScreen(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.BackupFeature,
                ScreenRegion.Full,
                0.65);
        }
        public Point? FindSkip(Image<Gray, byte> screen)
        {
            foreach (var template in _templates.GetSkipTemplates())
            {
                if (template == null) continue;

                Point? point = _imageMatcher.FindInRegion(
                    screen,
                    template,
                    ScreenRegion.LeftHalf,
                    _options.DefaultImageThreshold);

                if (point != null)
                    return point;
            }

            return null;
        }

        public Point? FindAgree(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.Agree,
                ScreenRegion.BottomRight,
                _options.AgreeImageThreshold);
        }

        public Point? FindCancel(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.Cancel,
                ScreenRegion.BottomLeft,
                _options.DefaultImageThreshold);
        }

        public Point? FindRecoveryEmail(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.RecoveryEmail,
                ScreenRegion.Full,
                _options.DefaultImageThreshold);
        }

        public Point? FindSeeMore(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.SeeMore,
                ScreenRegion.BottomHalf,
                _options.DefaultImageThreshold);
        }

        public Point? FindAccept(Image<Gray, byte> screen)
        {
            return _imageMatcher.FindInRegion(
                screen,
                _templates.Accept,
                ScreenRegion.BottomHalf,
                _options.DefaultImageThreshold);
        }

        public Point? FindText(byte[] screenBytes, string text, float confidence = 0.6f)
        {
            return _ocr.FindText(screenBytes, text, confidence);
        }

        public bool IsCreateTextVisible(byte[] screenBytes)
        {
            return FindText(screenBytes, "tạo", _options.DefaultOcrConfidence) != null;
        }

        public bool IsDeadEmailScreen(byte[] screenBytes)
        {
            return FindText(screenBytes, "thử cách khác", _options.DeadEmailOcrConfidence) != null;
        }

        public bool IsPasswordScreen(byte[] screenBytes)
        {
            return FindText(screenBytes, "mật khẩu", _options.DefaultOcrConfidence) != null;
        }

        public Point? FindAgreeByText(byte[] screenBytes)
        {
            return FindText(screenBytes, "đồng ý", _options.DefaultOcrConfidence);
        }
    }
}