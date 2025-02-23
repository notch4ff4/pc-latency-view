using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Globalization;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Windows.Media;
using Microsoft.Win32;

namespace pc_latency
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string currentDirectory;

        public MainWindow()
        {
            InitializeComponent();
            currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            UpdateCurrentFolderText();
            LoadCsvFiles();

            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            UpdateTheme();
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                UpdateTheme();
            }
        }

        private void UpdateTheme()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value != null)
                    {
                        bool isLightTheme = (int)value == 1;

                        if (isLightTheme)
                        {
                            Resources["WindowBackground"] = new SolidColorBrush(Colors.White);
                            Resources["WindowText"] = new SolidColorBrush(Colors.Black);
                            Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                        }
                        else
                        {
                            Resources["WindowBackground"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                            Resources["WindowText"] = new SolidColorBrush(Colors.White);
                            Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                        }
                    }
                }
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            base.OnClosing(e);
        }

        private void UpdateCurrentFolderText()
        {
            CurrentFolderText.Text = $"Текущая папка: {currentDirectory}";
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = currentDirectory,
                Description = "Выберите папку с CSV файлами"
            };

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                currentDirectory = dialog.SelectedPath;
                UpdateCurrentFolderText();
                LoadCsvFiles();
            }
        }

        private void LoadCsvFiles()
        {
            try
            {
                var csvFiles = Directory.GetFiles(currentDirectory, "*.csv")
                                     .Select(System.IO.Path.GetFileName)
                                     .ToList();

                FileList.ItemsSource = csvFiles;

                if (csvFiles.Any())
                {
                    FileList.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                FileList.ItemsSource = null;
            }
        }

        private void FileList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (FileList.SelectedItem is string selectedFile)
            {
                var result = ProcessCsvFile(selectedFile);
                if (result != null)
                {
                    MinLabel.Text = $"Минимум: {result.Min:F2} ms";
                    MeanLabel.Text = $"Среднее: {result.Mean:F2} ms";
                    MaxLabel.Text = $"Максимум: {result.Max:F2} ms";
                }
            }
        }

        private LatencyResult ProcessCsvFile(string fileName)
        {
            try
            {
                string filePath = System.IO.Path.Combine(currentDirectory, fileName);
                var lines = File.ReadAllLines(filePath);

                if (lines.Length < 2)
                {
                    MessageBox.Show($"Файл {fileName} пуст или содержит только заголовки.",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    return null;
                }

                var headers = lines[0].Split(',');
                int latencyColumnIndex = -1;

                for (int i = 0; i < headers.Length; i++)
                {
                    var header = headers[i].Trim('"');
                    if (header == "AllInputToPhotonLatency" || header == "MsPCLatency")
                    {
                        latencyColumnIndex = i;
                        break;
                    }
                }

                if (latencyColumnIndex == -1)
                {
                    MessageBox.Show($"Нет столбца 'AllInputToPhotonLatency' или 'MsPCLatency' в файле {fileName}.",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    return null;
                }

                var values = new List<double>();

                for (int i = 1; i < lines.Length; i++)
                {
                    var columns = lines[i].Split(',');
                    if (columns.Length > latencyColumnIndex)
                    {
                        var value = columns[latencyColumnIndex].Trim('"');
                        if (value != "NA" && double.TryParse(value,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double latency))
                        {
                            values.Add(latency);
                        }
                    }
                }

                if (!values.Any())
                {
                    MessageBox.Show($"Нет валидных численных значений в столбце латентности.",
                                  "Ошибка",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    return null;
                }

                return new LatencyResult
                {
                    Min = values.Min(),
                    Mean = values.Average(),
                    Max = values.Max()
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось обработать файл {fileName}.\n{ex.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                return null;
            }
        }
    }

    public class LatencyResult
    {
        public double Min { get; set; }
        public double Mean { get; set; }
        public double Max { get; set; }
    }
}
