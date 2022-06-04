﻿/*
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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.Globalization;
using static PhotoSift.NGettextShortSyntax;

namespace PhotoSift
{
    public partial class frmSettings : Form
	{
		AppSettings settings;

		public delegate void ApplyColorSettings();
		public ApplyColorSettings applyColorSettings;

		public frmSettings( AppSettings settings )
		{
			this.settings = settings;

			InitializeComponent();
			propertyGrid.SelectedObject = settings;

			this.Text = _("Settings");
			propertyGrid.Focus();
		}

		private void frmSettings_Load( object sender, EventArgs e )
		{
			if( !settings.FormRect_Settings.IsEmpty )
			{
				this.Left = settings.FormRect_Settings.X;
				this.Top = settings.FormRect_Settings.Y;
				this.Width = settings.FormRect_Settings.Width;
				this.Height = settings.FormRect_Settings.Height;
			}
			resetToolStripMenuItem.Text = _("Reset");
		}

		private void frmSettings_FormClosing( object sender, FormClosingEventArgs e )
		{
			if( this.WindowState != FormWindowState.Maximized )
			{
				settings.FormRect_Settings = new Rectangle( this.Left, this.Top, this.Width, this.Height );
			}
		}

		private void frmSettings_KeyDown( object sender, KeyEventArgs e )
		{
			if( e.KeyCode == Keys.F12 || e.KeyCode == Keys.Escape ) this.Close();
		}

		private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
		{
			applyColorSettings();

			if (e.ChangedItem.PropertyDescriptor.Name == "TargetFolderPath") // workaround, the displayed item is storage.
				this.settings.TargetFolder = (string)e.ChangedItem.Value;
		}

		private void resetToolStripMenuItem_Click(object sender, EventArgs e)
		{
			// System.ComponentModel.DefaultValueAttribute not automatically filled and only static, so give up.
			//propertyGrid.ResetSelectedProperty();

			void _PropItemResetToDefault(GridItem gridItem)
			{
				string keyName = gridItem.PropertyDescriptor.Name;
				if (settings.defaultSettings.TryGetValue(keyName, out object keyValue))
				{
					object convertedValue = Convert.ChangeType(keyValue, gridItem.PropertyDescriptor.PropertyType);
					PropertyInfo prop = (typeof(AppSettings)).GetProperty(keyName);
					prop.SetValue(this.settings, convertedValue);
				}
				else
				{
					Console.WriteLine("Error getting default value: " + keyName);
				}
			}

            GridItemType SelectedGridItem = propertyGrid.SelectedGridItem.GridItemType;
			if (SelectedGridItem == GridItemType.Property)
			{
				_PropItemResetToDefault(propertyGrid.SelectedGridItem);
			}
			else if (SelectedGridItem == GridItemType.Category)
			{
				DialogResult confirm = MessageBox.Show(
					// TRANSLATORS: {0} is propertyGrid.SelectedGridItem.GridItems.Count
					string.Format(_("Are you sure you want to reset {0} preferences of this group to defaults?"), propertyGrid.SelectedGridItem.GridItems.Count),
					_("Reset to defaults"), MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (confirm == DialogResult.No) return;

				foreach (GridItem item in propertyGrid.SelectedGridItem.GridItems)
				{
					_PropItemResetToDefault(item);
					item.PropertyDescriptor.ResetValue(this.settings); // Necessary for Refresh();
				}
			}
			propertyGrid.Refresh();
			//this.Focus(); // Refresh, not work for groups reset
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			if (propertyGrid.SelectedGridItem != null)
				contextMenuStrip1.Items[0].Enabled = true;
			else
				contextMenuStrip1.Items[0].Enabled = false;
		}
	}

	public class EnumTypeConverter : EnumConverter
	{
		private Type m_EnumType;
		public EnumTypeConverter( Type type )
			: base( type )
		{
			m_EnumType = type;
		}

		public override bool CanConvertTo( ITypeDescriptorContext context, Type destType )
		{
			return destType == typeof( string );
		}

		public override object ConvertTo( ITypeDescriptorContext context, CultureInfo culture, object value, Type destType )
		{
			FieldInfo fi = m_EnumType.GetField( Enum.GetName( m_EnumType, value ) );
			DescriptionAttribute dna =
				(DescriptionAttribute)Attribute.GetCustomAttribute(
				fi, typeof( DescriptionAttribute ) );

			if( dna != null )
				return dna.Description;
			else
				return value.ToString();
		}

		public override bool CanConvertFrom( ITypeDescriptorContext context, Type srcType )
		{
			return srcType == typeof( string );
		}

		public override object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, object value )
		{
			foreach( FieldInfo fi in m_EnumType.GetFields() )
			{
				DescriptionAttribute dna =
				(DescriptionAttribute)Attribute.GetCustomAttribute(
				fi, typeof( DescriptionAttribute ) );

				if( ( dna != null ) && ( (string)value == dna.Description ) )
					return Enum.Parse( m_EnumType, fi.Name );
			}
			return Enum.Parse( m_EnumType, (string)value );
		}
	}
}
