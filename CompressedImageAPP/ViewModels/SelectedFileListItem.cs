using CommunityToolkit.Mvvm.ComponentModel;

namespace CompressedImageAPP.ViewModels
{
    internal partial class SelectedFileListItem : ObservableObject
    {
        [ObservableProperty]
        private bool _isSelected = false;

        // 添加支持属性变更通知的 CompressedFileSize 字段
        [ObservableProperty]
        private long _compressedFileSize = -1;

        [ObservableProperty]
        private uint[] _compressedSize = [0,0];
        [ObservableProperty]
        private uint _isCompressed = 0;
        public string SourceFileParentFolderName { get; set; } = string.Empty;
        public string SourceFileName { get; set; } = string.Empty;
        public string SourceFilePath { get; set; } = string.Empty;
        public long SourceFileSize { get; set; } = -1;

        public double SourceFileSizeKB => Math.Round(SourceFileSize / 1024.0, 2);
        public string SourceFileKBFormattedSize => SourceFileSizeKB <= 0 ? "--" : $"{SourceFileSizeKB:N2} KB";
        public string CompressedFileName { get; set; } = string.Empty;
        public string CompressedFilePath { get; set; } = string.Empty;
        public string CompressedSizeForDisplay => _compressedSize[0] <= 0 ? "--" : $"{_compressedSize[0]}x{_compressedSize[1]}";
        public string IscompressedForDisplay => _isCompressed switch
        {
            0 => "未压缩",
            1 => "压缩完成",
            2 => "压缩中",
            3 => "压缩出错",
            _ => "未知状态"
            // 可选：处理其他值的情况 _ => "未知状态"
        };

        public string ErrorInfo { get; set; } = "未知错误";

        // 修改为只读属性（基于生成的 _compressedFileSize 字段）
        public double CompressedFileSizeKB => Math.Round(_compressedFileSize / 1024.0, 2);

        // 添加属性变更通知
        public string CompressedFileKBFormattedSize => _compressedFileSize <= 0 ? "--" : $"{CompressedFileSizeKB:N2} KB";

        public MainWindowViewModel? ParentViewModel { get; set; }

        partial void OnIsSelectedChanged(bool value)
        {
            if (ParentViewModel != null && !ParentViewModel._isBulkUpdating)
            {
                ParentViewModel.UpdateAllCheckedState();
            }
        }

        // 新增：当 CompressedFileSize 变化时触发格式化属性的通知
        partial void OnCompressedFileSizeChanged(long value)
        {
            OnPropertyChanged(nameof(CompressedFileKBFormattedSize));
            OnPropertyChanged(nameof(CompressedFileSizeKB));
        }

        partial void OnCompressedSizeChanged(uint[] value)
        {
            OnPropertyChanged(nameof(CompressedSizeForDisplay));
        }

        partial void OnIsCompressedChanged(uint value)
        {
            OnPropertyChanged(nameof(IscompressedForDisplay));
        }
    }
}
