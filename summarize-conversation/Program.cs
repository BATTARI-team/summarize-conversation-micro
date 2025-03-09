// See https://aka.ms/new-console-template for more information

using Google.Cloud.Storage.V1;

Console.WriteLine("Hello, World!");

// Environment.SetEnvironmentVariable(
//     "GOOGLE_APPLICATION_CREDENTIALS","/Users/takuto/Downloads/battari-dev-5a4baeecc393.json"
//     );

var storage = StorageClient.Create();
var buckets = storage.ListBuckets("battari-dev");

//バケットリストの表示
foreach (var bucket in buckets)
{
    Console.WriteLine(bucket.Name);
}

// ローカルファイルのパス
var localPath = "/Users/takuto/RiderProjects/summarize-conversation/output.mp3";
// バケット名
var bucketName = "battari-dev.firebasestorage.app";
// ファイル名
var fileName = "01e470a982434fb25598e19cd7742861_313_0.mp4";
using var fileStream = File.OpenWrite(localPath);
// storage.UploadObject(bucketName, fileName, null, fileStream);
storage.DownloadObject(bucketName, fileName, fileStream);