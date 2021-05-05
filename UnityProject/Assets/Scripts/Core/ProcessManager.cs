using System;
using System.Collections.Generic;
using UnityEngine;

public class ProcessManager
{
    //虚拟进程枚举ID
    public enum ProcessID
    {
        Game,
    }

    public MainLoopProcess currentMainLoop;         //主循环Process
    public List<MainLoopProcess> sideProcesses;     //辅助Process
    public ProcessID? upcomingProcess;              //即将切换的目标进程

    public ProcessManager()
    {
        this.sideProcesses = new List<MainLoopProcess>();

        Debug.Log("init main process ：Game");
        this.SwitchMainProcess(ProcessManager.ProcessID.Game);
    }

    public void Update(float dt)
    {
        //所有Process.RawUpdate
        this.currentMainLoop.RawUpdate(dt);
        foreach (var process in sideProcesses) 
        {
            process.RawUpdate(dt);
        }
            
        //检测切换MainProcess任务
        if (this.upcomingProcess.HasValue)
        {
            this.SwitchMainProcess(this.upcomingProcess.Value);
            this.upcomingProcess = new ProcessManager.ProcessID?();
        }
    }

    //停止指定辅助Process
    public void StopSideProcess(MainLoopProcess process)
    {
        //移除引用
        for (int i = this.sideProcesses.Count - 1; i >= 0; --i)
        {
            if (this.sideProcesses[i] == process)
                this.sideProcesses.RemoveAt(i);
        }
        //关闭Process
        process.ShutDownProcess();
    }

    //请求切换MainProcess
    public void RequestMainProcessSwitch(ProcessID ID)
    {
        if (this.upcomingProcess.HasValue)
        {
            this.upcomingProcess = new ProcessManager.ProcessID?(ID);
        }
    }

    //切换MainProcess
    private void SwitchMainProcess(ProcessID ID)
    {
        //获取当前主进程
        MainLoopProcess preMainLoop = this.currentMainLoop;
        if (this.currentMainLoop != null)
        {
            //关闭当前主进程
            this.currentMainLoop.ShutDownProcess();
            this.currentMainLoop.processActive = false;
            this.currentMainLoop = null;

            //GC
            GC.Collect();
            //释放资源引用
            Resources.UnloadUnusedAssets();
        }

        //根据枚举 实例化一个新的Process实例
        switch (ID)
        {
            case ProcessManager.ProcessID.Game:
                this.currentMainLoop = new Game(this);
                break;
        }
        //当前主进程与目标进程 通信
        preMainLoop?.CommunicateWithUpcomingProcess(this.currentMainLoop);
    } 
}
