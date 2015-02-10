using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CourseraDownloader.Model;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using HtmlAgilityPack;
using Ookii.Dialogs.Wpf;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace CourseraDownloader {
    /// <summary>
    /// CourseWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CourseWindow : Window {
        private Student _student;
        private CourseSection _courses;
        private HtmlNodeCollection _materialsCollection;
        private List<Material> _currentMaterialsList;
        private int _reconnect;
        private object _locker;
        
        public int CurrentDownloadMaterialCount { get; set; }
        public int DownloadCount { get; set; }

        public CourseWindow(Student newStudent) {
            InitializeComponent();

            Initialize(newStudent);
        }

        public void Initialize(Student newStudent) {
            _student = newStudent;
            this.greetLabel.Content = String.Format("Hi, {0}", _student.Name);
            _student.DownloadDoneEvent += OnDownloadFinishHandler;
            this.CurrentDownloadMaterialCount = 0;
            this.DownloadCount = 0;
            this._reconnect = 0;
            this._locker = new object();

            // update course combobox
            _courses = CourseSection.GetConfigSection();
            IList<CourseElement> courseList = new List<CourseElement>();
            for(int i = 0; i < _courses.Courses.Count; i++){
                courseList.Add(_courses.Courses[i]);
            }

            this.courseComboBox.ItemsSource = courseList;
            this.courseComboBox.DisplayMemberPath = "Name";
            this.courseComboBox.SelectedValuePath = "Name";

        }

        private void OnDownloadFinishHandler(object sender, EventArgs e) {
            lock (_locker) {
                Console.WriteLine(++this.DownloadCount);
                this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                    this.downloadProgressBar.Value = this.DownloadCount;
                }));

                //MessageBox.Show("Download Finished!");
                if (this.DownloadCount == CurrentDownloadMaterialCount) {
                    MessageBox.Show("Download Finished!", "Download Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DownloadCount = 0;
                    this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                        this.downloadProgressBar.Value = this.DownloadCount;
                    }));

                    this.BatchEnable();
                }
            }
        }

        private void BatchDisable() {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                this.courseComboBox.IsEnabled = false;
                this.unitComboBox.IsEnabled = false;
                this.browseButton.IsEnabled = false;
                this.downloadButton.IsEnabled = false;
                this.downloadPathTextBox.IsEnabled = false;
                this.materialDataGrid.IsEnabled = false;
            }));
        }

        private void BatchEnable() {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                this.courseComboBox.IsEnabled = true;
                this.unitComboBox.IsEnabled = true;
                this.browseButton.IsEnabled = true;
                this.downloadButton.IsEnabled = true;
                this.downloadPathTextBox.IsEnabled = true;
                this.materialDataGrid.IsEnabled = true;
            }));
        }

        private void courseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string selectedCourse = ((ComboBox)sender).SelectedValue.ToString();
            int selectedIndex = ((ComboBox)sender).SelectedIndex;

            if (_materialsCollection != null) {
                _materialsCollection.Clear();
            }
            
            this.BatchDisable();

            Task getMaterialTask = Task.Run(() => {
                _materialsCollection = _student.GetMaterials(_courses.Courses[selectedIndex].Url); 
            });

            getMaterialTask.ContinueWith((s) => {
                //_materialsCollection = _student.GetMaterials(_courses.Courses[selectedIndex].Url);
                if (_materialsCollection != null) {
                    // update the week combobox
                    Dispatcher.BeginInvoke((Action)(() => {
                        this.unitComboBox.Items.Clear();

                        for (int i = 0; i < _materialsCollection.Count; i++) {
                            this.unitComboBox.Items.Add(String.Format("Unit {0}", i + 1));
                        }
                        this.BatchEnable();
                    }));
                }
                else {

                    //if (_reconnect++ < 5) {
                    //    _student.Login();
                    //    courseComboBox_SelectionChanged(sender, e);
                    //}
                    //else {
                    //    MessageBox.Show("Fail to get page.\n Please login again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    //    _reconnect = 0;
                    //}

                    MessageBoxResult mbResult = MessageBox.Show("Fail to get page.\n Login again?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (mbResult == MessageBoxResult.Yes) {
                        _student.Login();
                        BatchEnable();
                        //courseComboBox_SelectionChanged(sender, e);
                    }
                    else {
                        this.Dispatcher.BeginInvoke((Action)(() => this.Close()));
                    }
                }
            });
        }

        private void unitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            int selectedIndex = ((ComboBox)sender).SelectedIndex;

            if (selectedIndex >= 0) {
                string selectedCourse = this.courseComboBox.SelectedValue.ToString();
                //MessageBox.Show(String.Format("{0} {1}", selectedCourse, "week " + selectedIndex));

                _currentMaterialsList = new List<Material>();

                HtmlNode materialContainer = _materialsCollection.ElementAt(selectedIndex);
                HtmlNodeCollection titlesCollection = materialContainer.SelectNodes(@".//a[@class='lecture-link']");
                HtmlNodeCollection resourcesCollection = materialContainer.SelectNodes(@".//div[@class='course-lecture-item-resource']");

                for (int i = 0; i < titlesCollection.Count; i++) {
                    Material tmpMaterial = new Material(titlesCollection.ElementAt(i).InnerText.Trim());

                    HtmlNodeCollection currentMaterialResourcesCollection = resourcesCollection.ElementAt(i).SelectNodes(@".//a");
                    foreach (var item in currentMaterialResourcesCollection) {
                        tmpMaterial.DownloadLinkList.Add(item.GetAttributeValue("href", "FailToGetHref"));
                    }

                    _currentMaterialsList.Add(tmpMaterial);
                    /*
                    foreach (var item in currentMaterialResourcesCollection) {
                        Console.WriteLine(item.GetAttributeValue("href", "FailToGetHref"));
                    }
                     * */
                }

                this.materialDataGrid.ItemsSource = _currentMaterialsList;
            }
        }

        private void selectAllCheckBox_Checked(object sender, RoutedEventArgs e) {
            foreach (Material item in _currentMaterialsList) {
                item.IsSelected = true;
            }
        }

        private void selectAllCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            foreach (Material item in _currentMaterialsList) {
                item.IsSelected = false;
            }
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e) {
            /*
            int count = 0;
            foreach (Material item in _currentMaterialsList) {
                Console.WriteLine(String.Format("{0}: {1}", count++, item.IsSelected));
            }
             * */
            if (this.downloadPathTextBox.Text == "" || this.downloadPathTextBox.Text == null) {
                MessageBox.Show("Please select the download path ...", "Invalid path", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool isDownloadListEmpty = true;
            foreach (Material m in _currentMaterialsList) {
                if (m.IsSelected) {
                    isDownloadListEmpty = false;
                }
            }

            if (isDownloadListEmpty) {
                MessageBox.Show("Please select at least 1 item ...", "Empty Download", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else {
                this.BatchDisable();

                this.CurrentDownloadMaterialCount = 0;
                foreach (Material m in _currentMaterialsList) {
                    if (m.IsSelected) CurrentDownloadMaterialCount += m.DownloadLinkList.Count;
                }

                this.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
                    this.downloadProgressBar.Maximum = this.CurrentDownloadMaterialCount;
                }));

                // Here begins to execute the downloading on a new thread !
                // Actually, multithreading task that can avoid freezing the UI should be started at this point, 
                // if not, this line of code will run the main thread, so it is not weird that the UI would still be frozen 
                // even I think I have applied the multithread method.
                string downloadPath = this.downloadPathTextBox.Text;
                Task downloadTask = Task.Run(() => _student.DownloadMaterials(_currentMaterialsList, downloadPath));
            }
        }

        private void browseButton_Click(object sender, RoutedEventArgs e) {
            VistaFolderBrowserDialog folderBrowserDialog = new VistaFolderBrowserDialog();
            folderBrowserDialog.Description = "Please select a folder.";
            folderBrowserDialog.UseDescriptionForTitle = true;
            if ((bool)folderBrowserDialog.ShowDialog(this)) {
                // MessageBox.Show(this, "The selected folder was: " + folderBrowserDialog.SelectedPath, "Folder browser dialog"); 
                this.downloadPathTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }
    }

    public class Material : INotifyPropertyChanged {
        private bool _isSelected;

        public string Title { get; set; }
        public List<string> DownloadLinkList { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null) { 
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool IsSelected {
            get {
                return this._isSelected;
            }
            set {
                this._isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public Material(string newTitle) {
            this.Title = newTitle;
            this.IsSelected = false;
            this.DownloadLinkList = new List<string>();
        }
    }
}
