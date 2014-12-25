﻿// Author: Dominic Beger (Trade/ProgTrade)
// License: Creative Commons Attribution NoDerivs (CC-ND)
// Created: 01-08-2014 12:11

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Windows.Forms;
using MySql.Data.MySqlClient.Properties;
using nUpdate.Administration.Core;
using nUpdate.Administration.Core.Application;
using nUpdate.Administration.Core.Application.Extension;
using nUpdate.Administration.Core.Localization;
using nUpdate.Administration.UI.Popups;
using Application = System.Windows.Forms.Application;

namespace nUpdate.Administration.UI.Dialogs
{
    public partial class MainDialog : BaseDialog
    {
        private SecureString _ftpPassword = new SecureString();
        private SecureString _sqlPassword = new SecureString();
        private SecureString _proxyPassword = new SecureString();
        private readonly LocalizationProperties _lp = new LocalizationProperties();

        /// <summary>
        ///     The path of the project that is stored in a file that was opened.
        /// </summary>
        public string ProjectPath { get; set; }

        public MainDialog()
        {
            InitializeComponent();
        }

        ///// <summary>
        /////     Sets the language
        ///// </summary>
        //public void SetLanguage()
        //{
        //    string languageFilePath = Path.Combine(Program.LanguagesDirectory,
        //        String.Format("{0}.json", Settings.Default.Language.Name));
        //    if (File.Exists(languageFilePath))
        //        _lp = Serializer.Deserialize<LocalizationProperties>(File.ReadAllText(languageFilePath));
        //    else
        //    {
        //        File.WriteAllBytes(Path.Combine(Program.LanguagesDirectory, "en.json"), Resources.en);
        //        Settings.Default.Language = new CultureInfo("en");
        //        Settings.Default.Save();
        //        Settings.Default.Reload();
        //        _lp = Serializer.Deserialize<LocalizationProperties>(File.ReadAllText(languageFilePath));
        //    }

        //    Text = _lp.ProductTitle;
        //    headerLabel.Text = _lp.ProductTitle;
        //    infoLabel.Text = _lp.MainDialogInfoText;

        //    sectionsListView.Groups[0].Header = _lp.MainDialogProjectsGroupText;
        //    sectionsListView.Groups[1].Header = _lp.MainDialogInformationGroupText;
        //    sectionsListView.Groups[2].Header = _lp.MainDialogPreferencesGroupText;

        //    sectionsListView.Items[0].Text = _lp.MainDialogNewProjectText;
        //    sectionsListView.Items[1].Text = _lp.MainDialogOpenProjectText;
        //    sectionsListView.Items[4].Text = _lp.MainDialogFeedbackText;
        //    sectionsListView.Items[5].Text = _lp.MainDialogPreferencesText;
        //    sectionsListView.Items[6].Text = _lp.MainDialogInformationText;
        //}

        private void MainDialog_Load(object sender, EventArgs e)
        {
            if (Environment.OSVersion.Version.Major < 6)
            {
                DialogResult dr = MessageBox.Show(_lp.OperatingSystemNotSupportedWarn, String.Empty, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                if (dr == DialogResult.OK)
                    Application.Exit();
            }
            
            var fai = new FileAssociationInfo(".nupdproj");
            if (!fai.Exists)
            {
                try
                {
                    fai.Create("nUpdate Administration");

                    var pai = new ProgramAssociationInfo(fai.ProgId);
                    if (!pai.Exists)
                    {
                        pai.Create("nUpdate Administration Project File",
                            new ProgramVerb("Open", Application.ExecutablePath));
                        pai.DefaultIcon = new ProgramIcon(Application.ExecutablePath);
                    }
                }
                catch
                {
                    Popup.ShowPopup(this, SystemIcons.Error, _lp.MissingRightsWarnCaption, _lp.MissingRightsWarnText,
                        PopupButtons.Ok);
                }

                if (!String.IsNullOrEmpty(ProjectPath))
                    OpenProject(ProjectPath);
            }

            Program.LanguagesDirectory = Path.Combine(Program.Path, "Localization");
            if (!Directory.Exists(Program.LanguagesDirectory))
            {
                Directory.CreateDirectory(Program.LanguagesDirectory); // Create the directory

                // Save the language content
                var lang = new LocalizationProperties();
                string content = Serializer.Serialize(lang);
                File.WriteAllText(Path.Combine(Program.LanguagesDirectory, "en.json"), content);
            }

            if (!File.Exists(Program.ProjectsConfigFilePath))
            {
                using (File.Create(Program.ProjectsConfigFilePath))
                {
                }
            }

            if (!File.Exists(Program.StatisticServersFilePath))
            {
                using (File.Create(Program.StatisticServersFilePath))
                {
                }
            }

            string projectsPath = Path.Combine(Program.Path, "Projects");
            if (!Directory.Exists(projectsPath))
                Directory.CreateDirectory(projectsPath);

            try
            {
                foreach (
                    var entryParts in
                        File.ReadAllLines(Program.ProjectsConfigFilePath).Select(entry => entry.Split('%')))
                {
                    Program.ExisitingProjects.Add(entryParts[0], entryParts[1]);
                }
            }
            catch (Exception ex)
            {
                Popup.ShowPopup(this, SystemIcons.Error, "Error wile reading the project data.", ex,
                    PopupButtons.Ok);
            }

            //SetLanguage();
            sectionsListView.DoubleBuffer();
        }

        public UpdateProject OpenProject(string projectPath)
        {
            UpdateProject project;
            try
            {
                project = ApplicationInstance.LoadProject(projectPath);
            }
            catch (Exception ex)
            {
                Popup.ShowPopup(this, SystemIcons.Error, _lp.ProjectReadingErrorCaption, ex,
                    PopupButtons.Ok);
                return null;
            }

            var credentialsDialog = new CredentialsDialog();
            if (credentialsDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (_ftpPassword != null)
                        _ftpPassword = new SecureString();

                    if (_sqlPassword != null)
                        _sqlPassword = new SecureString();

                    if (_proxyPassword != null)
                        _proxyPassword = new SecureString();

                    _ftpPassword =
                        AesManager.Decrypt(Convert.FromBase64String(project.FtpPassword),
                            credentialsDialog.Password.Trim(), credentialsDialog.Username.Trim());

                    if (project.Proxy != null)
                        _proxyPassword =
                            AesManager.Decrypt(Convert.FromBase64String(project.ProxyPassword),
                                credentialsDialog.Password.Trim(), credentialsDialog.Username.Trim());

                    if (project.UseStatistics)
                        _sqlPassword =
                            AesManager.Decrypt(Convert.FromBase64String(project.SqlPassword),
                                credentialsDialog.Password.Trim(), credentialsDialog.Username.Trim());
                }
                catch (CryptographicException)
                {
                    Popup.ShowPopup(this, SystemIcons.Error, "Invalid credentials.",
                        "The entered credentials are invalid.", PopupButtons.Ok);
                    return null;
                }
                catch (Exception ex)
                {
                    Popup.ShowPopup(this, SystemIcons.Error, "The decryption progress has failed.",
                        ex, PopupButtons.Ok);
                    return null;
                }
            }
            else
            {
                return null;
            }

            if (project.FtpUsername == credentialsDialog.Username) 
                return project;

            Popup.ShowPopup(this, SystemIcons.Error, "Invalid credentials.",
                "The entered credentials are invalid.", PopupButtons.Ok);
            return null;
        }

