﻿using System;
using System.Collections.Generic;
using nUpdate.Administration.Views.NewProject;

namespace nUpdate.Administration.ViewModels.NewProject
{
    public class NewProjectViewManager : ViewManager
    {
        public NewProjectViewManager() : base(
            new Dictionary<Type, Type>
            {
                {typeof(GenerateKeyPairPageViewModel), typeof(GenerateKeyPairPage)},
                {typeof(GeneralDataPageViewModel), typeof(GeneralDataPage)},
                {typeof(TransferProviderSelectionPageViewModel), typeof(UpdateProviderSelectionPage)},
                {typeof(HttpDataPageViewModel), typeof(HttpDataPage)},
                {typeof(FtpDataPageViewModel), typeof(FtpDataPage)}
            })
        { }
    }
}
