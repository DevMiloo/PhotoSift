using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace PhotoSift
{
    public partial class frmKeyFolders : Form
    {
		public frmKeyFolders(Dictionary<string, string> keyFolderPaths)
		{
			InitializeComponent();
			AddComponents(keyFolderPaths);
		}

		private void AddComponents(Dictionary<string, string> keyFolderPaths)
        {
            var sizes = new Dictionary<string, Size>
            {
                { "Min", new Size(40, 16) }
            };
            var curFont = new TextBox().Font;
			foreach (string key in keyFolderPaths.Keys)
            {
				sizes.Add(key, TextRenderer.MeasureText(keyFolderPaths[key], curFont));
			}
			int maxH = sizes.OrderByDescending(s => s.Value.Height).First().Value.Height;
			int maxW = sizes.OrderByDescending(s => s.Value.Width).First().Value.Width + 20;

			foreach (string key in keyFolderPaths.Keys)
			{
				int y_loc = (this.Controls.Count / 2 ) * 22;
				Label lbl = new Label();
				lbl.Name = "lbl" + key;
				lbl.Text = key;
				lbl.AutoSize = false;
				lbl.Size = new Size(17, 19);
				lbl.Location = new Point(12, y_loc + 9);
				this.Controls.Add(lbl);

				TextBox txt = new TextBox();
				txt.Name = "txt" + key;
				txt.Text = Path.GetFileName(keyFolderPaths[key]);
				txt.ReadOnly = true;
				txt.Size = new Size(maxW, maxH);
				txt.Location = new Point(37, y_loc + 6);
				this.Controls.Add(txt);
			}
			Label lblm = new Label
            {
                Name = "lblMargins",
                Text = "",
                AutoSize = false,
                Size = new Size(17, 4),
                Location = new Point(12, (this.Controls.Count / 2) * 22 + 9)
            };
            this.Controls.Add(lblm);

			this.FormBorderStyle = FormBorderStyle.FixedSingle;
		}


		public static Dictionary<string, string> DictionaryFromType(object atype)
		{
			if (atype == null) return new Dictionary<string, string>();
			Type t = atype.GetType();
			PropertyInfo[] props = t.GetProperties();
			Dictionary<string, string> dict = new Dictionary<string, string>();
			foreach (PropertyInfo prp in props)
			{
				object[] attrs = prp.GetCustomAttributes(true);
				foreach (object attr in attrs)
				{
					if (attr is CategoryAttribute)
						if ((attr as CategoryAttribute).Category == "Key Folders")
						{
							string value = prp.GetValue(atype, new object[] { }).ToString();
							if (value != string.Empty)
								dict.Add(prp.Name.Replace("KeyFolder_", ""), value);
						}
				}
			}
			return dict;
		}

    }
}
