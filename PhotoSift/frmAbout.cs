/*
 *  PhotoSift
 *  Copyright (C) 2013-2014  RL Vision
 *  Copyright (C) 2020-2022  YFdyh000
 *  
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * */

using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using static PhotoSift.NGettextShortSyntax;

namespace PhotoSift
{
    partial class frmAbout : Form
	{
		public frmAbout( AppSettings settings )
		{
			InitializeComponent();
			this.Text = string.Format( _("About {0}"), Util.GetAppName() );
			this.labelProductName.Text = Util.GetAppName() + " " + Assembly.GetExecutingAssembly().GetName().Version.Major + "." + Assembly.GetExecutingAssembly().GetName().Version.Minor;
			this.labelCopyright.Text = AssemblyCopyright;
			this.labelLicense.Text = _("Free, open source software (GPLv3)");
			this.labelStats.Text = _("Loaded: ") + settings.Stats_LoadedPics + _("\nCopied/Moved: ") + ( settings.Stats_CopiedPics + settings.Stats_MovedPics ) + _("\nRenamed: ") + settings.Stats_RenamedPics + _("\nDeleted: ") + settings.Stats_DeletedPics;
			Debug.WriteLine(_(_("Loaded: ")));
			btnClose.Text = _("Ok");
			linkLabel1.Text = _("The project on GitHub");
			label1.Text = _("Statistics:");
			label3.Text = _("Donate:");
		}

		#region Assembly Attribute Accessors

		public string AssemblyTitle
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyTitleAttribute ), false );
				if( attributes.Length > 0 )
				{
					AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
					if( titleAttribute.Title != "" )
					{
						return titleAttribute.Title;
					}
				}
				return System.IO.Path.GetFileNameWithoutExtension( Assembly.GetExecutingAssembly().CodeBase );
			}
		}

		public string AssemblyVersion
		{
			get
			{
				return Assembly.GetExecutingAssembly().GetName().Version.ToString();
			}
		}

		public string AssemblyDescription
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyDescriptionAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyDescriptionAttribute)attributes[0] ).Description;
			}
		}

		public string AssemblyProduct
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyProductAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyProductAttribute)attributes[0] ).Product;
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyCopyrightAttribute)attributes[0] ).Copyright;
			}
		}

		public string AssemblyCompany
		{
			get
			{
				object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCompanyAttribute ), false );
				if( attributes.Length == 0 )
				{
					return "";
				}
				return ( (AssemblyCompanyAttribute)attributes[0] ).Company;
			}
		}
		#endregion

		private void btnClose_Click( object sender, EventArgs e )
		{
			this.Close();
		}

		private static void openUrl(string url) {
            Process.Start(url);
        }
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => openUrl("https://github.com/yfdyh000/PhotoSift");
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => openUrl("https://liberapay.com/yfdyh000");
        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => openUrl("https://afdian.net/@yfdyh000");
    }
}
