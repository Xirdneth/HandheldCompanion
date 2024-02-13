using HandheldCompanion.Controls;
using HandheldCompanion.GraphicsProcessingUnit;
using HandheldCompanion.Managers.Interfaces;
using HandheldCompanion.Views;
using System;

namespace HandheldCompanion.Managers
{
    public class GPUManager : IGPUManager
    {
        #region events
        public event InitializedEventHandler Initialized;
        public delegate void InitializedEventHandler(GPU GPU);
        #endregion

        public bool IsInitialized;

        private GPU currentGPU { get; set; }
        private readonly Lazy<IProfileManager> profileManager;

        public GPUManager(Lazy<IProfileManager> profileManager)
        {
            this.profileManager = profileManager;
            currentGPU = new();
            // initialize processor
            currentGPU = GPU.GetCurrent();
            currentGPU.ImageSharpeningChanged += CurrentGPU_ImageSharpeningChanged;
            currentGPU.GPUScalingChanged += CurrentGPU_GPUScalingChanged;
            currentGPU.IntegerScalingChanged += CurrentGPU_IntegerScalingChanged;

            if (currentGPU is AMDGPU)
            {
                ((AMDGPU)currentGPU).RSRStateChanged += CurrentGPU_RSRStateChanged;
            }
            else if (currentGPU is IntelGPU)
            {

            }

            // manage events
            profileManager.Value.Applied += ProfileManager_Applied;
            profileManager.Value.Discarded += ProfileManager_Discarded;
            profileManager.Value.Updated += ProfileManager_Updated;

        }

        private void CurrentGPU_RSRStateChanged(bool Supported, bool Enabled, int Sharpness)
        {
            // todo: use ProfileMager events
            Profile profile = profileManager.Value.GetCurrent();
            AMDGPU amdGPU = (AMDGPU)currentGPU;

            if (Enabled != profile.RSREnabled)
                amdGPU.SetRSR(profile.RSREnabled);
            if (Sharpness != profile.RSRSharpness)
                amdGPU.SetRSRSharpness(profile.RSRSharpness);
        }

        private void CurrentGPU_IntegerScalingChanged(bool Supported, bool Enabled)
        {
            // todo: use ProfileMager events
            Profile profile = profileManager.Value.GetCurrent();

            if (Enabled != profile.IntegerScalingEnabled)
                currentGPU.SetIntegerScaling(profile.IntegerScalingEnabled, profile.IntegerScalingType);
        }

        private void CurrentGPU_GPUScalingChanged(bool Supported, bool Enabled, int Mode)
        {
            // todo: use ProfileMager events
            Profile profile = profileManager.Value.GetCurrent();

            if (Enabled != profile.GPUScaling)
                currentGPU.SetGPUScaling(profile.GPUScaling);
            if (Mode != profile.ScalingMode)
                currentGPU.SetScalingMode(profile.ScalingMode);
        }

        private void CurrentGPU_ImageSharpeningChanged(bool Enabled, int Sharpness)
        {
            // todo: use ProfileMager events
            Profile profile = profileManager.Value.GetCurrent();

            if (Enabled != profile.RISEnabled)
                currentGPU.SetImageSharpening(profile.RISEnabled);
            if (Sharpness != profile.RISSharpness)
                currentGPU.SetImageSharpeningSharpness(Sharpness);
        }

        public void Start()
        {
            currentGPU.Start();

            IsInitialized = true;
            Initialized?.Invoke(currentGPU);

            LogManager.LogInformation("{0} has started", "GPUManager");
        }

        public void Stop()
        {
            if (!IsInitialized)
                return;

            currentGPU.Stop();

            IsInitialized = false;

            LogManager.LogInformation("{0} has stopped", "GPUManager");
        }

        private void ProfileManager_Applied(Profile profile, UpdateSource source)
        {
            try
            {
                // apply profile GPU Scaling
                // apply profile scaling mode
                if (profile.GPUScaling)
                {
                    if (!currentGPU.GetGPUScaling())
                        currentGPU.SetGPUScaling(true);

                    if (currentGPU.GetScalingMode() != profile.ScalingMode)
                        currentGPU.SetScalingMode(profile.ScalingMode);
                }
                else if (currentGPU.GetGPUScaling())
                {
                    currentGPU.SetGPUScaling(false);
                }

                // apply profile RSR
                if (currentGPU is AMDGPU amdGPU)
                {
                    if (profile.RSREnabled)
                    {
                        if (!amdGPU.GetRSR())
                            amdGPU.SetRSR(true);

                        if (amdGPU.GetRSRSharpness() != profile.RSRSharpness)
                            amdGPU.SetRSRSharpness(profile.RSRSharpness);
                    }
                    else if (amdGPU.GetRSR())
                    {
                        amdGPU.SetRSR(false);
                    }
                }

                // apply profile Integer Scaling
                if (profile.IntegerScalingEnabled)
                {
                    if (!currentGPU.GetIntegerScaling())
                        currentGPU.SetIntegerScaling(true, profile.IntegerScalingType);
                }
                else if (currentGPU.GetIntegerScaling())
                {
                    currentGPU.SetIntegerScaling(false, 0);
                }

                // apply profile image sharpening
                if (profile.RISEnabled)
                {
                    if (!currentGPU.GetImageSharpening())
                        currentGPU.SetImageSharpening(profile.RISEnabled);

                    if (currentGPU.GetImageSharpeningSharpness() != profile.RISSharpness)
                        currentGPU.SetImageSharpeningSharpness(profile.RISSharpness);
                }
                else if (currentGPU.GetImageSharpening())
                {
                    currentGPU.SetImageSharpening(false);
                }
            }
            catch { }
        }

        private void ProfileManager_Discarded(Profile profile)
        {
            try
            {
                /*
                // restore default GPU Scaling
                if (profile.GPUScaling && currentGPU.GetGPUScaling())
                    currentGPU.SetGPUScaling(false);
                */

                // restore default RSR
                if (currentGPU is AMDGPU amdGPU)
                {
                    if (profile.RSREnabled && amdGPU.GetRSR())
                        amdGPU.SetRSR(false);
                }

                // restore default integer scaling
                if (profile.IntegerScalingEnabled && currentGPU.GetIntegerScaling())
                    currentGPU.SetIntegerScaling(false, 0);

                // restore default image sharpening
                if (profile.RISEnabled && currentGPU.GetImageSharpening())
                    currentGPU.SetImageSharpening(false);
            }
            catch { }
        }

        // todo: moveme
        private void ProfileManager_Updated(Profile profile, UpdateSource source, bool isCurrent)
        {
            ProcessEx.SetAppCompatFlag(profile.Path, ProcessEx.DisabledMaximizedWindowedValue, !profile.FullScreenOptimization);
            ProcessEx.SetAppCompatFlag(profile.Path, ProcessEx.HighDPIAwareValue, !profile.HighDPIAware);
        }
    }
}
