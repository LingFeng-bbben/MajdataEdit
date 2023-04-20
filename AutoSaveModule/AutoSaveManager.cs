/*
  Copyright (c) Moying-moe All rights reserved. Licensed under the MIT license.
  See LICENSE in the project root for license information.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace MajdataEdit.AutoSaveModule
{
    /// <summary>
    /// 自动保存管理类
    /// **单例运行**
    /// 其提供自动保存行为的计时能力，同时管理IAutoSave实现类的对象
    /// </summary>
    public sealed class AutoSaveManager
    {
        #region Singleton
        private static volatile AutoSaveManager _instance;
        private static object syncLock = new object();
        public static AutoSaveManager Of()
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new AutoSaveManager();
                    }
                }
            }
            return _instance;
        }
        #endregion

        private List<IAutoSave> autoSavers = new List<IAutoSave>();

        /// <summary>
        /// 自动保存计时Timer 默认每60秒检查一次
        /// </summary>
        private Timer autoSaveTimer = new Timer(1000 * 60);

        /// <summary>
        /// 自上次保存后，是否产生了修改
        /// </summary>
        private bool isFileChanged = false;

        public static readonly int LOCAL_AUTOSAVE_MAX_COUNT = 5;
        public static readonly int GLOBAL_AUTOSAVE_MAX_COUNT = 30;


        /// <summary>
        /// 构造函数
        /// </summary>
        private AutoSaveManager() {
            // 本地存储者和全局存储者
            this.autoSavers.Add(new LocalAutoSave());
            this.autoSavers.Add(new GlobalAutoSave());

            // 存储事件
            this.autoSaveTimer.AutoReset = true;
            this.autoSaveTimer.Elapsed += autoSaveTimer_Elapsed;
        }

        /// <summary>
        /// 获取自动保存Timer间隔
        /// </summary>
        /// <returns></returns>
        public double GetAutoSaveTimerInterval()
        {
            return this.autoSaveTimer.Interval;
        }

        /// <summary>
        /// 设置自动保存Timer间隔
        /// </summary>
        /// <param name="interval"></param>
        public void SetAutoSaveTimerInterval(double interval)
        {
            this.autoSaveTimer.Interval = interval;
        }

        /// <summary>
        /// 文件发生了改动
        /// </summary>
        public void SetFileChanged()
        {
            this.isFileChanged = true;
        }

        /// <summary>
        /// Timer触发事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void autoSaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 若文件未改动，则跳过此次自动保存
            if (!this.isFileChanged)
            {
                return;
            }

            // 执行保存行为
            foreach (IAutoSave saver in this.autoSavers)
            {
                saver.DoAutoSave();
            }

            // 标记变更已被保存
            this.isFileChanged = false;
        }

        public void SetAutoSaveEnable(bool enabled)
        {
            if (enabled)
            {
                this.autoSaveTimer.Start();
            }
            else
            {
                this.autoSaveTimer.Stop();
            }
        }
    }
}
