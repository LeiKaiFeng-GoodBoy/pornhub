﻿using Xamarin.Essentials;
using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeiKaiFeng.Http;
using LeiKaiFeng.Pornhub;
using System.Linq;

namespace pornhub
{

    public sealed class Source
    {
        //public byte[] MainPageCer { get; set; }

        public byte[] AdPageCer { get; set; }

        public byte[] AdVideo { get; set; }
    }


    static class Info
    {
        public static string ReplaceResponseHtml(string html)
        {
            return html;

            //return new StringBuilder(html)
            //    .Replace("ci.", "ei.")
            //    .Replace("di.", "ei.")
            //    .ToString();
        }

        public static bool CheckingVideoHtml(string html)
        {
            if (html.Contains("/ev-h.p") ||
                html.Contains("/ev.p"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task<MHttpStream> CreateRemoteStream()
        {
            const string HOST = "www.livehub.com";

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await socket.ConnectAsync(HOST, 443).ConfigureAwait(false);

            SslStream sslStream = new SslStream(new NetworkStream(socket, true), false, (a, b, c, d) => true);


            await sslStream.AuthenticateAsClientAsync(HOST, null, System.Security.Authentication.SslProtocols.Tls12, false).ConfigureAwait(false);

            return new MHttpStream(socket, sslStream);
        }




        public static Func<Stream, string, Task<Stream>> CreateLocalStream(X509Certificate certificate)
        {
            return async (stream, host) =>
            {

                SslStream sslStream = new SslStream(stream, false);

                await sslStream.AuthenticateAsServerAsync(certificate).ConfigureAwait(false);


                return sslStream;
            };

        }
    }

    public partial class MainPage : ContentPage
    {


        public MainPage(Source source)
        {
            InitializeComponent();

            const string PFX_BASE64_TEXT = "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";

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 43433);

            PacServer pacServer = PacServer.Start(new IPEndPoint(IPAddress.Loopback, 59237),
                PacServer.Create(endPoint, "cn.pornhub.com", "hw-cdn2.adtng.com", "ht-cdn2.adtng.com", "vz-cdn2.adtng.com"),
                PacServer.Create(new IPEndPoint(IPAddress.Loopback, 80), "www.pornhub.com", "hubt.pornhub.com"));

            PornhubProxyInfo info = new PornhubProxyInfo
            {
                MainPageStreamCreate = Info.CreateLocalStream(new X509Certificate2(Convert.FromBase64String(PFX_BASE64_TEXT))),

                ADPageStreamCreate = Info.CreateLocalStream(new X509Certificate2(source.AdPageCer)),

                RemoteStreamCreate = Info.CreateRemoteStream,

                MaxContentSize = 1024 * 1024 * 5,

                ADVideoBytes = source.AdVideo,

                CheckingVideoHtml = Info.CheckingVideoHtml,

                MaxRefreshRequestCount = 30,

                ReplaceResponseHtml = Info.ReplaceResponseHtml,


            };

            PornhubProxyServer server = new PornhubProxyServer(info);


            Task t = server.Start(endPoint);


            m_copyUri.Clicked += (obj, e) =>
            {
                Xamarin.Essentials.Clipboard.SetTextAsync(pacServer.ProxyUri.AbsoluteUri);
            };
        }

        void OnOpenBrowser(object sender, EventArgs e)
        {
            Browser.OpenAsync("https://cn.pornhub.com/", BrowserLaunchMode.SystemPreferred);
        }
    }
}