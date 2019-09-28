﻿using System.IO;
using System.Linq;
using nUpdate.Administration.Common.Logging;

// ReSharper disable InconsistentNaming

namespace nUpdate.Administration.Common
{
    internal static class ProjectSession
    {
        static ProjectSession()
        {
            AvailableLocations = JsonSerializer.Deserialize<TrulyObservableCollection<UpdateProjectLocation>>(File.ReadAllText(PathProvider.ProjectsConfigFilePath)) ?? new TrulyObservableCollection<UpdateProjectLocation>();
            AvailableLocations.CollectionChanged += (sender, args) => 
                File.WriteAllText(PathProvider.ProjectsConfigFilePath, JsonSerializer.Serialize(AvailableLocations.ToList()));
        }

        /// <summary>
        ///     Gets the active <see cref="UpdateProject"/> of the <see cref="ProjectSession"/>.
        /// </summary>
        internal static UpdateProject ActiveProject { get; private set; }

        /// <summary>
        ///     Gets the <see cref="IUpdateProvider"/> of the <see cref="ProjectSession"/> for managing update transfers.
        /// </summary>
        internal static IUpdateProvider UpdateProvider { get; private set; }

        /// <summary>
        ///     Gets the <see cref="PackageActionLogger"/> of the <see cref="ProjectSession"/> for the package history.
        /// </summary>
        internal static PackageActionLogger Logger { get; private set; }

        /// <summary>
        ///     Gets the <see cref="KeyManager"/> of the <see cref="ProjectSession"/> for the password management.
        /// </summary>
        internal static KeyManager PasswordManager { get; private set; }

        /// <summary>
        ///     Gets the path of the file containing the <see cref="UpdateProject"/> data of the <see cref="ProjectSession"/>.
        /// </summary>
        internal static string ProjectFilePath
            => AvailableLocations.First(x => x.Guid == ActiveProject.Guid).LastSeenPath;

        /// <summary>
        ///     Gets or sets the path of the local package data folders of the current <see cref="ActiveProject"/>.
        /// </summary>
        internal static string PackagesPath { get; set; }

        internal static TrulyObservableCollection<UpdateProjectLocation> AvailableLocations { get; set; }
        
        /// <summary>
        ///     Initializes the <see cref="ProjectSession"/> with the specified <see cref="UpdateProject"/>.
        /// </summary>
        /// <param name="project">The <see cref="UpdateProject"/> of the <see cref="ProjectSession"/>.</param>
        internal static void InitializeWithProject(UpdateProject project)
        {
            ActiveProject = project;
            Logger = new PackageActionLogger(project);
            UpdateProvider = UpdateProviderResolver.Resolve(project);
            PackagesPath = Path.Combine(PathProvider.Path, "Projects", project.Guid.ToString());

            ActiveProject.PropertyChanged += (sender, args) => ActiveProject.Save();
        }

        internal static void Terminate()
        {
            ActiveProject = default;
            UpdateProvider = null;
            Logger = null;
            UpdateProvider = null;
            PasswordManager = null;
        }
    }
}