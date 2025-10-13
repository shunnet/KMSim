using LibreHardwareMonitor.Hardware;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;

namespace Snet.Windows.KMSim.utility
{
    public class SystemMonitoring
    {
        /// <summary>
        /// 获取当前对象（单例模式）
        /// </summary>
        /// <returns></returns>
        public static SystemMonitoring Instance()
        {
            if (instance == null)
            {
                lock (Lock)
                {
                    instance = new SystemMonitoring();
                }
            }
            return instance;
        }

        private static SystemMonitoring instance;
        private static readonly object Lock = new object();  //锁
        private Computer computer;
        private UpdateVisitor updateVisitor = new UpdateVisitor();

        /// <summary>
        /// 传感器信息类型
        /// </summary>
        public class SensorDataType
        {
            public string Key { get; set; }
            public string Vlaue { get; set; }
        }

        /// <summary>
        /// 硬件信息类型
        /// </summary>
        public class HardwareDataType
        {
            public HardwareDataType()
            {
                Vlaues = new List<SensorDataType>();
            }

            public string Key { get; set; }
            public string Vlaue { get; set; }
            public List<SensorDataType> Vlaues { get; set; }
        }

        /// <summary>
        /// 硬件信息
        /// </summary>
        public class HardwareData
        {
            public HardwareData()
            {
                Info = new List<HardwareDataType>();
            }

            /// <summary>
            /// 信息
            /// </summary>
            public List<HardwareDataType> Info { get; set; }

            /// <summary>
            /// 系统名称
            /// </summary>
            public string SystemName { get; set; }

            /// <summary>
            /// 系统版本
            /// </summary>
            public string SystemVer { get; set; }

