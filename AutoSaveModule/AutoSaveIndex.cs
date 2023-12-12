/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

namespace MajdataEdit.AutoSaveModule;

/// <summary>
///     自动保存索引 用于索引当前环境中自动保存的文件
/// </summary>
public class AutoSaveIndex
{
    /// <summary>
    ///     已存在的自动保存文件数量
    /// </summary>
    public int Count = 0;

    /// <summary>
    ///     自动保存文件列表
    /// </summary>
    public List<FileInfo> FilesInfo = new();

    public class FileInfo
    {
        /// <summary>
        ///     自动保存文件名
        /// </summary>
        public string? FileName;

        /// <summary>
        ///     原先的文件路径
        /// </summary>
        public string? RawPath;

        /// <summary>
        ///     自动保存时间
        /// </summary>
        public long SavedTime;
    }
}