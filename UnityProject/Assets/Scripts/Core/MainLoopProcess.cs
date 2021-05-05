
//功能：游戏功能模块虚拟进程

public abstract class MainLoopProcess
{
    public int framesPerSecond = 40;        //帧率
    public bool processActive = true;       //激活状态
    public ProcessManager manager;          //处理器管理器
    public ProcessManager.ProcessID ID;     //处理器ID

    public MainLoopProcess(ProcessManager manager, ProcessManager.ProcessID ID)
    {
        this.manager = manager;
        this.ID = ID;
    }

    //时间比例系数
    public virtual float TimeSpeedFac
    {
        get
        {
            return framesPerSecond / 40f;
        }
    }

    //原生Update：调用频率和Mono相同
    private float myTimeStacker;
    public virtual void RawUpdate(float dt)
    {
        //根据当前Process帧率进行计时
        myTimeStacker += dt * framesPerSecond;
        
        //以虚拟帧率调用Update
        if (myTimeStacker > 1.0)
        {
            this.Update();
            --myTimeStacker;
            if (myTimeStacker >= 2.0)
                myTimeStacker = 0.0f;
        }
        GrafUpdate(myTimeStacker);
    }

    //虚拟游戏循环:逻辑帧
    public virtual void Update() { }

    //调用频率同Mono
    public virtual void GrafUpdate(float timeStacker) { }

    //关闭处理器
    public virtual void ShutDownProcess() { }

    //与即将切换的Process通信交互
    public virtual void CommunicateWithUpcomingProcess(MainLoopProcess nextProcess) { }
}
