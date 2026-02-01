// Copyright (C) 2024 Stratum Contributors
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Autofac;
using Serilog;
using Stratum.Desktop.Persistence;
using Stratum.Desktop.Services;

namespace Stratum.Desktop
{
    public partial class App : Application
    {
        private const string MutexName = "Global\\Stratum.Desktop.SingleInstance";
        private const string PipeName = "Stratum.Desktop.Activation";

        public static IContainer Container { get; private set; }
        public static Database Database { get; private set; }

        private Mutex _singleInstanceMutex;
        private CancellationTokenSource _pipeCancellation;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                if (!AcquireSingleInstance())
                {
                    SignalExistingInstance();
                    Shutdown(0);
                    return;
                }

                _pipeCancellation = new CancellationTokenSource();
                _ = StartPipeServerAsync(_pipeCancellation.Token);

                InitializeLogging();
                EnsureDataDirectory();
                SQLitePCL.Batteries_V2.Init();

                var mainWindow = new MainWindow();
                mainWindow.Show();

                await Task.Run(async () =>
                {
                    Database = new Database();
                    Container = Dependencies.Build(Database);
                    await Database.OpenAsync(null, Database.Origin.Application);
                });

                await mainWindow.InitializeViewModelAsync();

                var prefManager = Container.Resolve<PreferenceManager>();
                var locManager = Container.Resolve<LocalizationManager>();
                locManager.SetLanguage(prefManager.Preferences.Language);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(500);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    Log.Information("Startup GC completed");
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start application");
                MessageBox.Show(
                    $"Failed to start: {ex.Message}\n\n{ex.StackTrace}",
                    "Stratum",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private void LoadApplicationResources()
        {
            try
            {
                // Load styles and animations
                var stylesDict = new ResourceDictionary
                {
                    Source = new Uri("Resources/Styles.xaml", UriKind.Relative)
                };
                
                var animationsDict = new ResourceDictionary
                {
                    Source = new Uri("Resources/Animations.xaml", UriKind.Relative)
                };

                Resources.MergedDictionaries.Add(animationsDict);
                Resources.MergedDictionaries.Add(stylesDict);
                
                Log.Information("Application resources loaded successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load application resources");
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled dispatcher exception");
            MessageBox.Show(
                $"Unhandled error: {e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "Stratum Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Error(ex, "Unhandled domain exception");
            MessageBox.Show(
                $"Fatal error: {ex?.Message}\n\n{ex?.StackTrace}",
                "Stratum Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _pipeCancellation?.Cancel();
            _pipeCancellation?.Dispose();

            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();

            if (Database != null)
            {
                await Database.CloseAsync(Database.Origin.Application);
            }

            Log.CloseAndFlush();
            base.OnExit(e);
        }

        private static void InitializeLogging()
        {
#if DEBUG
            var logPath = Path.Combine(GetDataDirectory(), "logs", "stratum-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .WriteTo.Console()
                .CreateLogger();
#else
            // Release: minimal logging, only errors
            var logPath = Path.Combine(GetDataDirectory(), "logs", "stratum-.log");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 3)
                .CreateLogger();
#endif

            Log.Information("Stratum Desktop starting");
        }

        private static void EnsureDataDirectory()
        {
            var dataDir = GetDataDirectory();
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(Path.Combine(dataDir, "logs"));
        }

        public static string GetDataDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Stratum");
        }

        private bool AcquireSingleInstance()
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
            return createdNew;
        }

        private async Task StartPipeServerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(cancellationToken);

                    if (server.IsConnected)
                    {
                        _ = server.ReadByte();
                        Dispatcher.Invoke(ActivateMainWindow);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Pipe server error");
                }
            }
        }

        private void SignalExistingInstance()
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1000);
                client.WriteByte(1);
                client.Flush();
                Log.Information("Signaled existing instance to activate");
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to signal existing instance");
            }
        }

        private void ActivateMainWindow()
        {
            var window = MainWindow;
            if (window == null) return;

            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;

            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;

            Log.Information("Main window activated");
        }

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
    }
}