            /// <summary>
            /// 系统运行时间
            /// </summary>
            public string SystemRunTime { get; set; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SystemMonitoring()
        {
            if (computer == null)
            {
                computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsControllerEnabled = true,
                    IsNetworkEnabled = true,
                    IsStorageEnabled = true
                };
                Init();
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public void Init()
        {
            computer.Open();
        }

        /// <summary>
        /// 结束
        /// </summary>
        public void End()
        {
            computer.Close();
        }

        /// <summary>
        /// 硬件数据获取
        /// </summary>
        public HardwareData GetInfo()
        {
            computer.Accept(updateVisitor);
            HardwareData hardwareData = new HardwareData();
            hardwareData.SystemName = GetSystemName();
            hardwareData.SystemVer = GetSystemVer();
            hardwareData.SystemRunTime = GetSystemRunTime();

            foreach (IHardware hardware in computer.Hardware)  //硬件
            {
                HardwareDataType hardwareDataType = new HardwareDataType() { Key = GetHardwareNameCn(hardware), Vlaue = hardware.Name };
                for (int i = 0; i < hardware.Sensors.Length; i++)
                {
                    string SensorsNameCn = GetSensorsNameCn(hardware, i);
                    if (!string.IsNullOrEmpty(hardware.Sensors[i].Value.ToString()))
                    {
                        hardwareDataType.Vlaues.Add(new SensorDataType() { Key = $"{SensorsNameCn},{hardware.Sensors[i].Name}", Vlaue = hardware.Sensors[i].Value.ToString() });
                    }
                }
                hardwareData.Info.Add(hardwareDataType);
            }
            return hardwareData;
        }

        /// <summary>
        /// 获取系统版本
        /// </summary>
        /// <returns></returns>
        private string GetSystemVer()
        {
            string result = string.Empty;
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (ManagementObject os in searcher.Get())
            {
                result = os["Caption"].ToString();
                break;
            }
            return result;
        }

        /// <summary>
        /// 获取系统名称
        /// </summary>
        /// <returns></returns>
        private string GetSystemName()
        {
            return System.Environment.GetEnvironmentVariable("ComputerName");
        }

        /// <summary>
        /// 获取系统运行时间
        /// </summary>
        /// <returns></returns>
        private string GetSystemRunTime()
        {
            int minutes = (Environment.TickCount / 0x3e8) / 60;
            int day = (int)Math.Floor(Convert.ToDouble(minutes / 1440));
            int hour = day > 0 ? (int)Math.Floor(Convert.ToDouble((minutes - day * 1440) / 60)) : (int)Math.Floor(Convert.ToDouble(minutes / 60));
            int minute = hour > 0 ? minutes - day * 1440 - hour * 60 : minutes;
            string time = string.Empty;
            if (day > 0)
                time += day + "天";
            if (hour > 0)
                time += hour + "小时";
            if (minute > 0)
                time += minute + "分钟";
            return time;
        }

        /// <summary>
        /// 获取硬件名称
        /// </summary>
        /// <returns></returns>
        private string GetHardwareNameCn(IHardware hardware)
        {
            string HardwareNameCn = string.Empty;
            switch (hardware.HardwareType)
            {
                case HardwareType.Motherboard:  //主板
                    HardwareNameCn = "主板";
                    break;

                case HardwareType.SuperIO: //IO芯片
                    HardwareNameCn = "IO芯片";
                    break;

                case HardwareType.Cpu: //处理器
                    HardwareNameCn = "处理器";
                    break;

                case HardwareType.Memory:  //内存
                    HardwareNameCn = "内存";
                    break;

                case HardwareType.GpuNvidia: //英伟达显卡
                    HardwareNameCn = "英伟达显卡";
                    break;

                case HardwareType.GpuAmd:  //AMD显卡
                    HardwareNameCn = "AMD显卡";
                    break;

                case HardwareType.GpuIntel: //英特尔显卡
                    HardwareNameCn = "因特尔显卡";
                    break;

                case HardwareType.Storage: //硬盘
                    HardwareNameCn = "硬盘";
                    break;

                case HardwareType.Network:  //网络
                    HardwareNameCn = "网络";
                    break;

                case HardwareType.Cooler:  //散热器
                    HardwareNameCn = "散热器";
                    break;

                case HardwareType.EmbeddedController: //嵌入式控制器
                    HardwareNameCn = "嵌入式控制器";
                    break;

                case HardwareType.Psu: //电源
                    HardwareNameCn = "电源";
                    break;

                case HardwareType.Battery: //电池
                    HardwareNameCn = "电池";
                    break;
            }
            return HardwareNameCn;
        }

        /// <summary>
        /// 获取传感器名称
        /// </summary>
        /// <returns></returns>
        private string GetSensorsNameCn(IHardware hardware, int i)
        {
            string SensorsNameCn = string.Empty;
            switch (hardware.Sensors[i].SensorType)
            {
                case SensorType.Voltage: //电压
                    SensorsNameCn = "电压";
                    break;

                case SensorType.Current:  //电流
                    SensorsNameCn = "电流";
                    break;

                case SensorType.Power://功率
                    SensorsNameCn = "功率";
                    break;

                case SensorType.Clock: //运行时间
                    SensorsNameCn = "运行时间";
                    break;

                case SensorType.Temperature:  //温度
                    SensorsNameCn = "温度";
                    break;

                case SensorType.Load: //负载
                    SensorsNameCn = "负载";
                    break;

                case SensorType.Frequency:  //频率
                    SensorsNameCn = "频率";
                    break;

                case SensorType.Fan: //风扇转速
                    SensorsNameCn = "风扇转速";
                    break;

                case SensorType.Flow: //流量
                    SensorsNameCn = "流量";
                    break;

                case SensorType.Control:  //控制器
                    SensorsNameCn = "控制器";
                    break;

                case SensorType.Level:  //电频
                    SensorsNameCn = "电频";
                    break;

                case SensorType.Factor: //系数
                    SensorsNameCn = "系数";
                    break;

                case SensorType.Data: //数据区
                    SensorsNameCn = "数据区";
                    break;

                case SensorType.SmallData: //内存卡
                    SensorsNameCn = "内存卡";
                    break;

                case SensorType.Throughput:  //吞吐量
                    SensorsNameCn = "吞吐量";
                    break;

                case SensorType.TimeSpan: //时间间隔
                    SensorsNameCn = "时间间隔";
                    break;

                case SensorType.Energy:  //动力
                    SensorsNameCn = "动力";
                    break;

                case SensorType.Noise:  //噪声
                    SensorsNameCn = "噪声";
                    break;
            }
            return SensorsNameCn;
        }

        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
            }

            public void VisitSensor(ISensor sensor)
            { }

            public void VisitParameter(IParameter parameter)
            { }
        }

        /// <summary>
        /// 判断程序是否是以管理员身份运行。
        /// </summary>
        public static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 检测程序是否重复多开
        /// </summary>
        public static bool IsOpen()
        {
            Process currentProcess = Process.GetCurrentProcess();
            var runningProcess = (from process in Process.GetProcesses()
                                  where
                                    process.Id != currentProcess.Id &&
                                    process.ProcessName.Equals(
                                      currentProcess.ProcessName,
                                      StringComparison.Ordinal)
                                  select process).FirstOrDefault();
            if (runningProcess != null)
            {
                return true;
            }
            return false;
        }
    }
}