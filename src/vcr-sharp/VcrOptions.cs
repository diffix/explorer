namespace VcrSharp
{
    using System;

    public class VcrOptions
    {
        public VCRMode VcrMode { get; set; } = VCRMode.Cache;

        public RecordingOptions RecordingOptions { get; set; } = RecordingOptions.SuccessOnly;

        public Uri HttpClientBaseAddress { get; set; }
    }
}