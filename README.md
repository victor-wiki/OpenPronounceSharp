This project is C# implementation of [Halleck45/OpenPronounce](https://github.com/Halleck45/OpenPronounce) which can give a score about pronouncication. 

### How to Use

1. Use the tool [PyModelToSharp](https://github.com/victor-wiki/PyModelToSharp) to convert the model **facebook/wav2vec2-large-960h** and **facebook/wav2vec2-lv-60-espeak-cv-ft** to dat format.
2. For **OpenPronounceSharp.ConsoleApp**, it can use the converted dat file directly to get the score.
3. For **OpenPronounceSharp.WinApp**, configure the required options (dat file path must be specified) and then use the **Analyze** to get the score.
4. For **OpenPronounceSharp.Web**, FFMpeg is required to convert webm to wav file, and you can specify the **ConfigFilePath** in **appsettings.json**, the config file is just the file configured in **OpenPronounceSharp.WinApp**. 

