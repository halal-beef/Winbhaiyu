using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SharpBoot;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.Wim;
using Microsoft.Win32;
using static SharpBoot.Fastboot;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.IO.Compression;
using System.Runtime.Intrinsics.Arm;

namespace Winbhaiyu
{
    /// <summary>
    /// Interaction logic for NextPhase.xaml
    /// </summary>
    public partial class NextPhase : Window
    {
        public NextPhase()
        {
            InitializeComponent();
            consout.Clear();
        }

        private void DownloadFile(string Url, string Filename)
        {
            HttpClient hc = new();
            hc.Timeout = Timeout.InfiniteTimeSpan;
            using (FileStream fs0 = File.Create(Filename))
            {
                HttpResponseMessage hrm4 = hc.GetAsync(Url).GetAwaiter().GetResult();
                hrm4.Content.CopyToAsync(fs0).GetAwaiter().GetResult();
            }
        }

        private string DownloadString(string Url)
        {
            HttpClient hc = new();
            hc.Timeout = Timeout.InfiniteTimeSpan;
            hc.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Winbhaiyu", "1"));
            HttpResponseMessage hrm4 = hc.GetAsync(Url).GetAwaiter().GetResult();
            using (HttpContent content = hrm4.Content)
            {
                return content.ReadAsStringAsync().Result;
            }
        }

