using NumSharp;

namespace OpenPronounceSharp.Model
{
    /// <summary>
    /// Phones recognized in a waveform, with the frame posteriors they were decoded from.
    /// 
    /// ``phones`` are normalized, ``confidences`` (0-1) is the peak posterior of each
    /// phone over the frames it was decoded from, ``spans`` the ``(start, end)`` frame
    /// range of each phone, ``log_posteriors`` the ``(frames, vocab)`` log posteriors of
    /// the model and ``vocab`` its tokens indexed by id.
    /// </summary>
    public class PhoneRecognition
    {
        public List<string> phones { get; set; }
        public List<float> confidences { get; set; }
        public List<(int,int)> spans { get; set; }
        public NDArray log_posteriors { get; set; }
        public List<string> vocab { get; set; }
    }
}
