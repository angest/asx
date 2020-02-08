using CsvHelper;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace asx
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string log;
        public string Log
        {
            get { return log; }
            set
            {
                log = value;
                OnPropertyChanged("Log");
            }
        }

        private int? errorCount;
        public int? ErrorCount
        {
            get
            {
                return errorCount;
            }
            set
            {
                errorCount = value;
                OnPropertyChanged("ErrorCountString");
            }
        }
        public string ErrorCountString
        {
            get
            {
                if (ErrorCount.HasValue)
                {
                    return "Error: " + ErrorCount;
                }
                else
                {
                    return null;
                }
            }
        }

        private double progress;
        public double Progress
        {
            get { return progress; }
            set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        private Visibility showProgress = Visibility.Hidden;
        public Visibility ShowProgress
        {
            get { return showProgress; }
            set
            {
                showProgress = value;
                OnPropertyChanged("ShowProgress");
            }
        }

        private bool buttonEnabled = true;
        public bool ButtonEnabled
        {
            get { return buttonEnabled; }
            set
            {
                buttonEnabled = value;
                OnPropertyChanged("ButtonEnabled");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Start(object sender, RoutedEventArgs e)
        {
            ButtonEnabled = false;

            Log = "";
            ErrorCount = 0;
            Println("starting...");
            Progress = 0;
            ShowProgress = Visibility.Visible;

            Println("getting company list...");
            List<Company> companyList = await GetCompanyList();
            Progress = 5;

            Println("getting company data...");
            char letter = ' ';
            List<Company> companyData = new List<Company>();
            for (int i = 0; i < companyList.Count; i++)
            {
                string code = companyList[i].code;
                if (code[0] != letter)
                {
                    letter = code[0];
                    Print(letter.ToString());
                }

                Company company = await GetCompany(code);
                companyData.Add(company);
                Progress = 5 + 90 * (i + 1) / companyList.Count;
            }
            Println("");

            Println("exporting file...");
            ExportFile(companyData);
            Progress = 100;

            Println("all done");
            ButtonEnabled = true;
        }

        private async Task<List<Company>> GetCompanyList()
        {
            List<Company> companyList = new List<Company>();

            await Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    string csvString = client.DownloadString("http://www.asx.com.au/asx/research/ASXListedCompanies.csv");
                    Regex rgx = new Regex("^(.+),([A-Z0-9]{3}),(.+)$", RegexOptions.Multiline);
                    csvString = rgx.Replace(csvString, "\"$1\",\"$2\",\"$3\"");

                    using (TextReader csvReader = new StringReader(csvString))
                    {
                        csvReader.ReadLine();
                        csvReader.ReadLine();
                        csvReader.ReadLine();

                        CsvReader csv = new CsvReader(csvReader);
                        csv.Configuration.HasHeaderRecord = false;
                        companyList.AddRange(csv.GetRecords<Company>());
                    }
                }
            });

            companyList.Sort((x, y) => x.code.CompareTo(y.code));
            return companyList;
        }

        private async Task<Company> GetCompany(string code)
        {
            Company company = await Task.Run(() =>
            {
                using (var client = new WebClient())
                {
                    try
                    {
                        string json = client.DownloadString("http://www.asx.com.au/asx/1/company/" + code + "?fields=primary_share");
                        return JsonConvert.DeserializeObject<Company>(json);
                    }
                    catch
                    {
                        ErrorCount++;
                        Company c = new Company(null, code, null);
                        return c;
                    }
                }
            });

            return company;
        }

        private void ExportFile(List<Company> companyData)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV File|*.csv";
            saveFileDialog.ShowDialog(this);

            if (saveFileDialog.FileName != "")
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    var csv = new CsvWriter(writer);
                    csv.WriteRecords(companyData);
                }
                Process.Start("explorer.exe", "/select, " + saveFileDialog.FileName);
            }
        }

        private void Print(string message)
        {
            Log += message;
        }

        private void Println(string message)
        {
            Log += message;
            Log += Environment.NewLine;
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
