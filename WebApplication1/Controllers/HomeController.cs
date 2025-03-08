using System.Diagnostics;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;

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
        return "Hello, World!";
    }

    [HttpPost]
    public String StartSummarize(String fileName)
    {
        // ローカルファイルのパス
        // var localPath = "/Users/takuto/RiderProjects/summarize-conversation/output.mp3";
        // var localPath = "/Users/takuto/RiderProjects/summarize-conversation/" + fileName;
// バケット名
        var bucketName = "battari-dev.firebasestorage.app";
// ファイル名
        // var fileName = "01e470a982434fb25598e19cd7742861_313_0.mp4";
    var localPath = "/Users/takuto/RiderProjects/summarize-conversation/" + fileName;
        using var fileStream = System.IO.File.OpenWrite(localPath);
        // storage.UploadObject(bucketName, fileName, null, fileStream);
// var fileName = "01e470a982434fb25598e19cd7742861_313_0.mp4";
// #TODO
        // _storage.DownloadObject(bucketName, fileName, fileStream);
        var ffmpeg = new ProcessStartInfo();
        
        // ffmpeg.FileName = "/usr/local/bin/ffmpeg";
        ffmpeg.FileName = "/opt/homebrew/bin/ffmpeg";
        ffmpeg.Arguments = "-i " + localPath +" " + localPath + ".mp3";
        ffmpeg.UseShellExecute = false;
        ffmpeg.RedirectStandardOutput = true;
        ffmpeg.RedirectStandardError = true;
        var process = Process.Start(ffmpeg);
        process.WaitForExit();
        return "";
    }
    
}