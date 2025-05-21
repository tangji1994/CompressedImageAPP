using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Win32;

namespace CompressedImageAPP.ViewModels
{
    internal partial class MainWindowViewModel : ObservableObject
    {
        private readonly StringBuilder LogErrorMessage = new();
        public bool _isBulkUpdating = false;
        private readonly HashSet<string> _existingPathsCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Lock _collectionLock = new();
        private CancellationTokenSource? _cts;
        //[ObservableProperty]
        //private bool _isCanStart = false;
        [ObservableProperty]
        private bool _isProgressing = false;
        
        [ObservableProperty]
        private string _fileListInfoMessage = string.Empty;
        [ObservableProperty]
        private string _infoMessage = string.Empty;
        [ObservableProperty]
        private string _ErrFileListInfoMessage = string.Empty;
        [ObservableProperty]
        private string _startInfoMessage = string.Empty;
        [ObservableProperty]
        private ObservableCollection<SelectedFileListItem> _selectedFileListItems = [];
        [ObservableProperty]
        private ObservableCollection<SelectedFileListItem> _errorFileListItems = [];
        [ObservableProperty]
        private bool _fileListItemAllChecked = false;
        [ObservableProperty]
        private uint _imageQuality = 100;
        [ObservableProperty]
        private uint _imageZoomPercentage = 100;
        [ObservableProperty]
        private uint _imageWidth = 248;
        [ObservableProperty]
        private uint _imageHeight = 186;
        [ObservableProperty]
        private bool _isZoomByScale = false;
        [ObservableProperty]
        private bool _isZoomBySize = true;
        [ObservableProperty]
        private bool _isZoomByPercentage = true;
        [ObservableProperty]
        private bool _isZoomByWidth = false;
        [ObservableProperty]
        private bool _isZoomByHeight = false;
        [ObservableProperty]
        private bool _isOutputToSourceFolder = false;
        [ObservableProperty]
        private string _outputFormat = "jpg";
        [ObservableProperty]
        private ObservableCollection<OutputFormatOption> _outputFormatOptions =
        [ 
            new() { DisplayName = "输出为jpg（兼容性好）", Value = "jpg" },
            //new() { DisplayName = "输出为png", Value = "png" },
            new() { DisplayName = "输出为webp（压缩率高）", Value = "webp" },
        ];
        [ObservableProperty]
        private string _outputFolder = string.Empty;
        [ObservableProperty]
        private string _outputFolderInfoMessage = string.Empty;
        [ObservableProperty]
        private bool _isAutoRename = true;
        [ObservableProperty]
        private bool _isAutoCreateFolder = true;
        [ObservableProperty]
        private double _progressBarValue = 0;
        private int _totalFiles;
        private int _processedFiles;

        [ObservableProperty]
        private uint _imageRotateAngle = 0;
        [ObservableProperty]
        private bool _isAutoStretchingBackground = true;
        [ObservableProperty]
        private uint _imageDPI = 72;

        partial void OnIsOutputToSourceFolderChanged(bool value)
        {
            if (value)
            {
                StartInfoMessage = string.Empty;
            }
        }

        partial void OnOutputFolderChanged(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                OutputFolderInfoMessage = $"当前输出目录：{value}";
            }
        }


