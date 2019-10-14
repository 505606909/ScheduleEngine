﻿using M.WorkFlow.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace M.WFEngine.Task.Job
{
    /// <summary>
    /// 不同的任务节点需要执行的不同的任务
    /// </summary>
    public interface IJob
    {
        ETaskType TaskType { get; }
        void Exe(string dataId);
    }
}