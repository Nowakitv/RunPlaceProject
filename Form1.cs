using Guna.UI2.WinForms;
using Microsoft.VisualBasic.Logging;
using Microsoft.Win32;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace RunPlace
{
    public partial class Form1 : Form
    {
        string versionToWrite = "1.3";

        private Login login = new Login();
        public bool isAuth;
        public bool ShowAds = true;
        private bool doneloading = false;
        private bool panelVisible = false;

        private string ipAddress = "Mysql.bourbonvpn.re";
        private long totalDownloadBytes;
        private long totalUploadBytes;
        private NetworkInterface? vpnInterface;
        private ToolTip toolTip1;
        private ToolTip toolTip2;
        Loading loading = new Loading();

        public Form1()
        {
            InitializeComponent();
        }

        private async void guna2GradientButton1_Click(object sender, EventArgs e)
        {
            bool IsLogin = login.UserLogin;
            bool Subscribed = login.UserSubscribe;

            if (IsLogin)
            {
                try
                {
                    if (guna2ComboBox2.SelectedItem.ToString() == "Windows")
                    {
                        BtnConnexion.Text = "Connexion en cours";
                        ConnexionTimer.Stop();
                        await ConnexionVPN();

                        BtnConnexion.Cursor = Cursors.Hand;
                        BtnConnexion.FillColor = Color.FromArgb(0, 238, 148);
                        BtnConnexion.FillColor2 = Color.FromArgb(0, 165, 246);
                    }
                    if (guna2ComboBox2.SelectedItem.ToString() == "OpenVPN")
                    {
                        BtnConnexion.Text = "Connexion en cours";
                        ConnexionTimer.Stop();

                        await Task.Run(async () =>
                        {
                            string tempFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                            Directory.CreateDirectory(tempFolderPath);

                            string configFilePath = await CreateOpenVPNConfigFile(tempFolderPath);

                            if (string.IsNullOrEmpty(configFilePath))
                            {
                                MessageBox.Show("Impossible de créer le fichier de configuration OpenVPN.");
                                return;
                            }

                            string authFilePath = Path.Combine(tempFolderPath, "auth.txt");

                            string username = "ReunionVPN.Client";
                            string password = "c1QjSh329gTH70XYOAIq1ekM7OkSLRogq2rFv9cT";

                            try
                            {
                                using (StreamWriter writer = new StreamWriter(authFilePath))
                                {
                                    await writer.WriteLineAsync(username);
                                    await writer.WriteLineAsync(password);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Une erreur s'est produite lors de la création du fichier d'authentification : " + ex.Message);
                                return;
                            }

                            string openVpnPath = await FindOpenVPNPath();

                            if (string.IsNullOrEmpty(openVpnPath))
                            {
                                MessageBox.Show("Chemin d'OpenVPN introuvable.");
                                return;
                            }

                            await OpenVPNConnection(openVpnPath, configFilePath, authFilePath);
                        });

                        BtnConnexion.Cursor = Cursors.Hand;
                        BtnConnexion.FillColor = Color.FromArgb(0, 238, 148);
                        BtnConnexion.FillColor2 = Color.FromArgb(0, 165, 246);
                    }

                    else
                    {
                        BtnConnexion.FillColor = Color.Gray;
                        BtnConnexion.FillColor2 = Color.Gray;
                        BtnConnexion.Cursor = Cursors.No;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Une erreur s'est produite : " + ex.Message);
                }

            }
        }

        private async Task OpenVPNConnection(string openVpnPath, string configFilePath, string authFilePath)
        {
            try
            {
                ProcessStartInfo processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = openVpnPath;
                processStartInfo.Arguments = $"--config \"{configFilePath}\" --auth-user-pass \"{authFilePath}\"";
                processStartInfo.UseShellExecute = false;
                processStartInfo.CreateNoWindow = true;
                processStartInfo.RedirectStandardOutput = true;

                Process process = new Process();
                process.StartInfo = processStartInfo;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data) && e.Data.Contains("Peer Connection Initiated"))
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            File.Delete(configFilePath);
                            File.Delete(authFilePath);

                            //Stats Réseaux
                            vpnInterface = NetworkInterface.GetAllNetworkInterfaces()
                            .FirstOrDefault(x => x.Name == "OpenVPN TAP-Windows6");
                            Donnée.Start();

                            //Design
                            BtnDéconnexion.Show();
                            BtnConnexion.Hide();

                            guna2ComboBox1.Enabled = false;
                            guna2ComboBox2.Enabled = false;
                        }));
                    }

                };

                process.Start();
                process.BeginOutputReadLine();

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors de la connexion OpenVPN : " + ex.Message);
            }
        }

        static async Task<string> FindOpenVPNPath()
        {
            try
            {
                string registryPath = @"SOFTWARE\OpenVPN";
                string registryKey = "exe_path";
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        string openVpnPath = key.GetValue(registryKey) as string;
                        if (!string.IsNullOrEmpty(openVpnPath) && File.Exists(openVpnPath))
                        {
                            return openVpnPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors de la recherche du chemin d'OpenVPN : " + ex.Message);
            }

            return string.Empty;
        }

        static async Task<string> CreateOpenVPNConfigFile(string tempFolderPath)
        {
            try
            {

                string configFilePath = Path.Combine(tempFolderPath, "config.ovpn");

                string configContent = @"dev tun
persist-tun
persist-key
cipher AES-128-CBC
data-ciphers AES-256-GCM:AES-128-CBC:CHACHA20-POLY1305
auth SHA1
tls-client
client
resolv-retry infinite
remote bourbonvpn.softether.net 5555
auth-user-pass
remote-cert-tls server
explicit-exit-notify
--mute-replay-warnings

<ca>
-----BEGIN CERTIFICATE-----
MIID7jCCAtagAwIBAgIBADANBgkqhkiG9w0BAQsFADB2MSEwHwYDVQQDDBhib3Vy
Ym9udnBuLnNvZnRldGhlci5uZXQxITAfBgNVBAoMGGJvdXJib252cG4uc29mdGV0
aGVyLm5ldDEhMB8GA1UECwwYYm91cmJvbnZwbi5zb2Z0ZXRoZXIubmV0MQswCQYD
VQQGEwJVUzAeFw0yMzA3MzAxNTI1NDFaFw0zNzEyMzExNTI1NDFaMHYxITAfBgNV
BAMMGGJvdXJib252cG4uc29mdGV0aGVyLm5ldDEhMB8GA1UECgwYYm91cmJvbnZw
bi5zb2Z0ZXRoZXIubmV0MSEwHwYDVQQLDBhib3VyYm9udnBuLnNvZnRldGhlci5u
ZXQxCzAJBgNVBAYTAlVTMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA
5rraj/xcrJNbTjjYV2ttYJKvssPeu/c3d9+5li6sORuA15C7fgX/++sJ/0FZRf0A
Hbz7FeKEQpJ8Rgj4omk9GyE+qVLlcr7sLfQcE7BfMzuvxH2dJkCxiDDMVOlULTw9
EurXu7KnspcDPFEX97J40tcb8b+mnJ+8VwPYBwR8Jc3v4Ee2eP6LkgsLtytXSTrO
90+uSTgaosX4dEvUV8qGkUcrjo+sLVWwov361/LtgV+eISBdOzQsLJrckKMDESre
D/lQBg1+7i3VP8s/ERaxaLreAL7FGoJZETaosWiceRoI0awremG7SqhaFtfIxyjL
6MH8Gfg4Pz236j7L6qD2RwIDAQABo4GGMIGDMA8GA1UdEwEB/wQFMAMBAf8wCwYD
VR0PBAQDAgH2MGMGA1UdJQRcMFoGCCsGAQUFBwMBBggrBgEFBQcDAgYIKwYBBQUH
AwMGCCsGAQUFBwMEBggrBgEFBQcDBQYIKwYBBQUHAwYGCCsGAQUFBwMHBggrBgEF
BQcDCAYIKwYBBQUHAwkwDQYJKoZIhvcNAQELBQADggEBAOarT/i+q7sBx3jp6mjR
7qQkD5xYmWbL9jTV1dRq73lwhBYzOJSGjD/w+BdRc1bPIqxvYKJSrBLWpmg2VB9n
vQ1YUaWfDOSGF+7I83ze0r6ttAYkYlNt/bsFUGvG8d43nCrJqYZ0ab43lufOoopS
7A2nrIL9dvAsKensVAwagHMowQImwzABXSBJZZLPKXJI/mYTrR4A7kSxRfyi0Xrc
q7kOsKyceAzo9p9jqNvV5Q+Qzlz3Y0DE/Io89IIbcc2VF9yte4ccuC2NUZvlmUQs
Pr6E/pWltn5vX6x/SYc0+5ez6kzP1bfRx8Shhsz498JPxgWZaL6bfBlCXTHzc6jl
hAw=
-----END CERTIFICATE-----
</ca>
setenv CLIENT_CERT 0";

                File.WriteAllText(configFilePath, configContent);
                return configFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors de la création du fichier de configuration OpenVPN : " + ex.Message);
                return string.Empty;
            }
        }

        private async Task ConnexionVPN()
        {
            await Task.Run(() =>
            {
                try
                {
                    //VPN
                    string vpnName = "BourbonVPN";
                    string serverAddress = "mysql.bourbonvpn.re";
                    string preSharedKey = "zppEC86dopcEYkrt5PJy";
                    string username = "ReunionVPN.Client";
                    string password = "c1QjSh329gTH70XYOAIq1ekM7OkSLRogq2rFv9cT";


                    string createVpnScript = $@"
$vpnName = ""{vpnName}""
$serverAddress = ""{serverAddress}""
$preSharedKey = ""{preSharedKey}""

Add-VpnConnection -Name $vpnName -ServerAddress $serverAddress -TunnelType L2tp -L2tpPsk $preSharedKey -AuthenticationMethod Pap -Force -AllUserConnection";

                    string connectVpnCommand = $@"c:\windows\system32\rasdial.exe {vpnName} {username} {password}";

                    ProcessStartInfo psi = new ProcessStartInfo("powershell.exe")
                    {
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process() { StartInfo = psi })
                    {
                        process.Start();
                        process.StandardInput.WriteLine(createVpnScript);
                        process.StandardInput.WriteLine(connectVpnCommand);
                        process.StandardInput.WriteLine("exit");
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            this.Invoke((MethodInvoker)(() =>
                            {
                                //Stats Réseaux
                                vpnInterface = NetworkInterface.GetAllNetworkInterfaces()
                                .FirstOrDefault(x => x.Name == "BourbonVPN");
                                Donnée.Start();

                                //Design
                                BtnDéconnexion.Show();
                                BtnConnexion.Hide();

                                guna2ComboBox1.Enabled = false;
                                guna2ComboBox2.Enabled = false;
                            }));
                        }
                        else
                        {
                            this.Invoke((MethodInvoker)(() =>
                            {
                                MessageBox.Show("Échec de la connexion VPN.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Une erreur s'est produite lors de la connexion VPN : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void guna2GradientButton1_Click_1(object sender, EventArgs e)
        {
            if (guna2ComboBox2.SelectedItem.ToString() == "Windows")
            {
                //Pour Windows
                Déconnexionfunction();
            }
            if (guna2ComboBox2.SelectedItem.ToString() == "OpenVPN")
            {
                DéconnexionfunctionOpenVPN();
            }

            BtnConnexion.Show();
            BtnDéconnexion.Hide();
            Donnée.Stop();
            ConnexionTimer.Stop();

            guna2ComboBox1.Enabled = true;
            guna2ComboBox2.Enabled = true;


            //Design

            BtnConnexion.Text = "Connexion";
        }

        private async void DéconnexionfunctionOpenVPN()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "taskkill";
            processStartInfo.Arguments = "/IM openvpn.exe /F";
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.CreateNoWindow = true;
            Process.Start(processStartInfo);
        }

        private async void Déconnexionfunction()
        {
            await Task.Run(() =>
            {
                try
                {
                    string vpnName = "BourbonVPN";

                    string disconnectVpnCommand = $@"c:\windows\system32\rasdial.exe {vpnName} /disconnect";

                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe")
                    {
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process process = new Process() { StartInfo = psi };

                    process.Start();
                    process.StandardInput.WriteLine(disconnectVpnCommand);
                    process.StandardInput.WriteLine("exit");
                    process.WaitForExit();

                    process.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Une erreur s'est produite lors de la déconnexion VPN : " + ex.Message, "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        private void guna2Button3_Click(object sender, EventArgs e)
        {
            login.ShowDialog();

            int x = (this.Left + (this.Width - login.Width) / 2);
            int y = (this.Top + (this.Height - login.Height) / 2);

            x = Math.Max(x, this.Left);
            y = Math.Max(y, this.Top);

            login.Location = new Point(x, y);
        }

        private void guna2CirclePictureBox1_Click(object sender, EventArgs e)
        {
            bool IsLogin = login.UserLogin;

            if (IsLogin)
            {
                if (panelVisible)
                {
                    guna2Panel7.Hide();
                    panelVisible = false;
                }
                else
                {
                    guna2Panel7.Dock = DockStyle.Left;
                    guna2Panel2.BringToFront();
                    guna2Panel7.Show();
                    panelVisible = true;
                }
            }
            else
            {
                login.ShowDialog();

                int x = (this.Left + (this.Width - login.Width) / 2);
                int y = (this.Top + (this.Height - login.Height) / 2);

                x = Math.Max(x, this.Left);
                y = Math.Max(y, this.Top);

                login.Location = new Point(x, y);
            }

        }

        public async Task CreateVersionTextFileAsync(string version)
        {
            try
            {
                string bourbonVpnDirectory = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName;

                if (!Directory.Exists(bourbonVpnDirectory))
                {
                    Directory.CreateDirectory(bourbonVpnDirectory);
                }

                string versionFilePath = Path.Combine(bourbonVpnDirectory, "Update.txt");

                await File.WriteAllTextAsync(versionFilePath, version);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur s'est produite lors de la création du fichier de version : " + ex.Message);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void Form1_Shown(object sender, EventArgs e)
        {
            await CreateVersionTextFileAsync(versionToWrite);

            loading.Show();
            this.Controls.Add(loading);
            loading.BringToFront();
            loading.Dock = DockStyle.Fill;
            guna2Panel1.Dock = DockStyle.None;

            Progressbar_loading.Start();


            //await Actualisation();
            await login.LoginWeb();

            doneloading = true;
        }

        private async void Progressbar_loading_Tick(object sender, EventArgs e)
        {
            if (loading.guna2ProgressBar1.Value < loading.guna2ProgressBar1.Maximum)
            {
                loading.guna2ProgressBar1.Value++;

                if (doneloading)
                {
                    loading.guna2ProgressBar1.Value = 100;
                    logindone();
                    await Task.Delay(1000);
                }
            }
            else
            {
                if (doneloading)
                {
                    logindone();
                    await Task.Delay(1000);
                    Progressbar_loading.Stop();
                    loading.Hide();
                    guna2Panel1.Dock = DockStyle.Left;
                }
            }
        }

        private async void logindone()
        {
            bool IsLogin = login.UserLogin;
            bool Subscribed = login.UserSubscribe;
            string username = login.UsernameLogin.Text;

            if (IsLogin)
            {
                //utilisateur Connectée
                label3.Show();
                label3.Text = username;
                guna2Button3.Hide();

                BtnConnexion.FillColor = Color.FromArgb(0, 238, 148);
                BtnConnexion.FillColor2 = Color.FromArgb(0, 165, 246);
                BtnConnexion.Cursor = Cursors.Hand;

            }
            else
            {
                //utilisateur pas connectée
                label3.Hide();
                BtnConnexion.FillColor = Color.Gray;
                BtnConnexion.FillColor2 = Color.Gray;
                BtnConnexion.Cursor = Cursors.No;
                guna2Button3.Show();
            }
        }

        private void guna2ComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (guna2ComboBox2.SelectedItem.ToString() == "Windows")
            {
                guna2ImageButton2.Hide();
            }
            if (guna2ComboBox2.SelectedItem.ToString() == "OpenVPN")
            {
                guna2ImageButton2.Show();
            }
        }

        private void guna2CirclePictureBox1_MouseHover(object sender, EventArgs e)
        {

        }

        private async void guna2CirclePictureBox1_MouseLeave(object sender, EventArgs e)
        {

        }

        private void guna2Panel7_MouseHover(object sender, EventArgs e)
        {

        }

        private void guna2Panel7_MouseLeave(object sender, EventArgs e)
        {

        }

        private void guna2GradientButton1_Click_2(object sender, EventArgs e)
        {
            login.SettingsLogout();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            logindone();
        }
    }
}