        private void sectionsListView_Click(object sender, EventArgs e)
        {
            switch (sectionsListView.FocusedItem.Index)
            {
                case 0:
                    var newProjectDialog = new NewProjectDialog();
                    newProjectDialog.ShowDialog();
                    break;

                case 1:
                    using (var fileDialog = new OpenFileDialog())
                    {
                        fileDialog.Filter = "nUpdate Project Files (*.nupdproj)|*.nupdproj";
                        fileDialog.Multiselect = false;
                        if (fileDialog.ShowDialog() == DialogResult.OK)
                        {
                            Project = OpenProject(fileDialog.FileName);
                            if (Project == null)
                                return;

                            var projectDialog = new ProjectDialog { Project = Project, FtpPassword = _ftpPassword.Copy(), ProxyPassword = _proxyPassword.Copy(), SqlPassword = _sqlPassword.Copy() };
                            if (projectDialog.ShowDialog() == DialogResult.OK)
                            {
                                _ftpPassword.Dispose();
                                _proxyPassword.Dispose();
                                _sqlPassword.Dispose();
                            }
                        }
                    }
                    break;

                case 2:
                    var projectRemovalDialog = new ProjectRemovalDialog();
                    projectRemovalDialog.ShowDialog();
                    break;

                case 3:
                    using (var fileDialog = new OpenFileDialog())
                    {
                        fileDialog.Filter = "nUpdate Project Files (*.nupdproj)|*.nupdproj";
                        fileDialog.Multiselect = false;
                        if (fileDialog.ShowDialog() == DialogResult.OK)
                        {
                            Project = OpenProject(fileDialog.FileName);
                            if (Project == null)
                                return;

                            var projectEditDialog = new ProjectEditDialog { Project = Project, FtpPassword = _ftpPassword, ProxyPassword = _proxyPassword, SqlPassword = _sqlPassword};
                            if (projectEditDialog.ShowDialog() == DialogResult.OK)
                            {
                                _ftpPassword.Dispose();
                                _proxyPassword.Dispose();
                                _sqlPassword.Dispose();
                            }
                        }
                    }
                    break;

                case 4:
                    var feedbackDialog = new FeedbackDialog();
                    feedbackDialog.ShowDialog();
                    break;

                case 5:
                    var preferencesDialog = new PreferencesDialog();
                    preferencesDialog.ShowDialog();
                    break;

                case 6:
                    var infoDialog = new InfoDialog();
                    infoDialog.ShowDialog();
                    break;
                case 7:
                    var statisticsServerDialog = new StatisticsServerDialog {ReactsOnKeyDown = false};
                    statisticsServerDialog.ShowDialog();
                    break;
                case 8:
                    var helpDialog = new HelpDialog();
                    helpDialog.ShowDialog();
                    break;
                case 9:
                    Popup.ShowPopup(this, SystemIcons.Information, "Feature not implemented yet.",
                        "This is the first version of nUpdate Administration. The conversion will be available in coming versions as soon as changes to the configuration schemata are made.",
                        PopupButtons.Ok);
                        break;
            }
        }
    }
}