        [RelayCommand]
        private void AddFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "选择文件",
                Filter = "所有文件 (*.*)|*.*",
                Multiselect = true,
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() != true) return;
            AddFilesToCollection(openFileDialog.FileNames);
            StartInfoMessage = string.Empty;
        }

        [RelayCommand]
        private void AddFolder()
        {
            var openFolderDialog = new OpenFolderDialog()
            {
                Title = "选择文件夹",
                Multiselect = true,
            };
            if (openFolderDialog.ShowDialog() != true) return;
            AddFilesToCollection(TraverseDirectoryGetFilesParallel(openFolderDialog.FolderNames));
            StartInfoMessage = string.Empty;
        }

        private List<string> TraverseDirectoryGetFilesParallel(string[] folderNames)
        {
            var allFiles = new ConcurrentBag<string>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(folderNames, options, item =>
            {
                try
                {
                    var fileInfo = new FileInfo(item);

                    // 权限检查
                    try { fileInfo.GetAccessControl(); }
                    catch (UnauthorizedAccessException)
                    {
                        return;
                    }

                    if (fileInfo.Attributes.HasFlag(FileAttributes.Directory))
                    {
                        // 并行处理当前目录文件
                        try
                        {
                            Parallel.ForEach(Directory.EnumerateFiles(item), file =>
                            {
                                allFiles.Add(file);
                            });
                        }
                        catch (UnauthorizedAccessException)
                        {
                            return;
                        }

                        // 递归处理子目录
                        try
                        {
                            var subDirs = Directory.EnumerateDirectories(item)
                                                  .AsParallel()
                                                  .WithDegreeOfParallelism(4);
                            allFiles.AddRange(TraverseDirectoryGetFilesParallel(subDirs.ToArray()));
                        }
                        catch (UnauthorizedAccessException)
                        {
                        }
                    }
                    else
                    {
                        allFiles.Add(item);
                    }
                }
                catch
                {
                }
            });

            return allFiles.ToList();
        }


        [RelayCommand]
        private void RemoveSelectedFile()
        {
            try
            {
                _existingPathsCache.Clear();
                // 使用LINQ筛选出要保留的项目，避免直接修改集合
                var itemsToKeep = SelectedFileListItems
                    .Where(item => !item.IsSelected)
                    .ToList();

                // 清除原集合并添加保留的项目
                SelectedFileListItems.Clear();
                SelectedFileListItems.AddRange(itemsToKeep);

                // 更新缓存
                foreach (var path in itemsToKeep.Select(i => i.SourceFilePath))
                {
                    _existingPathsCache.Add(path);
                }

                FileListInfoMessage = $"移除文件已经完成，当前剩余文件总计: {SelectedFileListItems.Count}个";
                UpdateAllCheckedState();
            }
            finally
            {
                // 强制垃圾回收（谨慎使用）
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        [RelayCommand]
        private void SetImageRotateAngle0() {
            ImageRotateAngle = 0;
        }
        [RelayCommand]
        private void SetImageRotateAngle90() {
            ImageRotateAngle = 90;
        }
        [RelayCommand]
        private void SetImageRotateAngle180() {
            ImageRotateAngle = 180;
        }
        [RelayCommand]
        private void SetImageRotateAngle270() {
            ImageRotateAngle = 270;
        }

        [RelayCommand]
        private void SetOutputFolder()
        {
            var openFolderDialog = new OpenFolderDialog()
            {
                Title = "选择输出目录",
                Multiselect = false,
            };
            if (openFolderDialog.ShowDialog() != true) return;
            OutputFolder = openFolderDialog.FolderName;
            StartInfoMessage = string.Empty;
        }

        [RelayCommand]
        private async Task StartCompressedImage()
        {
            if (SelectedFileListItems.Count == 0)
            {
                StartInfoMessage = "请添加要处理的文件";
                return;
            }
            if (!IsOutputToSourceFolder)
            {
                if(string.IsNullOrEmpty(OutputFolder)){
                    StartInfoMessage = "请设置输出目录用于保存处理好的文件";
                    return;
                }
            }
            StartInfoMessage = string.Empty;
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            ErrorFileListItems.Clear();
            ErrFileListInfoMessage = string.Empty;
            ProgressBarValue = 0;
            _processedFiles = 0;
            _totalFiles = SelectedFileListItems.Count;
            IProgress<ProgressReport> progress = new Progress<ProgressReport>(report =>
            {
                switch (report.Type)
                {
                    case ProgressType.FileStart:
                        InfoMessage = $"正在处理：{ report.CurrentFileName}";
                        break;
                    case ProgressType.FileComplete:
                        Interlocked.Increment(ref _processedFiles);
                        ProgressBarValue = (double)_processedFiles / _totalFiles * 100;
                        FileListInfoMessage = $"已处理 {_processedFiles}/{_totalFiles} 个文件。{ErrorFileListItems.Count}个文件出现错误";
                        break;
                    case ProgressType.Error:
                        LogErrorMessage
                            .AppendLine($"{DateTime.Now.ToString("F")}")
                            .AppendLine($"处理{report.CurrentFileName}时错误：{report.Error?.Message}");
                        break;
                }
            });
            IsProgressing = true;
            try
            {
                await Parallel.ForEachAsync(SelectedFileListItems,
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount/2,
                        CancellationToken = _cts.Token
                    },
                    async (item, token) =>
                    {
                            await Task.Run(() =>
                            {
                                try
                                {
                                    progress.Report(new ProgressReport(ProgressType.FileStart, item.SourceFilePath));
                                    item.IsCompressed = 2;
                                    CompressAndConvertImageFromOneFormatToAnother(item);
                                    item.IsCompressed = 1;
                                }
                                catch(OperationCanceledException)
                                {
                                    _cts.Cancel();
                                }
                                catch (AuthenticationException)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        item.ErrorInfo = "文件权限不足";
                                        ErrorFileListItems.Add(item);
                                        ErrFileListInfoMessage = $"一共发生{ErrorFileListItems.Count}个错误";
                                    });
                                    item.IsCompressed = 3;
                                }
                                catch (MagickMissingDelegateErrorException)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        item.ErrorInfo = "格式不支持";
                                        ErrorFileListItems.Add(item);
                                        ErrFileListInfoMessage = $"一共发生{ErrorFileListItems.Count}个错误";
                                    });
                                    item.IsCompressed = 3;
                                }
                                catch(Exception e)
                                { 
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        item.ErrorInfo = "未知错误";
                                        ErrorFileListItems.Add(item);
                                        ErrFileListInfoMessage = $"一共发生{ErrorFileListItems.Count}个错误";
                                    });
                                    item.IsCompressed = 3;
                                    Debug.WriteLine(e.Message);
                                }
                                finally
                                {
                                    progress.Report(new ProgressReport(ProgressType.FileComplete, item.SourceFileName));
                                }
                            }
                            ,token);
                    });
            }
            catch (OperationCanceledException)
            {
               // InfoMessage = "取消操作";
                _cts.Cancel();
            }
            finally
            {
                IsProgressing = false;
                InfoMessage = string.Empty;
            }
        }

        private void CompressAndConvertImageFromOneFormatToAnother(SelectedFileListItem item)
        {
            string ImagePath = item.SourceFilePath;
            string outputFileName = $"{Path.GetFileNameWithoutExtension(ImagePath)}.{OutputFormat.ToLower()}";
            using var image = new MagickImage(ImagePath);
            image.AutoOrient();
            image.Quality = ImageQuality;
            image.Density = new Density(ImageDPI);
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
            }
            string outputFolder;
            if (IsOutputToSourceFolder)
            {
                outputFolder = Path.GetDirectoryName(ImagePath) ?? string.Empty;
            }
            else
            {
                if (IsAutoCreateFolder)
                {
                    outputFolder = Path.Combine(OutputFolder, item.SourceFileParentFolderName);
                }
                else
                {
                    outputFolder = OutputFolder;
                }
            }
            MagickGeometry magickGeometry = new();
            if (IsZoomByScale)
            {
                if (IsZoomByPercentage)
                {
                    magickGeometry = new MagickGeometry($"{ImageZoomPercentage}%");
                }
                if (IsZoomByHeight)
                {
                    magickGeometry = new MagickGeometry($"x{ImageHeight}");
                }
                if (IsZoomByWidth)
                {
                    magickGeometry = new MagickGeometry($"{ImageWidth}x");
                }
                image.Resize(magickGeometry);
            }
            if (IsZoomBySize)
            {
                if (IsAutoStretchingBackground && image.Width > 0 && ImageWidth > 0)
                {
                    if((decimal)image.Height/ (decimal)image.Width >= (decimal)ImageHeight / (decimal)ImageWidth)
                    {
                        magickGeometry = new MagickGeometry($"x{ImageHeight}");
                    }
                    else
                    {
                        magickGeometry = new MagickGeometry($"{ImageWidth}x");
                    }
                    image.Resize(magickGeometry);
                    image.Extent(ImageWidth, ImageHeight, Gravity.Center, MagickColors.White);
                }
                else
                {
                    magickGeometry = new MagickGeometry($"{ImageWidth}x{ImageHeight}!");
                    image.Resize(magickGeometry);
                }
            }
            

            string outputPath = Path.Combine(outputFolder, outputFileName);
            if (File.Exists(outputPath) && IsAutoRename)
            {
                outputPath = Path.Combine(outputFolder, $"{Path.GetFileNameWithoutExtension(outputPath)}_{DateTime.Now.ToString("yyyyMMddHHmmssffff")}.{OutputFormat.ToLower()}");
            }
            Directory.CreateDirectory(outputFolder);

            image.Rotate(ImageRotateAngle);
            image.Write(outputPath);
            var fileInfo = new FileInfo(outputPath);
            item.CompressedFilePath = fileInfo.FullName;
            item.CompressedFileName = fileInfo.Name;
            item.CompressedFileSize = fileInfo.Length;
            item.CompressedSize = [image.Width, image.Height];
        }

        [RelayCommand]
        private void StopCompressedImage()
        {
            _cts?.Cancel();
            //_semaphore?.Dispose();
            InfoMessage = "正在停止...";
        }

        private void AddFilesToCollection(IEnumerable<string> filePaths)
        {
            // 阶段1：快速路径过滤（无锁读取）
            var initialPaths = filePaths
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (initialPaths.Count == 0) return;

            // 阶段2：带锁的路径去重检查
            List<string> filteredPaths;
            lock (_collectionLock)
            {
                filteredPaths = initialPaths
                    .Where(path => !_existingPathsCache.Contains(path))
                    .ToList();

                if (filteredPaths.Count == 0)
                {
                    FileListInfoMessage = $"新增 {filteredPaths.Count} 个文件，新选择的文件全部已经存在；当前总计文件: {SelectedFileListItems.Count} 个";
                    return;
                }
            }

            // 阶段3：并行文件信息处理
            var newItems = new ConcurrentBag<SelectedFileListItem>();
            var exceptions = new ConcurrentQueue<Exception>();

            Parallel.ForEach(filteredPaths, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, path =>
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    if (!fileInfo.Exists) return;

                    var item = new SelectedFileListItem
                    {
                        ParentViewModel = this,
                        SourceFilePath = fileInfo.FullName,
                        SourceFileName = fileInfo.Name,
                        SourceFileSize = fileInfo.Length,
                        IsSelected = FileListItemAllChecked,
                        IsCompressed = 0,
                        SourceFileParentFolderName = fileInfo.Directory?.Name ?? string.Empty
                    };

                    newItems.Add(item);
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(new InvalidOperationException($"文件加载失败: {path}", ex));
                    LogErrorMessage.AppendLine($"{DateTime.Now.ToString("F")}")
                    .AppendLine($"文件加载失败: {path}, {ex}");
                    SaveErrorLog();
                }
            });

            // 处理异常
            if (!exceptions.IsEmpty)
            {
                LogErrorMessage
                    .AppendLine($"{DateTime.Now.ToString("F")}")
                    .AppendLine($"遇到 {exceptions.Count} 个错误：")
                    .AppendJoin(Environment.NewLine, exceptions.Select(e => e.Message));
                SaveErrorLog();
            }

            // 阶段4：原子性批量添加
            lock (_collectionLock)
            {
                var finalItems = newItems
                    .Where(item => !_existingPathsCache.Contains(item.SourceFilePath))
                    .ToList();

                if (finalItems.Count == 0)
                {
                    FileListInfoMessage = $"新增 {finalItems.Count} 个文件，新选择的文件全部已经存在；当前总计文件: {SelectedFileListItems.Count} 个";
                    return;
                }

                // 使用扩展方法批量添加
                SelectedFileListItems.AddRange(finalItems);

                // 更新缓存
                foreach (var path in finalItems.Select(i => i.SourceFilePath))
                {
                    _existingPathsCache.Add(path);
                }
                FileListInfoMessage = $"新增 {finalItems.Count} 个文件；当前总计文件: {SelectedFileListItems.Count} 个";
            }
        }



        [RelayCommand]
        private void ToggleAllChecked()
        {
            FileListItemAllChecked = !FileListItemAllChecked;
            _isBulkUpdating = true;// 批量更新时设置标志位
            foreach (var item in SelectedFileListItems)
            {
                item.IsSelected = FileListItemAllChecked;
            }
            _isBulkUpdating = false; // 批量操作结束后重置标志位
            // 手动更新全选状态
            UpdateAllCheckedState();
        }

        public void UpdateAllCheckedState()
        {
            if (SelectedFileListItems.Count == 0)
            {
                FileListItemAllChecked = false;
                return;
            }

            var selectedCount = SelectedFileListItems.Count(i => i.IsSelected);
            FileListItemAllChecked = selectedCount == SelectedFileListItems.Count;
        }



        private void SaveErrorLog()
        {
            try
            {
                // 创建日志目录
                var logDir = "logs";
                Directory.CreateDirectory(logDir);  // 自动创建不存在的目录

                // 生成带日期的文件名
                var date = DateTime.Now.ToString("yyyy-MM-dd");
                var fileName = $"error_{date}.log";
                var filePath = System.IO.Path.Combine(logDir, fileName);

                // 写入文件（追加模式）
                File.AppendAllText(filePath, LogErrorMessage.ToString());

                // 清空当前日志缓存
                LogErrorMessage.Clear();
            }
            catch (Exception ex)
            {
                // 建议至少记录写入失败的情况（这里简单处理）
                Debug.WriteLine($"日志写入失败: {ex.Message}");
            }
        }

        public enum ProgressType { FileStart, FileComplete, Error }
        public record ProgressReport(
            ProgressType Type,
            //int Current,
            //int Total,
            string CurrentFileName,
            //double Percentage,
            Exception? Error = null
            );
    }


    public class OutputFormatOption
    {
        public required string DisplayName { get; set; }
        public required string Value { get; set; }
    }
    public static class ObservableCollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }
    }

    public static class ConcurrentBagExtensions
    {
        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                bag.Add(item);
            }
        }
    }
}
