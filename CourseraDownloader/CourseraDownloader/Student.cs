using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.Web;
using System.Diagnostics;
using CourseraDownloader.Model;
using System.Threading;
using System.Text.RegularExpressions;


namespace CourseraDownloader {
    public class Student {
        private Account _account;
        private Uri _loginUri;
        private CookieContainer _cookie;
        private string[] _cookieString;
        private object locker = new Object();

        public string Name { get; set; }

        public Student() { }

        public Student(Account newAccount, string newName) {
            this._account = newAccount;
            this.Name = newName;
            _loginUri = new Uri("https://accounts.coursera.org/api/v1/login");
            _cookie = new CookieContainer();
            HtmlDocument tmp = new HtmlDocument();
        }

        public HttpStatusCode Login() {
            string csrfToken1 = ConstructToken(24);
            string csrfToken2 = ConstructToken(24);
            string csrf2Cookie = "csrf_token_" + ConstructToken(8);

            string formData = String.Format("email={0}&password={1}&webrequest=true", this._account.email, this._account.password);
            byte[] binFormData = Encoding.ASCII.GetBytes(formData);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(this._loginUri);
            request.Method = "POST";
            request.Host = "accounts.coursera.org";
            request.ContentType = "application/x-www-form-urlencoded";
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22";
            request.Accept = "*/*";
            request.Referer = "https://accounts.coursera.org/signin";
            // Remember dont add 'User-Agent'/'Accept' etc. attribute into headers, because they have been defined 
            // in the HttpWebRequest Class
            request.Headers.Add(new NameValueCollection{
                {"X-CSRFToken", csrfToken1},
                {"X-CSRF2-Cookie", csrf2Cookie},
                {"X-CSRF2-Token", csrfToken2},
                {"X-Requested-With", "XMLHttpRequest"},
                {"Cookie", String.Format("csrftoken={0}; {1}={2}", csrfToken1, csrf2Cookie, csrfToken2)}
            });

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(binFormData, 0, binFormData.Length);
            requestStream.Flush();
            requestStream.Close();

            try {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Console.WriteLine("Status code: {0}", response.StatusCode);
                Console.WriteLine("Status description: {0}", response.StatusDescription);

                // construct cookie
                _cookie.Add(request.RequestUri, new Cookie("csrftoken", csrfToken1));
                _cookie.Add(request.RequestUri, new Cookie(csrf2Cookie, csrfToken2));
                string[] tmp = response.Headers.GetValues("Set-Cookie");
                string[] responseCookie = {tmp[0] + tmp[1], tmp[2] + tmp[3]};
                this._cookieString = responseCookie;
                foreach(var cookieHeader in responseCookie){
                    var cookieTokens = cookieHeader.Split('=', ';');
                    string cookieName = cookieTokens[0];
                    string cookieValue = cookieTokens[1];
                    _cookie.Add(new Uri("https://www.coursera.org"), new Cookie(cookieName, cookieValue));
                    _cookie.Add(new Uri("https://class.coursera.org"), new Cookie(cookieName, cookieValue));
                }

                return response.StatusCode;
            }
            catch (WebException e) {
                WebResponse response = e.Response;
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                Console.WriteLine("Error description: {0}", httpResponse.StatusDescription);
                Stream responseStream = response.GetResponseStream();
                var reader = new StreamReader(responseStream);
                string text = reader.ReadToEnd();
                Console.WriteLine(text);
                return httpResponse.StatusCode;
            }
        }

        public HtmlNodeCollection GetMaterials(string uri) {
            // httpwebrequest method
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri + "/index");
            //request.Method = "GET";
            //request.Host = "class.coursera.org";
            //request.Accept = @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            //request.Referer = uri;
            //request.UserAgent = @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22";
            //request.CookieContainer = this._cookie;

