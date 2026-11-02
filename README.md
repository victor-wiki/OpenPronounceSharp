This project is C# implementation of [Halleck45/OpenPronounce](https://github.com/Halleck45/OpenPronounce) which can give a score about pronouncication. 

### How to Use

1. Use the project **OpenPronounceSharp.ConsoleApp** to download the model from huggingface.
2. Use the tool [PyModelToSharp](https://github.com/victor-wiki/PyModelToSharp) to convert the model file to dat format.
3. For **OpenPronounceSharp.WinApp**, configure options (dat file path must be specified) and then use the **Analyze** to get the score.
4. For **OpenPronounceSharp.Web**, FFMpeg is required to convert webm to wav file, and you can specify the **ConfigFilePath** in **appsettings.json**, the config file is just the file configured in **OpenPronounceSharp.WinApp**. 

