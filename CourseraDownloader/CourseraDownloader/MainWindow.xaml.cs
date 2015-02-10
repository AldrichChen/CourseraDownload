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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Configuration;
using System.Net;
using HtmlAgilityPack;

namespace CourseraDownloader
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Configuration config;
        private Student student;

        public MainWindow()
        {
            InitializeComponent();

            Initialize();
        }

        private void Initialize() {
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            string email = ConfigurationManager.AppSettings["email"];
            string password = ConfigurationManager.AppSettings["password"];
            string remember = ConfigurationManager.AppSettings["remember"];

            if (email != null && password != null) {
                this.emailTextBox.Text = email;
                this.passwordTextBox.Password = password;
            }

            if (remember == "true") {
                this.rememberCheckbox.IsChecked = true;
            }
            else {
                this.rememberCheckbox.IsChecked = false;
            }
        }

        private void SaveAccount(string email, string password) {
            config.AppSettings.Settings["email"].Value = email;
            config.AppSettings.Settings["password"].Value = password;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appsettings");
        }

        private void rememberCheckbox_Click(object sender, RoutedEventArgs e) {
            CheckBox checkBox = (CheckBox)sender;
            if ((bool)checkBox.IsChecked) {
                config.AppSettings.Settings["remember"].Value = "true";
            }
            else {
                config.AppSettings.Settings["remember"].Value = "false";
            }
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
            // here you should run the .exe(inside bin/Debug) instead of running in visual studio, 
            // the latter will not work. But i don't know the reason.
        }

        private void resetButton_Click(object sender, RoutedEventArgs e) {
            this.emailTextBox.Text = "";
            this.passwordTextBox.Password = "";

            SaveAccount("", "");
        }

        private void loginButton_Click(object sender, RoutedEventArgs e) {
            bool isRemember = (bool)this.rememberCheckbox.IsChecked;
            string email = this.emailTextBox.Text;
            string password = this.passwordTextBox.Password;

            student = new Student(new Account(email, password), ConfigurationManager.AppSettings["name"]);

            HttpStatusCode status = student.Login();
            if (status == HttpStatusCode.OK) {
                if (isRemember) {
                    SaveAccount(email, password);
                }
                CourseWindow courseWindow = new CourseWindow(student);
                courseWindow.Show();
                this.Close();
            }
            else {
                MessageBox.Show("Fail to login", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

    }
}
