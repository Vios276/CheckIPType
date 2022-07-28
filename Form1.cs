using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management;
using System.Net.NetworkInformation;

namespace CheckIPType
{
    public partial class Form1 : Form
    {
        private const string KT = "172.30.1.254";
        private const string LG = "192.168.219.1";
        private const string SK = "192.168.35.1";
        private const string IPTIME = "192.168.0.1";

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
        }

        private async Task RefreshPage()
        {
            var localIp = GetLocalIP();
            var publicIp = await CheckPublicIP();
            lblPublicIP.Text = publicIp;

            if (localIp == publicIp)
            {
                //직결상황
            }
            else
            {
                //공유기 확인
                //kt 172.30.1.254
                //LG 192.168.219.1
                //SK 192.168.35.1
                //iptime 192.168.0.1

                string dns = "168.126.63.1";

                switch (lblGateway.Text)
                {
                    case KT:
                        radioButton1.Checked = true;
                        dns = "168.126.63.1";
                        break;

                    case SK:
                        radioButton2.Checked = true;
                        dns = "210.220.163.82";
                        break;

                    case LG:
                        radioButton3.Checked = true;
                        dns = "164.124.101.2";
                        break;

                    case IPTIME:
                        radioButton4.Checked = true;
                        break;

                    default:
                        radioButton5.Checked = true;
                        break;
                }

                var ipList = GetTraceRoute(dns).ToList();
                richTextBox1.AppendText("현재 컴퓨터\n");
                var counter = 0;
                for (int i = 0; i < 2; i++)
                {
                    switch (ipList[i].ToString())
                    {
                        case KT:
                            richTextBox1.AppendText("KT 공유기\n");
                            break;

                        case SK:
                            richTextBox1.AppendText("SK 공유기\n");
                            break;

                        case LG:
                            richTextBox1.AppendText("LG 공유기\n");
                            break;

                        case IPTIME:
                            richTextBox1.AppendText("LG 공유기\n");
                            break;

                        default:
                            if (counter++ != 0)
                            {
                                i = 3;
                                break;
                            }
                            richTextBox1.AppendText("사설 공유기 or 외부 라우터\n");
                            break;
                    }
                }
            }
        }

        private string GetLocalIP()
        {
            ManagementObjectSearcher ipAddress =
                new ManagementObjectSearcher("Select * from Win32_NetworkAdapterConfiguration Where IPEnabled = 'True'");

            foreach (ManagementObject obj in ipAddress.Get())
            {
                if (obj["IPAddress"] == null) continue;
                if (!(obj["IPAddress"] is Array)) continue;

                //WMI 인스턴스의 ["IPAddress"]에 해당하는 값을 string 배열로 Casting
                //0 번쨰가 ip에 해당한다.
                lblLocalIP.Text = ((string[])obj["IPAddress"])[0];

                // Subnet Mask 를 가지고 올때, 만약 string[] 과 같은 형식 캐스팅이 불안하다면,
                // 이와 같이 Linq의 OfType로 형식을 확인후 진행이 가능하다.
                var subnetMask = ((IEnumerable)obj["IPSubnet"]).OfType<string>()
                    .Select(f => f.ToString()).ToList();

                lblSubNet.Text = subnetMask[0];

                lblGateway.Text = ((string[])obj["DefaultIPGateway"])[0];
            }

            return lblLocalIP.Text;
        }

        private async Task<string> CheckPublicIP()
        {
            HttpClient client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 10);
            var response = await client.GetAsync("https://api.ip.pe.kr/");

            var publicIp = await response.Content.ReadAsStringAsync();

            return publicIp;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            lblLocalIP.Text = string.Empty;
            lblSubNet.Text = string.Empty;
            lblGateway.Text = string.Empty;
            lblPublicIP.Text = string.Empty;

            radioButton1.Checked = false;
            radioButton2.Checked = false;
            radioButton3.Checked = false;
            radioButton4.Checked = false;
            radioButton5.Checked = false;

            richTextBox1.Text = string.Empty;

            await RefreshPage();
        }

        public static IEnumerable<IPAddress> GetTraceRoute(string hostname)
        {
            // following are similar to the defaults in the "traceroute" unix command.
            const int timeout = 10000;
            const int maxTTL = 30;
            const int bufferSize = 32;

            byte[] buffer = new byte[bufferSize];
            new Random().NextBytes(buffer);

            using (var pinger = new Ping())
            {
                for (int ttl = 1; ttl <= maxTTL; ttl++)
                {
                    PingOptions options = new PingOptions(ttl, true);
                    PingReply reply = pinger.Send(hostname, timeout, buffer, options);

                    // we've found a route at this ttl
                    if (reply.Status == IPStatus.Success || reply.Status == IPStatus.TtlExpired)
                        yield return reply.Address;

                    // if we reach a status other than expired or timed out, we're done searching or there has been an error
                    if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
                        break;
                }
            }
        }
    }
}
