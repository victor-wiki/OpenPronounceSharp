namespace OpenPronounceSharp.Model
{
    public record Language
    {
        public string code { get; set; }
        public string espeak { get; set; }
        public string asr_model { get; set; }
        public string name { get; set; }

        /// <summary>
        /// Mean per-frame DTW distance between the Wav2Vec2 embeddings of two good native
        /// voices of this language (gTTS vs Piper on three sentences, English checkpoint).
        /// The acoustic term of the score maps this value to 100 and ``acoustic_good + 9`` to 0.
        /// English is calibrated on human readings and speechocean762 (see benchmarks/);
        /// for the other languages the English embeddings put even two native voices
        /// further apart, hence the offset.
        /// </summary>
        public float acoustic_good { get; set; } = 6.0f;
    }
}
