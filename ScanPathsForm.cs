using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace winC2D
{
    public class ScanPathsForm : Form
    {
        private readonly List<ScanPathItem> paths;
        private readonly ListView listView;
        private readonly Button buttonAdd;
        private readonly Button buttonRemove;
        private readonly Button buttonOk;
        private readonly Button buttonCancel;
        private readonly Label labelHint;

        public List<ScanPathItem> Paths => paths;

        public ScanPathsForm(List<ScanPathItem> items)
        {
            this.paths = items ?? new List<ScanPathItem>();

            Text = Localization.T("Dialog.ScanPaths.Title");
            Width = 720;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;

            listView = new ListView
            {
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(680, 340)
            };
            listView.Columns.Add(Localization.T("Column.Path"), 480);
            listView.Columns.Add(Localization.T("Column.Removable"), 160);

            labelHint = new Label
            {
                Text = Localization.T("Dialog.ScanPaths.Hint"),
                AutoSize = false,
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(680, 24)
            };

            buttonAdd = new Button
            {
                Text = Localization.T("Button.AddPath"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Location = new System.Drawing.Point(10, 390),
                Size = new System.Drawing.Size(120, 30)
            };
            buttonAdd.Click += ButtonAdd_Click;

            buttonRemove = new Button
            {
                Text = Localization.T("Button.RemovePath"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Location = new System.Drawing.Point(140, 390),
                Size = new System.Drawing.Size(120, 30)
            };
            buttonRemove.Click += ButtonRemove_Click;

            buttonOk = new Button
            {
                Text = Localization.T("Button.Save"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new System.Drawing.Point(500, 390),
                Size = new System.Drawing.Size(90, 30)
            };
            buttonOk.Click += (s, e) => { ApplyListViewChanges(); DialogResult = DialogResult.OK; Close(); };

            buttonCancel = new Button
            {
                Text = Localization.T("Button.Cancel"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new System.Drawing.Point(600, 390),
                Size = new System.Drawing.Size(90, 30)
            };
            buttonCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(listView);
            Controls.Add(labelHint);
            Controls.Add(buttonAdd);
            Controls.Add(buttonRemove);
            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);

            Load += (s, e) => RefreshList();
        }

        private void RefreshList()
        {
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                foreach (var p in paths)
                {
                    var item = new ListViewItem(new[]
                    {
                        p.Path,
                        p.IsDefault ? Localization.T("Msg.No") : Localization.T("Msg.Yes")
                    })
                    {
                        Checked = p.Enabled,
                        Tag = p
                    };
                    listView.Items.Add(item);
                }
            }
            finally
            {
                listView.EndUpdate();
            }
        }

        private void ApplyListViewChanges()
        {
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Tag is ScanPathItem p)
                {
                    p.Enabled = item.Checked;
                }
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = Localization.T("Msg.SelectFolder");
            fbd.ShowNewFolderButton = true;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                var path = fbd.SelectedPath.Trim();
                if (paths.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
                    return;
                paths.Add(new ScanPathItem { Path = path, Enabled = true, IsDefault = false });
                RefreshList();
            }
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            var selected = listView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selected.Count == 0) return;
            foreach (var item in selected)
            {
                if (item.Tag is ScanPathItem p)
                {
                    if (p.IsDefault)
                    {
                        MessageBox.Show(Localization.T("Msg.CannotDeleteDefault"), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }
                    paths.Remove(p);
                }
            }
            RefreshList();
        }
    }

    public class ScanPathItem
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool IsDefault { get; set; }

        public ScanPathItem Clone() => new ScanPathItem
        {
            Path = this.Path,
            Enabled = this.Enabled,
            IsDefault = this.IsDefault
        };
    }
}
