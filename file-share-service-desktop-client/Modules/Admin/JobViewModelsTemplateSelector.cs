﻿using System;
using System.Windows;
using System.Windows.Controls;
using UKHO.FileShareService.DesktopClient.Modules.Admin.JobViewModels;

namespace UKHO.FileShareService.DesktopClient.Modules.Admin
{
    public class JobViewModelsTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                NewBatchJobViewModel => NewBatchTemplate ??
                                        throw new ArgumentNullException(
                                            $"{nameof(NewBatchTemplate)} property has not been set",
                                            nameof(NewBatchTemplate)),
                SetExpiryDateJobViewModel => SetExpiryDateTemplate ??
                                             throw new ArgumentNullException(
                                                 $"{nameof(SetExpiryDateTemplate)} property has not been set",
                                                 nameof(SetExpiryDateTemplate)),
                AppendAclJobViewModel => AppendAclTemplate ??
                                         throw new ArgumentNullException(
                                             $"{nameof(AppendAclTemplate)} property has not been set",
                                             nameof(AppendAclTemplate)),
                _ => throw new ArgumentException("Unsupported item:" + item, nameof(item))
            };
        }

        public DataTemplate? AppendAclTemplate { get; set; }

        public DataTemplate? SetExpiryDateTemplate { get; set; }

        public DataTemplate? NewBatchTemplate { get; set; }
    }
}