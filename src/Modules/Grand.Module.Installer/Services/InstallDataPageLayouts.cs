﻿using Grand.Domain.Pages;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual Task InstallPageLayouts()
    {
        var pageLayouts = new List<PageLayout> {
            new() {
                Name = "Default layout",
                ViewPath = "PageDetails",
                DisplayOrder = 1
            }
        };
        pageLayouts.ForEach(x => _pageLayoutRepository.Insert(x));
        return Task.CompletedTask;
    }
}