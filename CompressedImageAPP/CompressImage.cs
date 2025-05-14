using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick.Formats;
using ImageMagick;
using CompressedImageAPP.ViewModels;

namespace CompressedImageAPP
{
    internal static class CompressImage
    {
        public static async Task<string> CompressAndConvertImageFromOneFormatToAnotherAsync(
            uint Quality,
            string OutputFormat,
            string ImagePath,
            string OutputFolder,
            CancellationToken cancellationToken) // 添加 CancellationToken 参数
        {
            return await Task.Run(() =>
            {
                string outputPath = string.Empty;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Directory.CreateDirectory(OutputFolder);

                    string outputFileName = $"{Path.GetFileNameWithoutExtension(ImagePath)}.{OutputFormat.ToLower()}";
                    outputPath = Path.Combine(OutputFolder, outputFileName);

                    cancellationToken.ThrowIfCancellationRequested();

                    using var image = new MagickImage(ImagePath);
                    image.Quality = Quality;

                    cancellationToken.ThrowIfCancellationRequested();

                    switch (OutputFormat.ToLower())
                    {
                        case "jpg":
                            image.Format = MagickFormat.Jpeg;
                            image.Settings.SetDefines(new JpegWriteDefines()
                            {
                                OptimizeCoding = true,
                                SamplingFactor = JpegSamplingFactor.Ratio420,
                            });
                            break;
                        case "webp":
                            image.Format = MagickFormat.WebP;
                            image.Settings.SetDefines(new WebPWriteDefines()
                            {
                                Method = 6,
                                Lossless = false,
                                AlphaQuality = 90
                            });
                            break;
                        default:
                            image.Format = MagickFormat.WebP;
                            image.Settings.SetDefines(new WebPWriteDefines()
                            {
                                Method = 6,
                                Lossless = false,
                                AlphaQuality = 90
                            });
                            break;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // 可选：根据需要取消注释以下优化
                    // image.AutoOrient();
                    // image.Strip();

                    image.Write(outputPath);
                    return outputPath;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("操作已被取消。");

                    return string.Empty;
                }
                catch (MagickException ex)
                {
                    Console.WriteLine($"图像处理错误: {ex.Message}");

                    return string.Empty;
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"文件操作错误: {ex.Message}");

                    return string.Empty;
                }
            }, cancellationToken); // 传递 CancellationToken 到 Task.Run
        }

        internal static void CompressAndConvertImageFromOneFormatToAnotherAsync1(SelectedFileListItem item, uint imageQuality, string outputFormat1, string outputFormat2, bool isAutoCreateFolder, bool isAutoRename, bool isOutputToSourceFolder, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
