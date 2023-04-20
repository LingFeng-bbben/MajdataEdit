/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MajdataEdit.AutoSaveModule
{
    /// <summary>
    /// 自动保存索引文件的管理接口
    /// </summary>
    interface IAutoSaveIndexManager
    {
        /// <summary>
        /// 修改当前工作路径
        /// </summary>
        /// <param name="path"></param>
        void ChangePath(string path);

        /// <summary>
        /// 获取最多可以存储多少个自动保存文件
        /// </summary>
        /// <returns></returns>
        int GetMaxAutoSaveCount();

        /// <summary>
        /// 设置最多可以存储多少个自动保存文件
        /// </summary>
        /// <param name="maxAutoSaveCount"></param>
        void SetMaxAutoSaveCount(int maxAutoSaveCount);

        /// <summary>
        /// 获取索引文件管理器是否已经就绪
        /// </summary>
        /// <returns>若已经就绪则返回true</returns>
        bool IsReady();

        /// <summary>
        /// 获取一个新的自动保存文件名
        /// </summary>
        /// <returns></returns>
        string GetNewAutoSaveFileName();

        /// <summary>
        /// 刷新并维护索引。如果当前已经存储的文件数量超出了最大限制，则删除过时的自动保存文件
        /// </summary>
        void RefreshIndex();

        /// <summary>
        /// 获取当前存在多少个自动保存文件
        /// </summary>
        /// <returns></returns>
        int GetFileCount();

        /// <summary>
        /// 获取当前的自动保存文件信息列表
        /// </summary>
        /// <returns></returns>
        List<AutoSaveIndex.FileInfo> GetFileInfos();
    }
}
