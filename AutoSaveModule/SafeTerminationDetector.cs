/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.IO;

namespace MajdataEdit.AutoSaveModule;

/// <summary>
///     异常退出检测器
///     单例运行，生命周期等同于Edit
/// </summary>
public sealed class SafeTerminationDetector
{
    public readonly string RecordPath = Environment.CurrentDirectory + "/PROGRAM_RUNNING";

    private SafeTerminationDetector()
    {
    }

    /// <summary>
    ///     检查上一次退出是否为正常退出
    /// </summary>
    /// <returns>如果上次为正常退出则返回true，否则返回false</returns>
    public bool IsLastTerminationSafe()
    {
        if (File.Exists(RecordPath)) return false;

        return true;
    }

    /// <summary>
    ///     启动程序时，调用此函数
    ///     **注意！需在IsLastTerminationSafe之前调用！**
    /// </summary>
    public void RecordProgramStart()
    {
        File.WriteAllText(RecordPath, "");
    }

    public void ChangePath(string path)
    {
        File.WriteAllText(RecordPath, path);
    }

    /// <summary>
    ///     退出程序时，调用此函数
    /// </summary>
    public void RecordProgramClose()
    {
        File.Delete(RecordPath);
    }

    #region Singleton

    private static readonly SafeTerminationDetector _instance = new();

    public static SafeTerminationDetector Of()
    {
        return _instance;
    }

    #endregion
}