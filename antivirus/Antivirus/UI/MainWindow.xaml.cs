using Antivirus.Scan;
using System;
using System.Windows;
using Ookii.Dialogs.Wpf;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Antivirus.Util;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.ComponentModel;
using Antivirus.DB;

namespace Antivirus.UI
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FileScan SelectedScan { get; set; }

        private ScanManager scanManager;
        private DatabaseManager database;
        private SubscriptionManager subs = new SubscriptionManager();

        public MainWindow(ScanManager scanManager, DatabaseManager database)
        {
            this.scanManager = scanManager;
            this.database = database;
            this.DataContext = this;

            this.InitializeComponent();

            this.UpdateScans(this.scanManager.Scans);
            this.scanManager.OnScanCreated += OnScanCreated;
            this.scanManager.OnScanUpdated += OnScanUpdated;
        }

        private void OnScanCreated(FileScan scan)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Log($"Scan record of {scan.Path} created");
                this.UpdateScans(this.scanManager.Scans);
            });
        }

        private void OnScanUpdated(FileScan scan)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.Log($"Scan {scan.Path} updated, new state: {scan.State}");
                this.UpdateScans(this.scanManager.Scans);
            });
        }

        private void UpdateScans(List<FileScan> scans)
        {
            this.scanGrid.ItemsSource = scans;
            this.scanGrid.Items.Refresh();

            var scan = this.SelectedScan;
            this.SelectedScan = null;
            this.SelectedScan = scan;
        }

        private void Log(string data)
        {
            this.log.AppendText($"{DateTime.Now.ToString("HH:mm:ss.fff")}: {data}{Environment.NewLine}");
            this.log.Focus();
            this.log.CaretIndex = this.log.Text.Length;
            this.log.ScrollToEnd();
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog();
            dialog.Multiselect = true;
            dialog.CheckPathExists = true;
            dialog.Title = "Select file(s) to scan";
            var result = dialog.ShowDialog();
            if (result.GetValueOrDefault(false))
            {
                this.ScanFiles(dialog.FileNames.ToList());
            }
        }
        private void OpenDirectory(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select directory to scan";
            dialog.UseDescriptionForTitle = true;
            var result = dialog.ShowDialog();
            if (result.GetValueOrDefault(false))
            {
                this.ScanFiles(Directory.EnumerateFiles(dialog.SelectedPath).ToList());
            }
        }

        private void ScanFiles(List<string> files)
        {
            this.scanManager.ScanFiles(files);
        }

        private async void QuarantineScan(object sender, RoutedEventArgs e)
        {
            var scan = ((Button) sender).Tag as FileScan;

            try
            {
                if (scan.InQuarantine)
                {
                    await this.scanManager.UnlockFromQuarantine(scan);
                    this.Log($"Scan {scan.Path} restored from quarantine {scan.QuarantinePath}");
                }
                else
                {
                    await this.scanManager.LockInQuarantine(scan);
                    this.Log($"Scan {scan.Path} put into quarantine {scan.QuarantinePath}");
                }
                this.UpdateScans(this.scanManager.Scans);
            }
            catch (Exception ex)
            {
                this.Log(ex.Message);
            }
        }

        private void RemoveScan(object sender, RoutedEventArgs e)
        {
            var scan = ((Button)sender).Tag as FileScan;
            this.scanManager.Remove(scan);
            this.UpdateScans(this.scanManager.Scans);
        }

        private void HandleGridSelection(object sender, SelectionChangedEventArgs e)
        {
            this.SelectedScan = this.scanGrid.SelectedItem as FileScan;
        }

        private void HandleMenuPersistDatabase(object sender, RoutedEventArgs e)
        {
            this.database.Persist();
            this.Log($"Database persisted to {this.database.Path}");
        }
        private void HandleMenuExit(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
