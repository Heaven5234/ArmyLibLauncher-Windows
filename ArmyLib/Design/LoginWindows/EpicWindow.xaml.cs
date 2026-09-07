using ArmyLib.Source;
using Microsoft.Web.WebView2.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;
using System.Linq;

namespace ArmyLib.Design
{
    public partial class EpicWindow : Window
    {
        string baseUrl = "https://www.epicgames.com/id/login?redirectUrl=";
        string redirectUrl = $"https://www.epicgames.com/id/api/redirect?clientId={EpicLoginHandler.clientID}&responseType=code";

        public static string AccessToken { get; private set; }

        HttpClient _httpClient = new HttpClient();

        public EpicWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await LoginPage.EnsureCoreWebView2Async(null);

            try
            {
                var cookieManager = LoginPage.CoreWebView2.CookieManager;
                var cookies = await cookieManager.GetCookiesAsync("https://www.epicgames.com" );
                
                foreach(var cookie in cookies.ToList())
                {
                    cookieManager.DeleteCookie(cookie);
                }
            }

            catch(Exception ex)
            {
                Debug.WriteLine($"Cookie temizleme hatası: {ex.Message}");
            }

            LoginPage.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;

            string url = baseUrl + Uri.EscapeDataString(redirectUrl);

            LoginPage.Source = new Uri(url);
        }

        private async void CoreWebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            Uri uri = LoginPage.Source;

            Debug.WriteLine($"Path: {uri.AbsolutePath}");

            if (uri.Host != "www.epicgames.com") { this.Close(); return; }

            if (uri.AbsolutePath == "/id/api/redirect")
            {
                await Task.Delay(200);
                string content = await LoginPage.ExecuteScriptAsync("document.body.innerText");

                if (!string.IsNullOrEmpty(content) && content != null)
                {
                    try
                    {
                        string json = JsonSerializer.Deserialize<string>(content);

                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("authorizationCode", out var authCodeProp))
                            {
                                string authCode = authCodeProp.GetString();

                                Debug.WriteLine($"Authorization code : {authCode}");

                                if (!string.IsNullOrEmpty(authCode))
                                {
                                    this.Visibility = Visibility.Hidden;

                                    AccessToken = await EpicLoginHandler.ExchangeCodeForToken(authCode);

                                    if (!string.IsNullOrEmpty(AccessToken))
                                    {
                                        Debug.WriteLine("Epic games library has pulled succesfully");
                                    }

                                    else
                                    {
                                        Debug.WriteLine("Epic games library cannot pulled");
                                    }

                                    this.Close();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CoreWebView2_NavigationCompleted[EpicAPI] :  {ex.Message}");
                    }
                }
            }
        }
    }
}
