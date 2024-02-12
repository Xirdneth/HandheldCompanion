using Microsoft.Win32.TaskScheduler;
using System;
using System.Security.Principal;

namespace HandheldCompanion.Managers;

public class TaskManager : ITaskManager
{
    private const string TaskName = "HandheldCompanion";
    private readonly Lazy<ISettingsManager> settingsManager;
    private string TaskExecutable;

    // TaskManager vars
    private Task task;
    private TaskDefinition taskDefinition;
    private TaskService taskService;

    public bool IsInitialized { get; set; }

    public event InitializedEventHandler Initialized;
    public delegate void InitializedEventHandler();

    public TaskManager(Lazy<ISettingsManager> settingsManager)
    {
        this.settingsManager = settingsManager;
        settingsManager.Value.SettingValueChanged += SettingsManager_SettingValueChanged;

    }

    private void SettingsManager_SettingValueChanged(string name, object value)
    {
        switch (name)
        {
            case "RunAtStartup":
                UpdateTask(Convert.ToBoolean(value));
                break;
        }
    }

    public void Start(string Executable)
    {
        TaskExecutable = Executable;
        taskService = new TaskService();

        try
        {
            // get current task, if any, delete it
            task = taskService.FindTask(TaskName);
            if (task is not null)
                taskService.RootFolder.DeleteTask(TaskName);
        }
        catch { }

        try
        {
            // create a new task
            taskDefinition = TaskService.Instance.NewTask();
            taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;
            taskDefinition.Principal.UserId = WindowsIdentity.GetCurrent().Name;
            taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
            taskDefinition.Settings.DisallowStartIfOnBatteries = false;
            taskDefinition.Settings.StopIfGoingOnBatteries = false;
            taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.Zero;
            taskDefinition.Settings.Enabled = false;
            taskDefinition.Triggers.Add(new LogonTrigger() { UserId = WindowsIdentity.GetCurrent().Name });
            taskDefinition.Actions.Add(new ExecAction(TaskExecutable));

            task = TaskService.Instance.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
            task.Enabled = settingsManager.Value.GetBoolean("RunAtStartup");
        }
        catch { }

        IsInitialized = true;
        Initialized?.Invoke();

        LogManager.LogInformation("{0} has started", "TaskManager");
    }

    public void Stop()
    {
        if (!IsInitialized)
            return;

        IsInitialized = false;

        settingsManager.Value.SettingValueChanged -= SettingsManager_SettingValueChanged;

        LogManager.LogInformation("{0} has stopped", "TaskManager");
    }

    private void UpdateTask(bool value)
    {
        if (task is null)
            return;

        try
        {
            task.Enabled = value;
        }
        catch
        {
        }
    }
}