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
    interface IAutoSaveRecoverer
    {
        /// <summary>
        /// 获取本地的自动保存文件列表
        /// </summary>
        /// <returns></returns>
        List<AutoSaveIndex.FileInfo> GetLocalAutoSaves();
        /// <summary>
        /// 获取全局的自动保存文件列表
        /// </summary>
        /// <returns></returns>
        List<AutoSaveIndex.FileInfo> GetGlobalAutoSaves();
        /// <summary>
        /// 获取所有的自动保存文件列表 包括本地的和全局的
        /// </summary>
        /// <returns></returns>
        List<AutoSaveIndex.FileInfo> GetAllAutoSaves();
        /// <summary>
        /// 获取指定路径的谱面的信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        FumenInfos GetFumenInfos(string path);
        /// <summary>
        /// 根据recoveredFileInfo恢复文件
        /// </summary>
        /// <param name="recoveredFileInfo"></param>
        /// <returns></returns>
        bool RecoverFile(AutoSaveIndex.FileInfo recoveredFileInfo);
    }
}
