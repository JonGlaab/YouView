using FFMpegCore;
using FFMpegCore.Enums;
using System.Drawing;

namespace YouView.Services;

public class VideoProcessor
{
    private readonly IWebHostEnvironment _env;

    public VideoProcessor(IWebHostEnvironment env)
    {
        _env = env;
        
        string ffmpegFolder = Path.Combine(_env.WebRootPath, "ffmpeg");

        // position of .exe file
        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegFolder });
    }

    public async Task<TimeSpan> GetVideoDurationAsync(string filePath)
    {
        try 
        {
            var mediaInfo = await FFProbe.AnalyseAsync(filePath);
            return mediaInfo.Duration;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FFProbe Error: {ex.Message}");
            return TimeSpan.Zero;
        }
    }

    public async Task<bool> ProcessVideoUploadAsync(string inputPath, string outputPath)
    {
        try
        {
            // Convert to mp4
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithAudioCodec(AudioCodec.Aac)
                    .WithFastStart()) 
                .ProcessAsynchronously();
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FFMpeg Conversion Error: {ex.Message}");
            return false;
        }
    }
    // generate a thumbnail
    public async Task<bool> GenerateThumbnailAsync(string videoPath, string outputPath)
    {
        try
        {
            await FFMpeg.SnapshotAsync(videoPath, outputPath, new Size(1280, 720), TimeSpan.FromSeconds(5));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Thumbnail generation failed: {ex.Message}");
            return false;
        }
    }
    // extract audio
    public async Task<bool> ExtractAudioAsync(string videoPath, string outputAudioPath)
    {
        try
        {
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(outputAudioPath, true, options => options
                    .WithAudioCodec(AudioCodec.Aac)
                    .DisableChannel(Channel.Video)) 
                .ProcessAsynchronously();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audio extraction failed: {ex.Message}");
            return false;
        }
    }
    // generate preview( 3s)
    public async Task<bool> GenerateGifPreviewAsync(string videoPath, string outputPath)
    {
        try
        {
            await FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(outputPath, true, options => options
                    .Seek(TimeSpan.FromSeconds(10)) 
                    .WithDuration(TimeSpan.FromSeconds(3)) 
                    .WithVideoCodec("gif")
                    .Resize(320, 180) 
                    .WithFramerate(10)) 
                .ProcessAsynchronously();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GIF generation failed: {ex.Message}");
            return false;
        }
    }
}