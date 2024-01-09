using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace RunPlace
{
    public partial class Login : Form
    {
        private const string BaseUrl = "https://site.bourbonvpn.re/api/auth/login";
        //private const string BaseUrl = "http://192.168.0.141/api/auth/login";
        public const string SecretKey = "MeF7PkSecLse?s?bQRaoSSCApF8b84&GRrYELyJ6Cx8CBP&Mbp#difTFd33EJk&8CQQyPr@57Eyfp@PE8f7Q5DPq9QccRenz@Mme#zcjQD$$5!Q4Ko9JQyM&9xLfbL&xY3ES#imr?ig?pYGcD6x8cNg#Pxx4GD35qfP&8MG&ihpEoFXBisE4AnL6Kb@$R!iY5x5i9Tm#PnFkSGSTi#h3A$eAc##!Qjnrg!zeNn#&iMAMCqS9KnEEkjj!e$cmmp8Db@G68Pkdsko7R5H9RzDThDB9?4amg9GbdYtMYRPphzzcQJf!T#?LFtnHtFaExEkk7eeY#bRnFb&K4jYK7c?$rEE5CC$?XcT4j3LjtjE!5kKRtAhs?iN7GQ9Gj8#tb?&7F&DLCSaxAcKGa#3ycXo@oEN8gY3kNDhqEe!$SrG8MMGQRC@#85L@F&B3P7?Mc?b7LDN5@K4kXCYH9oY#yG7oTCo7ezHno3oXG7sHFmeA#@msqbFsh3ppsKiH8crMH6sRkxhD$8yK#RPLEAfLzXfqfPrjojA?gX3meRX5@9epGD5!P$a5QM9!P@p3bjnFxax!BMdombH55GqPpkMHyT7pFQ8CkonPdkttjYN86Bdefm5c@M!JDTXN5Ct5of7p!k47sJ4RDA3RLhoe#EMnD&L3CbCiNpMp?8JP#Q@R&G55Mjhke6syrfLFi3EyPF3!!7c$TfcDYQoc$k5sCif$#P84dGEfdAoFQoCsnKP?kayEBXf65piSC4RGmb3$zFTm4hdHrDG6FQaH6dD#AMtRG!h9BfAddnncAEGirxge3g398Q95k#GP!fkbER78fpBc5!eyprS!g8t&LDnhBMfCjeT$TnpoR5f36amE6Adq9P7$mbSLy6aPRs98I!RYfI8mFyCa8z6UPnm8li&aQYntVgic$8kn0!G!H1OoP&k4Dq6p&ryZ5qO8b3pI@3V67PyNn$o7Eh$pT6QRikCDR79C@y4?S#Uv19$8E1?VfVYsAA9";
        private string encryptionKey = "R2E2yPXrS3war5jdeKYCjzZvZSNVd3Hs";
        private byte[] staticIV = Encoding.UTF8.GetBytes("p52tEqM6rXKeVyDc");
        public bool UserLogin { get; private set; }
        public bool UserSubscribe = true;


        public Login()
        {
            InitializeComponent();
            LoadSettings();
        }

        private async void guna2Button1_Click(object sender, EventArgs e)
        {
            if (UsernameLogin.Text.Length > 0 && PasswordLogin.Text.Length > 0)
            {

                guna2Button1.Text = "Connexion en Cours";
                await LoginWeb();
            }
        }

        public async Task LoginWeb()
        {

            string username = UsernameLogin.Text;
            string password = PasswordLogin.Text;

            var loginModel = new { Username = username, Password = password };

            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("SecretKey", SecretKey);

                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(loginModel);

                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var response = await httpClient.PostAsync(BaseUrl, new StringContent(json, Encoding.UTF8, "application/json"));

                    if (response.IsSuccessStatusCode)
                    {
                        if (guna2ToggleSwitch1.Checked)
                        {
                            SaveSettings();
                        }

                        UserLogin = true;

                        guna2Button1.Text = "Connexion réussie !";
                        await Task.Delay(1500);
                        this.Hide();

                        string jsonResponse = await response.Content.ReadAsStringAsync();
                    }
                    else if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();

                        if (errorMessage.Contains("subscription is not active"))
                        {
                            UserSubscribe = false;
                            UserLogin = true;
                            guna2Button1.Text = "Connexion réussie !";
                            await Task.Delay(3000);
                            this.Hide();
                        }
                        else if (errorMessage.Contains("User is already connected"))
                        {
                            guna2Button1.Text = "Login";
                            label1.Text = "Un utilisateur avec ce nom d'utilisateur est déjà connecté.";
                        }
                        else if (errorMessage.Contains("Account banned from our services"))
                        {
                            guna2Button1.Text = "Compte Banni";
                            guna2Button1.FillColor = Color.Red;

                            string message = "Compte banni de nos services. Pour plus d'informations, veuillez nous contacter sur notre discord ou contacter un modérateur/gérant.";
                            DialogResult result = MessageBox.Show(message, "Compte Banni", MessageBoxButtons.OK);

                            if (result == DialogResult.OK)
                            {
                                Application.Exit();
                            }
                        }
                        else
                        {
                            label1.Text = "Nom d'utilisateur ou mot de passe incorrect";
                            await Task.Delay(1500);
                            label1.Text = "";
                        }
                    }
                    else
                    {
                        label1.Text = "Nom d'utilisateur ou mot de passe incorrect";
                        await Task.Delay(1500);
                        label1.Text = "";
                    }
                }
            }
            catch (Exception ex)
            {
                guna2Button1.Text = "Login";
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        public void SettingsLogout()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configFolderPath = Path.Combine(appDataPath, "BourbonVPN");
                string configFilePath = Path.Combine(configFolderPath, "config.ini");

                if (!Directory.Exists(configFolderPath))
                {
                    Directory.CreateDirectory(configFolderPath);
                }

                Configuration config;

                if (File.Exists(configFilePath))
                {
                    ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                    configFileMap.ExeConfigFilename = configFilePath;
                    config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                }
                else
                {
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.SaveAs(configFilePath);
                }

                if (config.AppSettings.Settings["Password"] != null)
                {
                    config.AppSettings.Settings["Password"].Value = "";
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

                Task.Delay(500);

                Application.Restart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message);
            }
        }

        public void Logout()
        {
            SettingsLogout();
            Application.Restart();
        }

        private void SaveSettings()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configFolderPath = Path.Combine(appDataPath, "BourbonVPN");
                string configFilePath = Path.Combine(configFolderPath, "config.ini");

                if (!Directory.Exists(configFolderPath))
                {
                    Directory.CreateDirectory(configFolderPath);
                }

                Configuration config;

                if (File.Exists(configFilePath))
                {
                    ExeConfigurationFileMap configFileMap = new ExeConfigurationFileMap();
                    configFileMap.ExeConfigFilename = configFilePath;
                    config = ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);
                }
                else
                {
                    config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    config.SaveAs(configFilePath);
                }

                if (config.AppSettings.Settings["Username"] != null)
                {
                    config.AppSettings.Settings["Username"].Value = UsernameLogin.Text;
                }

                if (config.AppSettings.Settings["Password"] != null)
                {
                    // Chiffrement du mot de passe
                    string encryptedPassword = EncryptPassword(PasswordLogin.Text);
                    config.AppSettings.Settings["Password"].Value = encryptedPassword;
                }

                if (config.AppSettings.Settings["RememberMe"] != null)
                {
                    config.AppSettings.Settings["RememberMe"].Value = guna2ToggleSwitch1.Checked.ToString();
                }

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message);
            }
        }

        public void LoadSettings()
        {
            try
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configFolderPath = Path.Combine(appDataPath, "BourbonVPN");
                string configFilePath = Path.Combine(configFolderPath, "config.ini");

                if (File.Exists(configFilePath))
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(configFilePath);

                    XmlNode appSettingsNode = xmlDoc.SelectSingleNode("//appSettings");
                    if (appSettingsNode != null)
                    {
                        foreach (XmlNode addNode in appSettingsNode.SelectNodes("add"))
                        {
                            string key = addNode.Attributes["key"].Value;
                            string value = addNode.Attributes["value"].Value;

                            if (key == "Username")
                            {
                                UsernameLogin.Text = value;
                            }
                            if (key == "Password")
                            {
                                // Déchiffrement du mot de passe
                                string decryptedPassword = DecryptPassword(value);
                                PasswordLogin.Text = decryptedPassword;
                            }
                            if (key == "RememberMe")
                            {
                                bool rememberMe;
                                if (bool.TryParse(value, out rememberMe))
                                {
                                    guna2ToggleSwitch1.Checked = rememberMe;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement : " + ex.Message);
            }
        }

        public string EncryptPassword(string password)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
                aesAlg.IV = staticIV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(password);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string DecryptPassword(string encryptedPassword)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Encoding.UTF8.GetBytes(encryptionKey);
                aesAlg.IV = staticIV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedPassword)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
