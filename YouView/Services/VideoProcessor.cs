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
        
        
        string root = _env.WebRootPath;
        string ffmpegFolder = Path.Combine(root, "ffmpeg");

        
        if (!Directory.Exists(ffmpegFolder))
        {
            string nested = Path.Combine(root, "wwwroot", "ffmpeg");
            if (Directory.Exists(nested))
            {
                ffmpegFolder = nested;
            }
        }

        // position of file
        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = ffmpegFolder });

        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            try 
            {
                var ffmpegPath = Path.Combine(ffmpegFolder, "ffmpeg");
                var ffprobePath = Path.Combine(ffmpegFolder, "ffprobe");

                // Set permission to 777 (Read/Write/Execute for everyone)
                if (File.Exists(ffmpegPath))
                    File.SetUnixFileMode(ffmpegPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
                
                if (File.Exists(ffprobePath))
                    File.SetUnixFileMode(ffprobePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Linux Permission Fix Failed: {ex.Message}");
            }
        }
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
    

    // generate a thumbnail
    public async Task<bool> GenerateThumbnailAsync(string videoPath, string outputPath)
    {
        try
        {
            // FFMpeg.SnapshotAsync(input, output, size, captureTime)
            // We pass null for size to keep original resolution
            await FFMpeg.SnapshotAsync(videoPath, outputPath, null, TimeSpan.FromSeconds(5));
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
                    .WithAudioCodec("libmp3lame")  // Use MP3
                    .WithAudioBitrate(32)          // 32 kbps (Tiny file size)
                    .WithAudioSamplingRate(16000)  // 16000 Hz
                    .WithCustomArgument("-ac 1")   // Force Mono (1 Audio Channel)
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