using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace PaintPower.Editors;

public partial class Header : UserControl
{
    public Header()
    {
        InitializeComponent();
    }

    // ------------------------------------------------------------
    // Version + Status
    // ------------------------------------------------------------
    public void SetVersion(string version)
    {
        VersionInfoTextBlock.Text = version;
    }

    public void SetStatus(string status)
    {
        ProjectStatus.Text = status;
    }

    // ------------------------------------------------------------
    // Load menu definition from Editor
    // ------------------------------------------------------------
    public void LoadDefinition(HeaderDefinition def)
    {
        MenuHost.Items.Clear();

        foreach (var menu in def.Menus)
        {
            var menuItem = new MenuItem { Header = menu.Key };

            foreach (var item in menu.Value)
            {
                if (item.IsSeparator)
                {
                    menuItem.Items.Add(new Separator());
                    continue;
                }

                var sub = new MenuItem { Header = item.Label };

                if (item.Command != null)
                    sub.Click += (_, _) => item.Command();

                menuItem.Items.Add(sub);
            }

            MenuHost.Items.Add(menuItem);
        }
    }
}
