using FFMpegCore;
using FFMpegCore.Enums;

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
}