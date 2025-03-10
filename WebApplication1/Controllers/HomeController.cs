using System.Diagnostics;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using Whisper.net;
using Whisper.net.Ggml;
using System.Text.Json;
using Whisper.net.Logger;
using OpenAI;
using OpenAI.Chat;
using System.Text;

namespace WebApplication1.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private StorageClient _storage;

    public HomeController(ILogger<HomeController> logger)
    {
        _storage = StorageClient.Create();
        _logger = logger;
    }

    public String Index()
    {
        return "Hello, World!" + Directory.GetCurrentDirectory();
    }

    [HttpPost]
    public async Task<String> StartSummarize(String fileName)
    {
        // ローカルファイルのパス
        // var localPath = "/Users/takuto/RiderProjects/summarize-conversation/output.mp3";
        // var localPath = "/Users/takuto/RiderProjects/summarize-conversation/" + fileName;
        // バケット名
        var bucketName = "battari-dev.firebasestorage.app";
        // ファイル名
        // var fileName = "01e470a982434fb25598e19cd7742861_313_0.mp4";
        var localPath = Directory.GetCurrentDirectory() + "/" + fileName;
        using var fileStream = System.IO.File.OpenWrite(localPath);
        // storage.UploadObject(bucketName, fileName, null, fileStream);
        // var fileName = "01e470a982434fb25598e19cd7742861_313_0.mp4";
        _storage.DownloadObject(bucketName, fileName, fileStream);
        var ffmpeg = new ProcessStartInfo();

        // ffmpeg.FileName = "/usr/local/bin/ffmpeg";
        // ffmpeg.FileName = "/usr/bin/ffmpeg";
        ffmpeg.FileName = "/opt/homebrew/bin/ffmpeg";
        ffmpeg.Arguments = "-i " + localPath + " -ac 1 -ar 16000 " + localPath + ".wav";
        ffmpeg.UseShellExecute = false;
        ffmpeg.RedirectStandardOutput = true;
        ffmpeg.RedirectStandardError = true;
        var process = Process.Start(ffmpeg);
        process.WaitForExit();
        var keyword = await Summarize(localPath + ".wav");
        try
        {
            var keywordJson = JsonSerializer.Deserialize<Dictionary<String, String>>(keyword);
            var word = keywordJson["word"];
            var sid = fileName.Split("_")[0];
            HttpClient httpClient = new HttpClient();
            using StringContent content = new(
                JsonSerializer.Serialize(new{
                    sid = sid,
                    keyword = word
                }),
                Encoding.UTF8,
                "application/json"
            );
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Environment.GetEnvironmentVariable("MAIN_BACKEND_URL")}/SummarizeConversation/PutSummarizationResult");
            requestMessage.Content = content;

            HttpResponseMessage message = await httpClient.SendAsync(requestMessage);
            try {
                message.EnsureSuccessStatusCode();
            }catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            return word;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return keyword;
        }
    }

    [HttpPost]
    public async Task<String> Summarize(String localPath)
    {
        // We declare three variables which we will use later, ggmlType, modelFileName and wavFileName
        var ggmlType = GgmlType.Base;
        var modelFileName = "ggml-base.bin";
        var wavFileName = localPath;

        // This section detects whether the "ggml-base.bin" file exists in our project disk. If it doesn't, it downloads it from the internet
        if (!System.IO.File.Exists(modelFileName))
        {
            await DownloadModel(modelFileName, ggmlType);
        }

        // Optional logging from the native library
        using var whisperLogger = LogProvider.AddConsoleLogging(WhisperLogLevel.Debug);

        // This section creates the whisperFactory object which is used to create the processor object.
        using var whisperFactory = WhisperFactory.FromPath("ggml-base.bin");

        // This section creates the processor object which is used to process the audio file, it uses language `auto` to detect the language of the audio file.
        using var processor = whisperFactory.CreateBuilder()
                                  .WithLanguage("japanese")
                                  .Build();

        Console.WriteLine(wavFileName);
        using var fileStream = System.IO.File.OpenRead(wavFileName);
        using var outputTxtFileStream = System.IO.File.OpenWrite(wavFileName + ".txt");

        String outputFromWhisper = "";

        // This section processes the audio file and prints the results (start time, end time and text) to the console.
        await foreach (var result in processor.ProcessAsync(fileStream))
        {
            Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
            outputFromWhisper += $"{result.Start}->{result.End}: {result.Text}\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes($"{result.Start}->{result.End}: {result.Text}\n");
            await outputTxtFileStream.WriteAsync(bytes);
        }

        var apiKey = ApiKey.OpenAIKey;
        var model = "gpt-4o-mini";
        ChatClient client = new(model: model, apiKey: apiKey);
        var prompt = $@"
{outputFromWhisper}
この会話をキーワードひとつで表してください．
形式は以下のように
{{
""word"": ""会話内容""
}}
";
        ChatCompletion completion = client.CompleteChat(prompt);
        
        return completion.Content[0].Text;
    }
    private static async Task DownloadModel(string fileName, GgmlType ggmlType)
    {
        Console.WriteLine($"Downloading Model {fileName}");
        // using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(ggmlType);
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(ggmlType);
        using var fileWriter = System.IO.File.OpenWrite(fileName);
        await modelStream.CopyToAsync(fileWriter);
    }
}