            // httpwebclient method
            HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, CookieContainer = this._cookie });
            var headers = new Dictionary<string, string>{
                {"User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22"},
                {"Referer", "https://www.coursera.org/"},
                {"HOST", "class.coursera.org"},
                {"Accept", @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8"}
            };
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri(uri + "/index");
            request.Method = HttpMethod.Get;
            foreach (var kvp in headers) {
                switch (kvp.Key.ToUpperInvariant()) {
                    case "HOST":
                        request.Headers.Host = kvp.Value;
                        break;
                    case "REFERER":
                        request.Headers.Referrer = new Uri(kvp.Value);
                        break;
                    default:
                        request.Headers.Add(kvp.Key, kvp.Value);
                        break;
                }
            }

            try {
                // httpwebrequest method
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //Console.WriteLine("Status code: {0}", response.StatusCode);
                //Console.WriteLine("Status description: {0}", response.StatusDescription);

                //StreamReader streamReader = new StreamReader(response.GetResponseStream());
                //string doc = streamReader.ReadToEnd();

                //HtmlDocument htmlDoc = new HtmlDocument();
                //htmlDoc.LoadHtml(doc);

                // httpwebclient method
                var response = client.SendAsync(request).Result;
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(response.Content.ReadAsStringAsync().Result);

                HtmlNode materialsContainer = htmlDoc.DocumentNode.SelectSingleNode(@".//div[@class='course-item-list']");

                if (materialsContainer != null) {
                    return materialsContainer.SelectNodes(@".//ul[@class='course-item-list-section-list']");
                }
                else {
                    Thread.Sleep(3000);
                    return null;
                }
            }
            catch (WebException e) {
                WebResponse response = e.Response;
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                Console.WriteLine("Error description: {0}", httpResponse.StatusDescription);
                Stream responseStream = response.GetResponseStream();
                var reader = new StreamReader(responseStream);
                string text = reader.ReadToEnd();
                Console.WriteLine(text);
                return null;
            }
            
        }

        public void GetCourses() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.coursera.org/maestro/api/topic/list_my");
            request.Method = "GET";
            request.UserAgent = @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22";
            request.Host = "www.coursera.org";
            request.ContentType = "application/json";
            request.Accept = "*/*";
            request.Referer = "https://coursera.org/";
            request.CookieContainer = this._cookie;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader streamReader = new StreamReader(response.GetResponseStream());
            string json = streamReader.ReadToEnd();
            JArray deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<JArray>(json);
            foreach (var entry in deserialized) {
                Console.WriteLine(entry["courses"][0]["home_link"].ToString());
            }
        }

        public void DownloadMaterials(List<Material> currentMaterialsList, string path) {
            Console.WriteLine("Downloading ...");
            List<string> ToBeDownloadList = new List<string>();
            foreach (var item in currentMaterialsList) {
                if (item.IsSelected) {
                    Console.WriteLine(item.Title);
                    for (int i = 0; i < item.DownloadLinkList.Count; i++) {
                        //DownloadItem(item.DownloadLinkList[i], path);
                        ToBeDownloadList.Add(item.DownloadLinkList[i]);
                    }
                }
            }

            //DownloadItem(ToBeDownloadList[7], path);

            ParallelOptions option = new ParallelOptions { MaxDegreeOfParallelism = 5 };
            ParallelLoopResult result = Parallel.For(0, ToBeDownloadList.Count, (index) => {
                DownloadItem(ToBeDownloadList[index], path);
            });


        }

        private void DownloadItem(string link, string path) {
            try {
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //Console.WriteLine("Status code: {0}", response.StatusCode);
                //Console.WriteLine("Status description: {0}", response.StatusDescription);

                string linkSuffix = HttpUtility.UrlDecode(link).Split('/').Last();
                var formatInfo = linkSuffix.Split('?');

                // explicit download links
                if (formatInfo.Length <= 1) {
                    WebClient client = new WebClient();
                    string filename = CheckPath(linkSuffix);
                    var random = new Random(Guid.NewGuid().GetHashCode());

                    if (filename.Split('.').Last() == "srt") {
                        filename = filename.Replace(".srt", "(" + random.Next(1, 100).ToString() + ")" + ".srt");
                    }
                    client.DownloadFile(new Uri(link), path + "/" + filename);

                    //client.DownloadFileCompleted += this.DownloadDirectLink;

                    Console.WriteLine("DownloadDirectLink: " + link);
                    Console.WriteLine("DownloadDirectLink: " + path + "/" + linkSuffix);
                }
                else {
                    // if the link is subtitles, this filename will be the name of the file
                    // but if the link is mp4, this filename will be unprotectedUrl
                    string filename = UnprotectUrl(link);
                    if (formatInfo.First() == "subtitles") {
                        // correct the illegal filepath
                        filename = CheckPath(filename);
                        string savePath = path + "/" + filename;

                        // here cannot put i into the lambda(like item.DownloadLinkList[i]), or it will be too late
                        // to capture i, i.e. before the new thread starts, i have changed
                        //int tmp = i;
                        //Parallel.For(1, 1000, new ParallelOptions { MaxDegreeOfParallelism = 5 }, (x) => {

                        //    this.DownloadForcedLink(item.DownloadLinkList[tmp], savePath);
                        //});
                        //Thread thread1 = new Thread(() => this.DownloadForcedLink(link, savePath));
                        //thread1.Start();
                        DownloadForcedLink(link, savePath);

                        Console.WriteLine("DownloadForcedLink: " + link);
                        Console.WriteLine("DownloadForcedLink: " + savePath);

                    }
                    else if (formatInfo.First() == "download.mp4") {
                        string unprotectedUrl = filename;
                        // must check this path, because ':' is an invalid Windows filename character
                        // if this filename is invalid, DownloadFileAsync will not do anything but just
                        // raise the DownloadFileCompleted event! so you will find nothing under the directory
                        //string saveName = CheckPath(item.Title + ".mp4");
                        Console.WriteLine("DownloadMp4: " + HttpUtility.UrlDecode(unprotectedUrl));
                        //Console.WriteLine("DownloadMp4: " + path + "/" + saveName);

                        filename = HttpUtility.UrlDecode(Regex.Split(unprotectedUrl, "filename").Last());
                        filename = Regex.Split(filename, "UTF-8").Last();
                        string saveName = CheckPath(HttpUtility.UrlDecode(filename));

                        saveName = saveName.Remove(0, 2);

                        WebClient client = new WebClient();
                        client.DownloadFile(new Uri(unprotectedUrl), path + "/" + saveName);
                        //client.DownloadFileCompleted += this.DownloadDirectLink;

                    }
                    else {
                        Console.WriteLine("Invalid Url ...");
                    }

                }

            }
            catch (WebException e) {
                WebResponse response = e.Response;
                HttpWebResponse httpResponse = (HttpWebResponse)response;
                Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                Console.WriteLine("Error description: {0}", httpResponse.StatusDescription);
                Stream responseStream = response.GetResponseStream();
                var reader = new StreamReader(responseStream);
                string text = reader.ReadToEnd();
                Console.WriteLine(text);

            }
            catch (UriFormatException ue) {
                Console.WriteLine(ue.Message);
                //Console.WriteLine("UriFormatException ... (Invalid Uri)");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }

            EventHandler handler = DownloadDoneEvent;
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }

            return;
        }

        private string UnprotectUrl(string protectedUrl) {
            using (HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer =  this._cookie}) {
                using (HttpClient client = new HttpClient(handler)) {
                    var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Accept", "text/html, application/xhtml+xml, */*" }
                    };
                    HttpRequestMessage request = new HttpRequestMessage();
                    request.RequestUri = new Uri(protectedUrl);
                    request.Method = HttpMethod.Get;
                    foreach (var kvp in headers) {
                        switch (kvp.Key.ToUpperInvariant()) {
                            case "HOST":
                                request.Headers.Host = kvp.Value;
                                break;
                            case "REFERER":
                                request.Headers.Referrer = new Uri(kvp.Value);
                                break;
                            default:
                                request.Headers.Add(kvp.Key, kvp.Value);
                                break;
                        }
                    }

                    try {
                        var response = client.SendAsync(request).Result;
                        if (protectedUrl.Split('/').Last().Split('?').First() == "download.mp4") {
                            string location = response.Headers.GetValues("Location").First();
                            
                            return location;
                        }
                        else if (protectedUrl.Split('/').Last().Split('?').First() == "subtitles") {
                            string contentDispositionString = response.Content.Headers.GetValues("Content-Disposition").First();
                            string unprotectedUrl = contentDispositionString.Split(';').ElementAt(1).Split('=').Last().Replace("\"", "");
                            return HttpUtility.UrlDecode(unprotectedUrl);
                        }
                        else {
                            return protectedUrl;
                        }
                    }
                    catch (AggregateException e) {
                        Console.WriteLine("Inivalid download link: " + protectedUrl);
                        return "";
                    }
                }
            }
        }

        private string GetFilename(string url) {
            HttpMessageHandler handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = this._cookie };
            HttpClient client = new HttpClient(handler);
            var headers = new Dictionary<string, string>
                    {
                        { "User-Agent", @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22" },
                        { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" }
                    };
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri(url);
            request.Method = HttpMethod.Get;
            foreach (var kvp in headers) {
                switch (kvp.Key.ToUpperInvariant()) {
                    case "HOST":
                        request.Headers.Host = kvp.Value;
                        break;
                    case "REFERER":
                        request.Headers.Referrer = new Uri(kvp.Value);
                        break;
                    default:
                        request.Headers.Add(kvp.Key, kvp.Value);
                        break;
                }
            }

            // httpwebrequest method
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //request.Method = "GET";
            //request.Accept = @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            //request.UserAgent = @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22";
            //request.CookieContainer = this._cookie;


            try {
                var response = client.SendAsync(request).Result;
                string contentDisposition = response.Content.Headers.GetValues("Content-Disposition").First();
                contentDisposition = contentDisposition.Split(';').ElementAt(1).Split('=').Last().Replace("\"", "");

                return HttpUtility.UrlDecode(contentDisposition);
                //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //string contentDisposition = response.Headers.GetValues("Content-Disposition").First();
                //contentDisposition = contentDisposition.Split(';').ElementAt(1).Split('=').Last().Replace("\"", "");
                //return HttpUtility.UrlDecode(contentDisposition);
            }
            catch {
                Console.WriteLine("Inivalid download link: " + url);
                return "";
            }

            return "";
        }

        private string ConstructToken(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringBuilder = new StringBuilder();
            var random = new Random(Guid.NewGuid().GetHashCode());

            for (int i = 0; i < length; i++) {
                stringBuilder.Append(chars.ElementAt(random.Next(chars.Length)));
            }

            return stringBuilder.ToString();
        }

        public string[] GetCookieString() {
            return this._cookieString;
        }

        private string CheckPath(string originalPath) {
            //string illegal = "\"M\"\\a/ry/ h**ad:>> a\\/:*?\"| li*tt|le|| la\"mb.?";

            string newPath = originalPath;
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalid) {
                newPath = newPath.Replace(c.ToString(), "-");
            }
            return newPath;
        }

        public event EventHandler DownloadDoneEvent;

        private void DownloadForcedLink(string downloadLink, string savePath) {
            lock (this.locker) {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadLink);
                request.Method = "GET";
                request.UserAgent = @"Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.22 (KHTML, like Gecko) Chrome/25.0.1364.97 Safari/537.22";
                request.Accept = @"text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                //request.ContentType = "application/octet-stream";
                request.CookieContainer = this._cookie;
                request.AllowAutoRedirect = true;

                // safest method to write network stream to filesystem
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream remoteStream = response.GetResponseStream();
                Stream localStream = File.Create(savePath);

                byte[] buffer = new byte[1024 * 8];
                int bytesRead;
                int bytesProcessed = 0;
                //Stopwatch watch = Stopwatch.StartNew();
                while ((bytesRead = remoteStream.Read(buffer, 0, buffer.Length)) > 0) {
                    localStream.Write(buffer, 0, bytesRead);
                    localStream.Flush();
                    bytesProcessed += bytesRead;
                }
                //watch.Stop();
                localStream.Close();
                remoteStream.Close();
                //Console.WriteLine(String.Format("{0}: {1}ms", filename, watch.ElapsedMilliseconds));
                //EventHandler handler = DownloadDoneEvent;
                //if (handler != null) {
                //    Console.WriteLine(savePath + " is done!");
                //    handler(this, EventArgs.Empty);
                //}
            }
        }
    }

    public class Account {
        public Account(string newEmail, string newPassword) {
            this.email = newEmail;
            this.password = newPassword;
        }

        public string email { get; set; }
        public string password { get; set; }
    }

    //public class DownloadItemArgs {
    //    public string DownloadLink { get; set; }
    //    public string FilePath { get; set; }
    //}
}