        private async void Install()
        {
            #region Device Variables
            bool is256 = false; //Safer to assume 128gb just in case of program malfunctioning
            string DisplayManufacturer = "";
            int Ram = 0;
            #endregion

            if (await messagebox("Make sure only no mass storage mode devices and no other android device is connected except your Poco X3 Pro, not following this may lead to consequences you will regret, THIS PROCESS CANNOT BE STOPPED WHEN STARTED! MAKE SURE ALL YOUR DATA IS BACKED UP! Connect your phone in fastboot mode and press continue to start.", "Continue", "Cancel", false, 12) == "Continue")
            {
                ReportProgress(0, "Starting install...");
            connection:
                Canvas.SetZIndex(block, 2);
                FadeIn(block);
                await Task.Delay(1000);
                Fastboot vyu = new();
                try
                {
                    vyu.Connect();
                }
                catch
                {
                    FadeOut(block);
                    await Task.Delay(1000);
                    if (await messagebox("Unable to connect to phone! Make sure drivers are installed and you have connected your phone in fastboot mode then press continue.", "Continue", "Cancel", true, 12) == "Continue")
                    {
                        goto connection;
                    }
                    else
                    {
                        WriteLine("Install canceled.");
                        return;
                    }
                }
                ReportProgress(0, "Executing: fastboot getvar:product");
                string prod = vyu.Command("getvar:product").Payload.Trim();
                if (prod == "vayu" || prod == "bhima")
                {
                    ReportProgress(0, "Poco X3 Pro Detected! Continuing...");
                    ReportProgress(0, "Downloading Files...");
                    var Download = new Action(() =>
                    {
                        string Drivers = JObject.Parse(DownloadString("https://api.github.com/repos/degdag/Vayu-Drivers/releases/latest"))["assets"][0]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");
                        string DriverUpdater = JObject.Parse(DownloadString("https://api.github.com/repos/WoA-Project/DriverUpdater/releases/latest"))["assets"][1]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");
                        string UEFI = JObject.Parse(DownloadString("https://api.github.com/repos/degdag/edk2-msm/releases/latest"))["assets"][0]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");

                        DownloadFile("https://github.com/Icesito68/Port-Windows-11-Poco-X3-pro/releases/download/binaries/gpt_both0.bin", "gpt_both0.bin");
                        ReportProgress(0, "Finished Downloading GPT File");
                        DownloadFile("https://github.com/Icesito68/Port-Windows-11-Poco-X3-pro/releases/download/Recoveries/twrp-3.7.0_12-vayu-mod.img", "twrp-3.7.0_12-vayu.img");
                        ReportProgress(0, "Finished Downloading TWRP");
                        DownloadFile("https://github.com/Icesito68/Port-Windows-11-Poco-X3-pro/releases/download/binaries/DisMTP", "DisMTP");
                        ReportProgress(0, "Finished Downloading MTP Disabling Script");
                        DownloadFile(Drivers, "Drivers.zip");
                        ReportProgress(0, "Finished Downloading Drivers");
                        DownloadFile(DriverUpdater, "DriverUpdater.zip");
                        ReportProgress(0, "Finished Downloading Drivers");
                        DownloadFile(UEFI, "UEFI.zip");
                        ReportProgress(0, "Finished Downloading UEFI");
                    });

                    await Task.Factory.StartNew(Download);

                    poo.ScrollToEnd();

                    ReportProgress(0, "Executing: fastboot flash partition:0 gpt_both0.bin");
                    vyu.Flash("gpt_both0.bin", "partition:0");
                    vyu.Command("erase:userdata");
                    vyu.Command("erase:metadata");
                    vyu.Command("erase:cache");
                    await Task.Delay(500);
                    ReportProgress(0, "Flashing TWRP, Please Wait...");

                    poo.ScrollToEnd();

                    var BootRecovery = new Action(() =>
                    {
                        vyu.Reboot(Fastboot.RebootOptions.Bootloader);
                        vyu.Disconnect();
                        Task.Delay(10000).Wait(); //10 Seconds for the phone to get into fastboot is plenty.
                        vyu.Connect();
                        ReportProgress(0, "Waiting for device...");
                        vyu.Flash("twrp-3.7.0_12-vayu.img", "recovery");
                        vyu.Reboot(Fastboot.RebootOptions.Recovery);
                        vyu.Disconnect();
                        StartADB("wait-for-usb-recovery").Wait();
                    });
                    await Task.Factory.StartNew(BootRecovery);
                    ReportProgress(0, "We are in TWRP!");
                    ReportProgress(0, "Unmounting userdata...");
                    var UnmntData = new Action(() =>
                    {
                        StartADB("shell twrp unmount data").Wait();
                    });
                    await Task.Factory.StartNew(UnmntData);
                    ReportProgress(0, "Hardware Info: \n");

                    string gptsize = "";

                    var DeclareStorage = new Action(async () =>
                    {
                        gptsize = (await StartADB("shell parted /dev/block/sda print")).Split('\n')[1].Trim();
                    });
                    await Task.Factory.StartNew(DeclareStorage);

                    var GetStorageRam = new Action(() =>
                    {

                        if (gptsize == "Disk /dev/block/sda: 255GB")
                        {
                            is256 = true;
                            ReportProgress(0, "Storage Size: 256GB");
                            Ram = 8;
                            ReportProgress(0, "RAM Size (Assumed via Storage): 8GB");
                        }
                        else if (gptsize == "Disk /dev/block/sda: 127GB")
                        {
                            is256 = false;
                            ReportProgress(0, "Storage Size: 128GB");
                            Ram = 6;
                            ReportProgress(0, "RAM Size (Assumed via Storage): 6GB");
                        }
                    });

                    await Task.Factory.StartNew(GetStorageRam);

                    string CmdLine = "";
                    var GetCmdLine = new Action(async () =>
                    {
                        CmdLine = await StartADB("shell cat /proc/cmdline");
                    });

                    await Task.Factory.StartNew(GetCmdLine);

                    var GetDisplay = new Action(() =>
                        {
                            if (CmdLine.Contains("msm_drm.dsi_display0=dsi_j20s_42_02_0b_video_display:"))
                            {
                                DisplayManufacturer = "Huaxing";
                                ReportProgress(0, "Display Manufacturer: Huaxing");
                            }
                            else
                            {
                                DisplayManufacturer = "Tianma";
                                ReportProgress(0, "Display Manufacturer: Tianma");
                            }
                        });

                    await Task.Factory.StartNew(GetDisplay);

                    poo.ScrollToEnd();

                    var ResizeGPT = new Action(() =>
                    {
                        ReportProgress(0, "Extending GPT size to allow for 64 partitions instead of 32...");
                        StartADB("shell sgdisk --resize-table 64 /dev/block/sda").Wait();
                    });

                    await Task.Factory.StartNew(ResizeGPT);

                    if (is256)
                    {
                        var CreateParts = new Action(() =>
                        {
                            WriteLine("Removing userdata...");
                            StartADB("shell parted /dev/block/sda rm 32").Wait();
                            WriteLine("Creating ESP Partition...");
                            StartADB("shell parted /dev/block/sda mkpart esp fat32 11.8GB 12.2GB").Wait();
                            WriteLine("Creating Windows Partition...");
                            StartADB("shell parted /dev/block/sda mkpart win ntfs 12.2GB 132.2GB").Wait();
                            WriteLine("Recreating userdata Partition...");
                            StartADB("shell parted /dev/block/sda mkpart userdata ext4 132.2GB 255GB").Wait();
                            StartADB("shell parted /dev/block/sda set 32 esp on").Wait();
                        });

                        await Task.Factory.StartNew(CreateParts);

                    }
                    else
                    {
                        var CreateParts = new Action(() =>
                        {
                            WriteLine("Removing userdata...");
                            StartADB("shell parted /dev/block/sda rm 32").Wait();
                            WriteLine("Creating ESP Partition...");
                            StartADB("shell parted /dev/block/sda mkpart esp fat32 11.8GB 12.2GB").Wait();
                            WriteLine("Creating Windows Partition...");
                            StartADB("shell parted /dev/block/sda mkpart win ntfs 12.2GB 70.2GB").Wait();
                            WriteLine("Recreating Userdata Partition...");
                            StartADB("shell parted /dev/block/sda mkpart userdata ext4 70.2GB 127GB").Wait();
                            StartADB("shell parted /dev/block/sda set 32 esp on").Wait();
                        });

                        await Task.Factory.StartNew(CreateParts);

                    }


                    var RebootRec = new Action(() =>
                    {
                        ReportProgress(0, "Rebooting back into TWRP, please wait...");
                        StartADB("shell twrp reboot recovery").Wait();
                        Task.Delay(1500).Wait(); //Offset the time twrp needs to process script and reboot phone and for windows to realise usb device not connected.
                        ReportProgress(0, "Waiting for device...");
                        StartADB("wait-for-usb-recovery").Wait();
                    });

                    await Task.Factory.StartNew(RebootRec);

                    var ErasePhone = new Action(() =>
                    {
                        ReportProgress(0, "Erasing userdata...");
                        StartADB("shell twrp format data").Wait();
                        StartADB("shell twrp wipe cache").Wait();
                        ReportProgress(0, "Formatting ESP...");
                        StartADB("shell mkfs.fat -F32 -s1 /dev/block/by-name/esp -n ESPVAYU").Wait();
                        ReportProgress(0, "Formatting Windows Partition...");
                        StartADB("shell mkfs.ntfs -f /dev/block/by-name/win -L WINVAYU").Wait();
                    });

                    await Task.Factory.StartNew(ErasePhone);

                    poo.ScrollToEnd();

                    var MSC = new Action(() =>
                    {
                        ReportProgress(0, "Putting phone into mass storage mode please wait...");
                        StartADB("push DisMTP /sbin").Wait();
                        StartADB("shell sh /sbin/DisMTP").Wait();
                        StartADB("wait-for-usb-recovery").Wait();
                        StartADB("shell msc.sh").Wait();
                    });

                    await Task.Factory.StartNew(MSC);

                    var DiskpartLetter = new Action(async () =>
                    {
                        Task.Delay(1500).Wait(); // Wait for disks to pick up.
                        ReportProgress(0, "Assigning Letters...");
                        string vols = await StartDiskpart("list volume\n");
                        string[] split = vols.Split('\n');
                        string windowspart = split.First(w => w.Contains("WINVAYU")).Trim();
                        int indexwin = Int32.Parse(windowspart.Split(new string[] { " " }, StringSplitOptions.None)[1]);
                        string esppart = split.First(e => e.Contains("ESPVAYU")).Trim();
                        int indexesp = Int32.Parse(esppart.Split(new string[] { " " }, StringSplitOptions.None)[1]);
                        ReportProgress(0, windowspart);
                        ReportProgress(0, esppart);
                        StartDiskpart($"sel volume {indexwin}\nassign letter=x").Wait();
                        StartDiskpart($"sel volume {indexesp}\nassign letter=y").Wait();
                        ReportProgress(0, "Letters Assigned");
                    });

                    await Task.Factory.StartNew(DiskpartLetter);

                    var InstallWindows = new Action(async () =>
                    {
                        WriteLine("");
                        WriteLine("Installing Windows...");
                    FileDialog:
                        Dispatcher.Invoke(() =>
                        {
                            this.Topmost = false;
                        });
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Title = "Browse to your windows install image";
                        openFileDialog.Filter = "Windows Installation Image (install.wim)|install.wim";
                        if (openFileDialog.ShowDialog() == true)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                this.Topmost = true;
                            });
                            using (WimHandle wimHandle = WimgApi.CreateFile(openFileDialog.FileName, WimFileAccess.Read, WimCreationDisposition.OpenExisting, WimCreateFileOptions.None, WimCompressionType.None))
                            {
                                WimgApi.SetTemporaryPath(wimHandle, Environment.GetEnvironmentVariable("TEMP"));
                                WimgApi.RegisterMessageCallback(wimHandle, Progress);

                                try
                                {
                                    using (WimHandle imageHandle = WimgApi.LoadImage(wimHandle, 1))
                                    {
                                        WimgApi.ApplyImage(imageHandle, @"X:\", WimApplyImageOptions.None);
                                    }
                                }
                                finally
                                {
                                    WimgApi.UnregisterMessageCallback(wimHandle, Progress);
                                }
                            }
                        }
                        else
                        {
                            await Dispatcher.Invoke(async () =>
                            {
                                this.Topmost = true;
                            await messagebox("You need a wim file to continue! Press continue to re-open the file dialog and choose your wim file.", "Continue", "Continue", true, 12);
                            await Task.Delay(1000);
                            Canvas.SetZIndex(block, 2);
                            FadeIn(block);
                            });
                            goto FileDialog;
                        }
                    });

                    await Task.Factory.StartNew(InstallWindows);

                    ReportProgress(0, "Finished installing windows");

                    var BCDBOOT = new Action(() =>
                    {
                        ReportProgress(0, "Creating windows bootloader files...");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = "bcdboot.exe";
                        startInfo.Arguments = @" X:\Windows /s Y: /f UEFI";
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = true;
                        Process process = new();
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();
                    });

                    await Task.Factory.StartNew(BCDBOOT);

                    var TMODE = new Action(() =>
                        {
                            ReportProgress(0, "Enabling test mode...");
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = "bcdedit.exe";
                            startInfo.Arguments = @" /store Y:\EFI\Microsoft\BOOT\BCD /set {default} testsigning on";
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            Process process = new();
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                        });

                    await Task.Factory.StartNew(TMODE);

                    var Unzip = new Action(() =>
                    {
                        ReportProgress(0, "Unzipping files");
                        ZipFile.ExtractToDirectory("DriverUpdater.zip",Directory.GetCurrentDirectory());
                        ZipFile.ExtractToDirectory("Drivers.zip", Directory.GetCurrentDirectory());
                        ZipFile.ExtractToDirectory("UEFI.zip", Directory.GetCurrentDirectory());
                        ReportProgress(0, "Unzipped files");
                    });

                    poo.ScrollToEnd();

                    await Task.Factory.StartNew(Unzip);

                    var ModifyDrivers = new Action(() =>
                        {
                            if(DisplayManufacturer == "Huaxing")
                            {
                                ReportProgress(0, "Editing touch driver to support huaxing...");
                                File.Delete("Vayu-Drivers-Full\\components\\QC8150\\Device\\DEVICE.SOC_QC8150.VAYU\\Drivers\\Touch\\j20s_novatek_ts_fw01.bin");
                                File.Move("Vayu-Drivers-Full\\components\\QC8150\\Device\\DEVICE.SOC_QC8150.VAYU\\Drivers\\Touch\\j20s_novatek_ts_fw02.bin", "Vayu-Drivers-Full\\components\\QC8150\\Device\\DEVICE.SOC_QC8150.VAYU\\Drivers\\Touch\\j20s_novatek_ts_fw01.bin");
                                ReportProgress(0, "Finished editing");
                            }
                        });

                    await Task.Factory.StartNew(ModifyDrivers);

                    var InstallDrivers = new Action(() =>
                        {
                            ReportProgress(0, "Installing drivers...");
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.FileName = "DriverUpdater.exe";
                            startInfo.Arguments = @" -d Vayu-Drivers-Full\definitions\Desktop\ARM64\Internal\vayu.txt -r Vayu-Drivers-Full -p X:";
                            startInfo.UseShellExecute = false;
                            startInfo.CreateNoWindow = true;
                            Process process = new();
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                        });

                    await Task.Factory.StartNew(InstallDrivers);

                    var ProvisionSensors = new Action(() =>
                    {
                        ReportProgress(0, "Provisioning Sensors...");
                        StartADB("shell twrp mount /persist").Wait();
                        StartADB("shell mount.ntfs /dev/block/by-name/win /win").Wait();
                        StartADB("shell mkdir -p /win/Windows/System32/drivers/DriverData/QUALCOMM/fastRPC/persist").Wait();
                        StartADB("shell cp -r /persist/sensors /win/Windows/System32/drivers/DriverData/QUALCOMM/fastRPC/persist/").Wait();
                        ReportProgress(0, "Provisioned! Install finished :D");
                    });

                    await Task.Factory.StartNew(ProvisionSensors);

                    var BootUEFI = new Action(async () =>
                    {
                        ReportProgress(0, "Booting UEFI...");
                        Fastboot vyu = new();
                        StartADB("shell twrp reboot bootloader").Wait();
                        Task.Delay(10000).Wait(); // 10 Second Delay
                        vyu.Connect();
                        string Folder = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(f => f.Contains("vayu-uefi")).First();
                        string File = Directory.GetFiles(Folder).Where(d => d.Contains($@"{DisplayManufacturer.ToLower()}-{Ram}gb")).First();
                        vyu.Boot(File);
                    });

                    await Task.Factory.StartNew(BootUEFI);

                    var DeleteFiles = new Action(() =>
                    {
                        string Folder = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(f => f.Contains("vayu-uefi")).First();

                        ReportProgress(0, "Deleting files...");
                        Directory.Delete("Vayu-Drivers-Full", true);
                        Directory.Delete(Folder, true);
                        File.Delete("DriverUpdater.exe");
                        File.Delete("DriverUpdater.pdb");
                        File.Delete("DriverUpdater.zip");
                        File.Delete("UEFI.zip");
                        File.Delete("Drivers.zip");
                        File.Delete("DisMTP");
                        File.Delete("twrp-3.7.0_12-vayu.img");
                        File.Delete("gpt_both0.bin");
                        ReportProgress(0, "Finished everything :D");
                    });

                    await Task.Factory.StartNew(DeleteFiles);

                    Dispatcher.Invoke(() =>
                    {
                        FadeOut(block);
                        Task.Delay(1000).Wait();
                        Canvas.SetZIndex(block, -2);
                    });
                }
                else
                {
                    await Dispatcher.Invoke(async () =>
                    {
                        FadeOut(block);
                        await Task.Delay(1000);
                        await messagebox("Unsupported product detected!", "Okay", ":(", true, 12);
                    });
                }
            }
        }

        private WimMessageResult Progress(WimMessageType messageType, object message, object userData)
        {
            switch (messageType)
            {
                case WimMessageType.Progress:  
                    WimMessageProgress progressMessage = (WimMessageProgress)message;
                    WriteLine($"Installing Windows... {progressMessage.PercentComplete}%");
                    break;
            }
            return WimMessageResult.Success;
        }

        private void ReportProgress(int useless, string msg)
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Dispatcher.Invoke(() =>
                {
                    WriteLine(msg);
                });
            }).Start();
        }


        private TimeSpan duration { get; set; } = TimeSpan.FromSeconds(1);
        private IEasingFunction ease { get; set; } = new QuarticEase { EasingMode = EasingMode.EaseInOut };


        public void FadeOut(DependencyObject element)
        {

            DoubleAnimation fadeAnimation = new DoubleAnimation()
            {
                From = 0.4,
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        public void Shift(DependencyObject element, Thickness from, Thickness to)
        {
            ThicknessAnimation shiftAnimation = new ThicknessAnimation()
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = ease
            };

            Storyboard.SetTarget(shiftAnimation, element);
            Storyboard.SetTargetProperty(shiftAnimation, new PropertyPath(MarginProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(shiftAnimation);
            storyboard.Begin();
        }


        public void FadeIn(DependencyObject element)
        {

            DoubleAnimation fadeAnimation = new DoubleAnimation()
            {
                From = 0,
                To = 0.4,
                Duration = duration,
                EasingFunction = ease
            };

            Storyboard.SetTarget(fadeAnimation, element);
            Storyboard.SetTargetProperty(fadeAnimation, new PropertyPath(OpacityProperty));

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(fadeAnimation);
            storyboard.Begin();
        }

        async Task<string> messagebox(string msg, string but1, string but2, bool err, int fn)
        {
            string re = await Dispatcher.Invoke(async () =>
            {

                Canvas.SetZIndex(block, 2);
                FadeIn(block);
                Dialog p = new(msg, but1, but2, err, fn);
                p.ShowDialog(this);
                FadeOut(block);
                await Task.Delay(1000);
                Canvas.SetZIndex(block, -2);
                return p.ans;
            });
            return re;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Shift(gr, gr.Margin, new Thickness(0));
            await Task.Delay(1000);
            if (await messagebox("We're not responsible for bricked devices, missing recovery partitions, dead microSD cards, dead cats or dogs, nuclear wars or you getting fired because you forgot to boot back in to android for the alarm, or any damage caused by this tool.\n\nDo you Accept?", "Accept", "Decline", false, 12) == "Accept")
            {

            }
            else
            {
                Environment.Exit(0);
            }

            foreach (Process process in Process.GetProcessesByName("adb"))
            {
                process.Kill(); // Kill any running adb server, this is because adb may potentially bring up a message about server version not matching, this can seriously affect the technique on how we grab hw info.
            }
        }

        private void WriteLine(string inp)
        {
            Dispatcher.Invoke(() =>
            {
                consout.Text += "\n" + inp;
            });
        }

        async static Task<string> StartADB(string args)
        {
            string outp;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "adb.exe";
            startInfo.Arguments = args;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            Process process = new();
            process.StartInfo = startInfo;
            process.Start();
            outp = process.StandardOutput.ReadToEnd();
            await process.WaitForExitAsync();
            return outp;
        }

        async static Task<string> StartDiskpart(string args)
        {
            string outp;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "diskpart.exe";
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            Process process = new();
            process.StartInfo = startInfo;
            process.Start();
            var eacharg = args.Split('\n');
            foreach (string i in eacharg)
            {
                process.StandardInput.WriteLine(i);
            }
            process.StandardInput.WriteLine("exit");
            outp = process.StandardOutput.ReadToEnd();
            await process.WaitForExitAsync();
            return outp;
        }

        private void InstallWindows_Click(object sender, RoutedEventArgs e)
        {
            Install();
        }
        private async void UninstallWindows_Click(object sender, RoutedEventArgs e)
        {
            // No need ot run stuff in an extra thread since the main ui thread wont be blocked for long.

            if (await messagebox("Make sure no other android device is connected except your Poco X3 Pro, not following this may lead to consequences you will regret, THIS PROCESS CANNOT BE STOPPED WHEN STARTED! MAKE SURE ALL YOUR DATA IS BACKED UP! Connect your phone in fastboot mode and press continue to start.", "Continue", "Cancel", false, 12) == "Continue")
            {
                ReportProgress(0, "Starting uninstall...");
            connection:
                Canvas.SetZIndex(block, 2);
                FadeIn(block);
                await Task.Delay(1000);
                Fastboot vyu = new();
                try
                {
                    vyu.Connect();
                }
                catch
                {
                    FadeOut(block);
                    await Task.Delay(1000);
                    if (await messagebox("Unable to connect to phone! Make sure drivers are installed and you have connected your phone in fastboot mode then press continue.", "Continue", "Cancel", true, 12) == "Continue")
                    {
                        goto connection;
                    }
                    else
                    {
                        WriteLine("Uninstall canceled.");
                        return;
                    }
                }
                ReportProgress(0, "Executing: fastboot getvar:product");
                string prod = vyu.Command("getvar:product").Payload.Trim();
                if (prod == "vayu" || prod == "bhima")
                {
                    ReportProgress(0, "Poco X3 Pro Detected! Continuing...");
                    ReportProgress(0, "Downloading Files...");
                    var Download = new Action(() =>
                    {
                        DownloadFile("https://github.com/Icesito68/Port-Windows-11-Poco-X3-pro/releases/download/binaries/gpt_both0.bin", "gpt_both0.bin");
                    });

                    await Task.Factory.StartNew(Download);

                    ReportProgress(0, "Executing: fastboot flash partition:0 gpt_both0.bin");
                    vyu.Flash("gpt_both0.bin", "partition:0");
                    ReportProgress(0, "Executing: fastboot erase userdata"); // In my experience the userdata partition was formatted as f2fs so this command is fine
                    vyu.Command("erase:userdata");
                    vyu.Reboot(Fastboot.RebootOptions.System);
                    ReportProgress(0, "Finished uninstall.");
                    await Task.Delay(500);
                }
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (await messagebox("Make sure no other android device is connected except your Poco X3 Pro, not following this may lead to consequences you will regret, THIS PROCESS CANNOT BE STOPPED WHEN STARTED! MAKE SURE ALL YOUR DATA IS BACKED UP! Connect your phone in fastboot mode and press continue to start.", "Continue", "Cancel", false, 12) == "Continue")
            {
                ReportProgress(0, "Updating Drivers...");
            connection:
                Canvas.SetZIndex(block, 2);
                FadeIn(block);
                await Task.Delay(1000);
                Fastboot vyu = new();
                try
                {
                    vyu.Connect();
                }
                catch
                {
                    FadeOut(block);
                    await Task.Delay(1000);
                    if (await messagebox("Unable to connect to phone! Make sure drivers are installed and you have connected your phone in fastboot mode then press continue.", "Continue", "Cancel", true, 12) == "Continue")
                    {
                        goto connection;
                    }
                    else
                    {
                        WriteLine("Driver update canceled.");
                        return;
                    }
                }
                ReportProgress(0, "Executing: fastboot getvar:product");
                string prod = vyu.Command("getvar:product").Payload.Trim();
                if (prod == "vayu" || prod == "bhima")
                {
                    ReportProgress(0, "Poco X3 Pro Detected! Continuing...");
                    ReportProgress(0, "Downloading Files...");
                    var Download = new Action(() =>
                    {
                        string Drivers = JObject.Parse(DownloadString("https://api.github.com/repos/degdag/Vayu-Drivers/releases/latest"))["assets"][0]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");
                        string DriverUpdater = JObject.Parse(DownloadString("https://api.github.com/repos/WoA-Project/DriverUpdater/releases/latest"))["assets"][1]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");
                        string UEFI = JObject.Parse(DownloadString("https://api.github.com/repos/degdag/edk2-msm/releases/latest"))["assets"][0]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");

                        DownloadFile("https://github.com/Icesito68/Port-Windows-11-Poco-X3-pro/releases/download/Recoveries/twrp-3.7.0_12-vayu-mod.img", "twrp-3.7.0_12-vayu.img");
                        ReportProgress(0, "Finished Downloading TWRP");
                        DownloadFile("https://github.com/Icesito68/Port-Windows-11-Poco-X3-pro/releases/download/binaries/DisMTP", "DisMTP");
                        ReportProgress(0, "Finished Downloading MTP Disabling Script");
                        DownloadFile(Drivers, "Drivers.zip");
                        ReportProgress(0, "Finished Downloading Drivers");
                        DownloadFile(DriverUpdater, "DriverUpdater.zip");
                        ReportProgress(0, "Finished Downloading Drivers");
                    });

                    await Task.Factory.StartNew(Download);

                    var BootRecovery = new Action(() =>
                    {
                        ReportProgress(0, "Booting TWRP...");
                        vyu.Flash("twrp-3.7.0_12-vayu.img", "recovery");
                        vyu.Reboot(Fastboot.RebootOptions.Recovery);
                        vyu.Disconnect();
                        StartADB("wait-for-usb-recovery").Wait();
                    });

                    await Task.Factory.StartNew(BootRecovery);
                    poo.ScrollToEnd();

                    var MSC = new Action(() =>
                    {
                        ReportProgress(0, "We are in TWRP!");
                        ReportProgress(0, "Putting phone into mass storage mode please wait...");
                        StartADB("push DisMTP /sbin").Wait();
                        StartADB("shell sh /sbin/DisMTP").Wait();
                        StartADB("wait-for-usb-recovery").Wait();
                        StartADB("shell msc.sh").Wait();
                    });

                    await Task.Factory.StartNew(MSC);
                    poo.ScrollToEnd();

                    var DiskpartLetter = new Action(async () =>
                    {
                        Task.Delay(1500).Wait(); // Wait for disks to pick up.
                        ReportProgress(0, "Assigning Letter...");
                        string vols = await StartDiskpart("list volume\n");
                        string[] split = vols.Split('\n');
                        string windowspart = split.First(w => w.Contains("WINVAYU")).Trim();
                        int indexwin = Int32.Parse(windowspart.Split(new string[] { " " }, StringSplitOptions.None)[1]);
                        ReportProgress(0, windowspart);
                        StartDiskpart($"sel volume {indexwin}\nassign letter=x").Wait();
                        ReportProgress(0, "Letter Assigned");
                    });

                    await Task.Factory.StartNew(DiskpartLetter);

                    poo.ScrollToEnd();

                    var Unzip = new Action(() =>
                    {
                        ReportProgress(0, "Unzipping files");
                        ZipFile.ExtractToDirectory("DriverUpdater.zip", Directory.GetCurrentDirectory());
                        ZipFile.ExtractToDirectory("Drivers.zip", Directory.GetCurrentDirectory());
                        ReportProgress(0, "Unzipped files");
                    });

                    await Task.Factory.StartNew(Unzip);

                    string DisplayManufacturer = "";
                    string CmdLine = await StartADB("shell cat /proc/cmdline");

                    var GetDisplay = new Action(() =>
                    {

                        if (CmdLine.Contains("msm_drm.dsi_display0=dsi_j20s_42_02_0b_video_display:"))
                        {
                            DisplayManufacturer = "Huaxing";
                            ReportProgress(0, "Display Manufacturer: Huaxing");
                        }
                        else
                        {
                            DisplayManufacturer = "Tianma";
                            ReportProgress(0, "Display Manufacturer: Tianma");
                        }
                    });

                    await Task.Factory.StartNew(GetDisplay);
                    poo.ScrollToEnd();

                    var ModifyDrivers = new Action(() =>
                    { 
                        if (DisplayManufacturer == "Huaxing")
                        {
                            ReportProgress(0, "Editing touch driver to support huaxing...");
                            File.Delete("Vayu-Drivers-Full\\components\\QC8150\\Device\\DEVICE.SOC_QC8150.VAYU\\Drivers\\Touch\\j20s_novatek_ts_fw01.bin");
                            File.Move("Vayu-Drivers-Full\\components\\QC8150\\Device\\DEVICE.SOC_QC8150.VAYU\\Drivers\\Touch\\j20s_novatek_ts_fw02.bin", "Vayu-Drivers-Full\\components\\QC8150\\Device\\DEVICE.SOC_QC8150.VAYU\\Drivers\\Touch\\j20s_novatek_ts_fw01.bin");
                            ReportProgress(0, "Finished editing");
                        }
                    });

                    await Task.Factory.StartNew(ModifyDrivers);

                    var InstallDrivers = new Action(() =>
                    {
                        ReportProgress(0, "Installing drivers...");
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = "DriverUpdater.exe";
                        startInfo.Arguments = @" -d Vayu-Drivers-Full\definitions\Desktop\ARM64\Internal\vayu.txt -r Vayu-Drivers-Full -p X:";
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = false;
                        Process process = new();
                        process.StartInfo = startInfo;
                        process.Start();
                        process.WaitForExit();
                    });

                    await Task.Factory.StartNew(InstallDrivers);

                    ReportProgress(0, "Finished Installing Drivers");

                    poo.ScrollToEnd();

                    Dispatcher.Invoke(() =>
                    {
                        FadeOut(block);
                        Task.Delay(1000).Wait();
                        Canvas.SetZIndex(block, -2);
                    });
                }
                else
                {
                    await Dispatcher.Invoke(async () =>
                    {
                        FadeOut(block);
                        await Task.Delay(1000);
                        await messagebox("Unsupported product detected!", "Okay", ":(", true, 12);
                    });

                }
            }
        }

        private async void Boot_Click(object sender, RoutedEventArgs e)
        {
            if (await messagebox("Make sure no other android device is connected except your Poco X3 Pro, not following this may lead to consequences you will regret, THIS PROCESS CANNOT BE STOPPED WHEN STARTED! MAKE SURE ALL YOUR DATA IS BACKED UP! Connect your phone in TWRP and press continue to start.", "Continue", "Cancel", false, 12) == "Continue")
            {
                Canvas.SetZIndex(block, 2);
                FadeIn(block);
                await Task.Delay(1000);
                ReportProgress(0, "Booting UEFI...");
                var WaitForDevice = new Action(() =>
                {
                    ReportProgress(0, "Waiting for device...");
                    StartADB("wait-for-usb-recovery").Wait();
                    ReportProgress(0, "We are in TWRP!");
                });

                await Task.Factory.StartNew(WaitForDevice);

                string device = (await StartADB("shell getprop ro.product.device")).Trim();
                if (device == "vayu" || device == "bhima")
                {
                    ReportProgress(0, "Poco X3 Pro Detected! Continuing...");
                    ReportProgress(0, "Downloading UEFI...");

                    var Download = new Action(() =>
                    {
                        string UEFI = JObject.Parse(DownloadString("https://api.github.com/repos/degdag/edk2-msm/releases/latest"))["assets"][0]["browser_download_url"].ToString().Replace("{", "").Replace("}", "");

                        DownloadFile(UEFI, "UEFI.zip");
                        ReportProgress(0, "Finished Downloading UEFI");
                    });

                    await Task.Factory.StartNew(Download);

                    var Unzip = new Action(() =>
                    {
                        ReportProgress(0, "Unzipping files");
                        ZipFile.ExtractToDirectory("UEFI.zip", Directory.GetCurrentDirectory());
                        ReportProgress(0, "Unzipped files");
                    });

                    await Task.Factory.StartNew(Unzip);

                    string gptsize = (await StartADB("shell parted /dev/block/sda print")).Split('\n')[1].Trim();
                    string CmdLine = await StartADB("shell cat /proc/cmdline");
                    string DisplayManufacturer = "";
                    int Ram = 0;


                    var GetDeviceInfo = new Action(() =>
                    {

                        if (gptsize == "Disk /dev/block/sda: 255GB")
                        {
                            Ram = 8;
                            ReportProgress(0, "RAM Size (Assumed via Storage): 8GB");
                        }

                        if (CmdLine.Contains("msm_drm.dsi_display0=dsi_j20s_42_02_0b_video_display:"))
                        {
                            DisplayManufacturer = "Huaxing";
                            ReportProgress(0, "Display Manufacturer: Huaxing");
                        }
                        else
                        {
                            DisplayManufacturer = "Tianma";
                            ReportProgress(0, "Display Manufacturer: Tianma");
                        }
                    });

                    await Task.Factory.StartNew(GetDeviceInfo);

                    var BootUEFI = new Action(async () =>
                    {
                        Fastboot vyu = new();
                        StartADB("shell twrp reboot bootloader").Wait();
                        Task.Delay(10000).Wait(); // 10 Second Delay
                        vyu.Connect();
                        string Folder = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(f => f.Contains("vayu-uefi")).First();
                        string File = Directory.GetFiles(Folder).Where(d => d.Contains($@"{DisplayManufacturer.ToLower()}-{Ram}gb")).First();
                        vyu.Boot(File);
                    });

                    await Task.Factory.StartNew(BootUEFI);

                    var DeleteFiles = new Action(() =>
                    {
                        string Folder = Directory.GetDirectories(Directory.GetCurrentDirectory()).Where(f => f.Contains("vayu-uefi")).First();

                        ReportProgress(0, "Deleting files...");
                        Directory.Delete(Folder, true);
                        File.Delete("UEFI.zip");
                    });

                    await Task.Factory.StartNew(DeleteFiles);

                    ReportProgress(0, "UEFI Booted.");
                    poo.ScrollToEnd();

                    Dispatcher.Invoke(() =>
                    {
                        FadeOut(block);
                        Task.Delay(1000).Wait();
                        Canvas.SetZIndex(block, -2);
                    });
                }
            }
            else
            {
                await Dispatcher.Invoke(async () =>
                {
                    FadeOut(block);
                    await Task.Delay(1000);
                    await messagebox("Unsupported product detected!", "Okay", ":(", true, 12);
                });

            }
        }
            private void Drag_MouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    this.DragMove();
                }
            }

        private async void Close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left && Panel.GetZIndex(block) != 2)
            {
                Shift(Borf, Borf.Margin, new Thickness(336));
                await Task.Delay(1000);
                Environment.Exit(0);
            }
        }

        private async void Mini_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ChangedButton == MouseButton.Left)
            {
                Shift(Borf, Borf.Margin, new Thickness(336));
                await Task.Delay(1000);
                this.WindowState = WindowState.Minimized;
            }
        }

        private void Rein_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Activated(object sender, EventArgs e)
        {
            Shift(Borf, Borf.Margin, new Thickness(10));
        }
    }
}
