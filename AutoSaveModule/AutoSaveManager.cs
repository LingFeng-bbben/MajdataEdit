/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System.Timers;
using Timer = System.Timers.Timer;

namespace MajdataEdit.AutoSaveModule;

/// <summary>
///     自动保存管理类
///     **单例运行**
///     其提供自动保存行为的计时能力，同时管理IAutoSave实现类的对象
/// </summary>
public sealed class AutoSaveManager
{
    public static readonly int LOCAL_AUTOSAVE_MAX_COUNT = 5;
    public static readonly int GLOBAL_AUTOSAVE_MAX_COUNT = 30;

    private readonly List<IAutoSave> autoSavers = new();

    /// <summary>
    ///     自动保存计时Timer 默认每60秒检查一次
    /// </summary>
    private readonly Timer autoSaveTimer = new(1000 * 60);

    /// <summary>
    ///     自上次保存后，是否产生了修改
    /// </summary>
    private bool isFileChanged;


    /// <summary>
    ///     构造函数
    /// </summary>
    private AutoSaveManager()
    {
        // 本地存储者和全局存储者
        autoSavers.Add(new LocalAutoSave());
        autoSavers.Add(new GlobalAutoSave());

        // 存储事件
        autoSaveTimer.AutoReset = true;
        autoSaveTimer.Elapsed += autoSaveTimer_Elapsed;
    }

    /// <summary>
    ///     获取自动保存Timer间隔
    /// </summary>
    /// <returns></returns>
    public double GetAutoSaveTimerInterval()
    {
        return autoSaveTimer.Interval;
    }

    /// <summary>
    ///     设置自动保存Timer间隔
    /// </summary>
    /// <param name="interval"></param>
    public void SetAutoSaveTimerInterval(double interval)
    {
        autoSaveTimer.Interval = interval;
    }

    /// <summary>
    ///     文件发生了改动
    /// </summary>
    public void SetFileChanged()
    {
        isFileChanged = true;
    }

    /// <summary>
    ///     Timer触发事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void autoSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        // 若文件未改动，则跳过此次自动保存
        if (!isFileChanged) return;

        // 执行保存行为
        foreach (var saver in autoSavers) saver.DoAutoSave();

        // 标记变更已被保存
        isFileChanged = false;
    }

    public void SetAutoSaveEnable(bool enabled)
    {
        if (enabled)
            autoSaveTimer.Start();
        else
            autoSaveTimer.Stop();
    }

    #region Singleton

    private static volatile AutoSaveManager? _instance;
    private static readonly object syncLock = new();

    public static AutoSaveManager Of()
    {
        if (_instance == null)
            lock (syncLock)
            {
                if (_instance == null) _instance = new AutoSaveManager();
            }

        return _instance;
    }

    #endregion
}