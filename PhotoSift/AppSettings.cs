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
using System.ComponentModel;
using System.Drawing.Design;
using System.Drawing;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO;
using static PhotoSift.WinApi;
using GlobalizedPropertyGrid;

namespace PhotoSift
{
    /// <summary>
    /// Contains all PhotoSift settings. Most attributes control appearance in the PropertyGrid in frmSettings.
    /// </summary>
    [Serializable]
	[DefaultProperty("FileMode")]
	public class AppSettings: GlobalizedObject
	{
		// -- Settings shown in the property grid --

		// File Operations Group
		// (space in front of category name is intended; makes it sort first)
		[Category("File Operations" ), LocalizedDisplayName("File mode"), LocalizedDescription("When pressing an action key, should the file be copied or moved?")]
		[TypeConverter( typeof( EnumTypeConverter ) )]
		public FileOperations FileMode { get; set; }

		[Category("File Operations" ), LocalizedDisplayName( "Existing files" ), LocalizedDescription( "When pressing an action key, and the target folder already contains a file with the same name, what action do you want to take? Append number means a (1) will be added to the end of the filename." )]
		[TypeConverter( typeof( EnumTypeConverter ) )]
        public ExistingFileOptions ExistingFiles { get; set; }
        
		[Category("File Operations" ), LocalizedDisplayName( "Delete mode" ), LocalizedDescription( "Determines the action to take when pressing the Delete key. You can force different modes with Shift+Del (Delete), Alt+Del (Recycle) and Ctrl+Del (Remove from List)" )]
		[TypeConverter( typeof( EnumTypeConverter ) )]
		public DeleteOptions DeleteMode { get; set; }
		[Category("File Operations" ), LocalizedDisplayName("Target base folder"), LocalizedDescription("Target base folder. %PhotoSift% or relative path (not \\ starting) will be replaced with the location of the software.")]
		[Editor(typeof(FolderNameEditor2), typeof(UITypeEditor))]
		public string TargetFolderPath { get; set; } // TargetFolder_Serializable
		[XmlIgnore]
		[Browsable(false)]
		public string TargetFolder
		{
			get
			{
				string nPath = this.TargetFolderPath.Replace("%PhotoSift%", System.Windows.Forms.Application.StartupPath);
				if (!Path.IsPathRooted(nPath)) nPath = Path.Combine(System.Windows.Forms.Application.StartupPath, nPath); // For filled in the properties panel
				return nPath;
			}
			set
			{
				if (SaveRelativePaths)
					this.TargetFolderPath = value.Replace(System.Windows.Forms.Application.StartupPath, "%PhotoSift%");
				else
					this.TargetFolderPath = value;
			}
		}

		// Appearance Group
#if RLVISION
		[Category( "Appearance" ), LocalizedDisplayName( "Background color" ), LocalizedDescription( "Sets the window background color." )]
		[TypeConverter( typeof( EnumTypeConverter ) )]
		public GrayColors ColorBackground { get; set; }
#else
		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Background color" ), LocalizedDescription( "Sets the window background color." )]
		public Color ColorBackground
		{
			get { return ColorBackground_Serializable.ToColor(); }
			set { ColorBackground_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor ColorBackground_Serializable { get; set; }
#endif

		[Category("Appearance"), LocalizedDisplayName("Gradient background mode"), LocalizedDescription("Set the mode of gradient background colors.")]
		public LineGradientMode ColorGradientBackgroundMode { get; set; }
	
		[XmlIgnore]
		[Category("Appearance"), LocalizedDisplayName("Gradient background color 1"), LocalizedDescription("Set the gradient background colors of the window. The main background color is ignored.")]
		public Color ColorGradientBackgroundOne
		{
			get { return ColorGradientBackgroundOne_Serializable.ToColor(); }
			set { ColorGradientBackgroundOne_Serializable = new SerializableColor(value); }
		}
		[Browsable(false)]
		public SerializableColor ColorGradientBackgroundOne_Serializable { get; set; }
		[XmlIgnore]
		[Category("Appearance"), LocalizedDisplayName("Gradient background color 2"), LocalizedDescription("Set the gradient background colors of the window. The main background color is ignored.")]
		public Color ColorGradientBackgroundTwo
		{
			get { return ColorGradientBackgroundTwo_Serializable.ToColor(); }
			set { ColorGradientBackgroundTwo_Serializable = new SerializableColor(value); }
		}
		[Browsable(false)]
		public SerializableColor ColorGradientBackgroundTwo_Serializable { get; set; }
		[Category("Appearance"), LocalizedDisplayName("Gradient background gamma correction"), LocalizedDescription("Gamma correction is disabled by default. Enabling it may get better or worse result.")]
		public bool ColorGradientBackgroundGammaCorrection { get; set; }


		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Text label color" ), LocalizedDescription( "Sets the font color of text labels." )]
		public Color ColorLabelFront
		{
			get { return ColorLabelFront_Serializable.ToColor(); }
			set { ColorLabelFront_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor ColorLabelFront_Serializable { get; set; }

		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Text label background color" ), LocalizedDescription( "Sets the background color of text labels. Not visible if the background is transparent." )]
		public Color ColorLabelBack
		{
			get { return ColorLabelBack_Serializable.ToColor(); }
			set { ColorLabelBack_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor ColorLabelBack_Serializable { get; set; }

		[Browsable( false )]
		public SerializableFont LabelFont_Serializable { get; set; }
		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Text label font" ), LocalizedDescription( "Sets the font used in text labels." )]
		public Font LabelFont
		{
			get { return LabelFont_Serializable.ToFont(); }
			set { LabelFont_Serializable = SerializableFont.FromFont( value ); }
		}

		[Category( "Appearance" ), LocalizedDisplayName( "Transparent text labels" ), LocalizedDescription( "Decides if text labels should have transparent or solid color background." )]
		public bool ColorTransparentLabels { get; set; }

		[Category( "Appearance" ), LocalizedDisplayName( "Custom menu theme" ), LocalizedDescription("Enables a custom menu theme, based on the four colors specified below.")]
		public bool CustomMenuColors { get; set; }
		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Custom menu theme: Background" ), LocalizedDescription( "Background color" )]
		public Color CustomMenuColorBackground
		{
			get { return CustomMenuColorBackground_Serializable.ToColor(); }
			set { CustomMenuColorBackground_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor CustomMenuColorBackground_Serializable { get; set; }
		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Custom menu theme: Text" ), LocalizedDescription( "Text color" )]
		public Color CustomMenuColorText
		{
			get { return CustomMenuColorText_Serializable.ToColor(); }
			set { CustomMenuColorText_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor CustomMenuColorText_Serializable { get; set; }
		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Custom menu theme: Border" ), LocalizedDescription( "Border color" )]
		public Color CustomMenuColorBorder
		{
			get { return CustomMenuColorHighlight_Serializable.ToColor(); }
			set { CustomMenuColorHighlight_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor CustomMenuColorHighlight_Serializable { get; set; }
		[XmlIgnore]
		[Category( "Appearance" ), LocalizedDisplayName( "Custom menu theme: Highlight" ), LocalizedDescription( "Background color for items under mouse" )]
		public Color CustomMenuColorHightlight
		{
			get { return CustomMenuColorSelected_Serializable.ToColor(); }
			set { CustomMenuColorSelected_Serializable = new SerializableColor( value ); }
		}
		[Browsable( false )]
		public SerializableColor CustomMenuColorSelected_Serializable { get; set; }
		[Category("Appearance"), LocalizedDisplayName("Target path in title bar"), LocalizedDescription("Display the target path in the title bar while no files are in the queue.")]
		public bool TargetPathInTitlebar { get; set; }

		// Controls Group
		[Category("Controls"), LocalizedDisplayName("Repeat interval (ms)"), LocalizedDescription("Interval (ms) for press and hold a key to repeat actions. The value < 100 ms will disalbe this feature. The first trigger will double the time.")]
		public int HoldKeyInterval { get; set; }
		[Category("Controls"), LocalizedDisplayName("Rewind on pool end"), LocalizedDescription("Try to rewind when the pool end is reached while handle.")]
		public bool RewindOnEnd { get; set; }
		[Category("Controls"), LocalizedDisplayName("Loop in pool"), LocalizedDescription("Cycle your pool when you switch images, there is no pool end screen.")]
		public bool LoopInPool { get; set; }
		[Category( "Controls" ), LocalizedDisplayName( "Medium jump" ), LocalizedDescription("Number of images to skip when doing a medium jump. Medium jump is invoked by holding Ctrl and pressing Left/Right arrow key.")]
		public int MediumJump { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Long jump" ), LocalizedDescription("Number of images to skip when doing a long jump. Long jump is invoked by holding Shift and pressing Left/Right arrow key.")]
		public int LargeJump { get; set; }

		[Category("Controls"), LocalizedDisplayName("Confirm to clear Threshold"), LocalizedDescription("Confirm to clear the images pool if the number of images in the pool exceeds this value. The warn is disalbed if set as 0.")]
		public long WarnThresholdOnClearQueue { get; set; }
		[Category( "Controls" ), LocalizedDisplayName( "Escape key to exit" ), LocalizedDescription( "Allows you to exit the program by pressing the Escape key. In fullscreen, Escape key always reverts back to window mode." )]
		public bool CloseOnEscape { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "On delete show next image" ), LocalizedDescription( "When an image is deleted, this determined if the program is to show the image positioned before or after the deleted image." )]
		public bool OnDeleteStepForward { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Auto advance interval" ), LocalizedDescription( "When the auto advance slideshow is enabled, this number determines how long to wait in seconds before showing the next images." )]
		public double AutoAdvanceInterval { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Auto scroll in actual size" ), LocalizedDescription( "In actual size mode (1:1) you can scroll the image. Auto scroll means you only have to move the mouse to scroll. If turned off, you have to click and hold the mouse button to scroll." )]
		public bool ActualSizeAutoScroll { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Auto scroll: Sensitivity" ), LocalizedDescription( "Determines how far you have to move the mouse (in pixels) to fully scroll the image from one side to the other." )]
		public int ActualSizeAutoScrollDistance { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Auto scroll: Float image" ), LocalizedDescription( "Allows the image to freely 'float' around the cursor. Otherwise it will stick to the window borders, limiting the scrolling." )]
		public bool ActualSizeAutoScrollNoLimitInsideForm { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Scale mode: Linear mode" ), LocalizedDescription( "In scale mode (RMB), the mouse position determines the scale factor. If linear, the picture will snap to the new scaled size. If not linear, the image will keep its original scale, but when you move the mouse the scale factor will differ left and right of the original position." )]
		public bool LinearScale { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Scale mode: Snapping" ), LocalizedDescription( "In scale mode (RMB), the zoom level can be set to snap to an interval, for example each 10 percent. Default is 0, meaning freely zoom without snapping." )]
		public int FreeZoomSnap { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Keyboard zoom levels" ), LocalizedDescription( "Enter a comma delimited list of zoom levels to use when using keyboard zoom (+/- keys). Default is: '25,50,75,100,125,150,200'. Enter a single value to use this value as incremental steps instead." )]
		public string ZoomSteps { get; set; }

		[Category( "Controls" ), LocalizedDisplayName( "Limit zoom to window size" ), LocalizedDescription( "If enabled, zooming maxes out when the image size is equal to the window size. In other words, you can not zoom images larger that the current window size." )]
		public bool ZoomLimitMaxToWindowSize { get; set; }
		[Category("Controls"), LocalizedDisplayName("Player intercept keys"), LocalizedDescription("If enabled, the video player will intercept some keyboard keys to perform actions.")]
		public VideoPlayerHookKeysOptions VideoPlayerHookKeysControl { get; set; }
		[Category("Controls"), LocalizedDisplayName("Ignore video beginnings (s)"), LocalizedDescription("If the value is greater than 0, the videos will be seek to the location (in seconds) to ignore the beginning. If the video length is less than this value, it plays from scratch. Seeking is not supported for some video formats.")]
        public int SkipVideoBeginSeconds { get; set; }

		// Display Group
		[Category( "Display" ), LocalizedDisplayName( "Info label" ), LocalizedDescription( "Info label is the one in the top left corner. It shows information about the currently loaded image. In windowed mode, this information is also shown in the window title." )]
		[TypeConverter( typeof( EnumTypeConverter ) )]
		public ShowModes ShowInfoLabel { get; set; }

		[Category( "Display" ), LocalizedDisplayName( "Mode label" ), LocalizedDescription("Mode label is the one in the bottom left corner. It shows notifications about modes changes and similar.")]
		[TypeConverter( typeof( EnumTypeConverter ) )]
		public ShowModes ShowModeLabel { get; set; }

		[Category("Display"), LocalizedDisplayName("Info label format"), LocalizedDescription("Here you can decide what to show in the info label. Available format tags: Filename=%f, Parent folder=%d, Full path=%p, Image width=%w, Image height=%h, Filesize=%s, New line=%n, Image pool total count=%t, Image pool current number=%c")]
		public string InfoLabelFormat { get; set; }
		[Category("Display"), LocalizedDisplayName("Info label format for videos"), LocalizedDescription("Here you can decide what to show in the info label. Available format tags: Filename=%f, Parent folder=%d, Full path=%p, width x height=%wb, width=%w, height=%h, Filesize=%s, New line=%n, pool total count=%t, pool current number=%c, current playback position=%curpos, media duration=%duration")]
		public string InfoLabelFormatVideov2 { get; set; }

		[Category( "Display" ), LocalizedDisplayName( "Hide cursor in fullscreen" ), LocalizedDescription( "If enabled, the cursor will automatically be hidden on inactivity." )]
		public bool FullscreenHideCursor { get; set; }

		[Category( "Display" ), LocalizedDisplayName( "Enlarge small images" ), LocalizedDescription( "Images smaller that the current view can be enlarged to fill the whole view. Hotkey: F4" )]
		public bool EnlargeSmallImages { get; set; }

#if RLVISION
		[Category( "Display" ), LocalizedDisplayName( "Auto move to best suited screen" ), LocalizedDescription( "In a multi-monitor environment, if the application is in fullscreen mode the windows will automatically moves to the best suited monitor (based on aspect ratio) when changing picture." )]
		public bool AutoMoveToScreen { get; set; }
#endif

		// System Group
		[Category( "System" ), LocalizedDisplayName( "Disable screensaver" ), LocalizedDescription( "Disable any screensaver, sleep mode etc while the programis running." )]
		public bool PreventSleep { get; set; }

        
		// Key Folder group
		[Category( "Key Folders" ), DisplayName( "A" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_A { get; set; }
		[Category( "Key Folders" ), DisplayName( "B" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_B { get; set; }
		[Category( "Key Folders" ), DisplayName( "C" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_C { get; set; }
		[Category( "Key Folders" ), DisplayName( "D" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_D { get; set; }
		[Category( "Key Folders" ), DisplayName( "E" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_E { get; set; }
		[Category( "Key Folders" ), DisplayName( "F" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_F { get; set; }
		[Category( "Key Folders" ), DisplayName( "G" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_G { get; set; }
		[Category( "Key Folders" ), DisplayName( "H" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_H { get; set; }
		[Category( "Key Folders" ), DisplayName( "I" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_I { get; set; }
		[Category( "Key Folders" ), DisplayName( "J" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_J { get; set; }
		[Category( "Key Folders" ), DisplayName( "K" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_K { get; set; }
		[Category( "Key Folders" ), DisplayName( "L" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_L { get; set; }
		[Category( "Key Folders" ), DisplayName( "M" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_M { get; set; }
		[Category( "Key Folders" ), DisplayName( "N" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_N { get; set; }
		[Category( "Key Folders" ), DisplayName( "O" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_O { get; set; }
		[Category( "Key Folders" ), DisplayName( "P" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_P { get; set; }
		[Category( "Key Folders" ), DisplayName( "Q" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_Q { get; set; }
		[Category( "Key Folders" ), DisplayName( "R" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_R { get; set; }
		[Category( "Key Folders" ), DisplayName( "S" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_S { get; set; }
		[Category( "Key Folders" ), DisplayName( "T" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_T { get; set; }
		[Category( "Key Folders" ), DisplayName( "U" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_U { get; set; }
		[Category( "Key Folders" ), DisplayName( "V" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_V { get; set; }
		[Category( "Key Folders" ), DisplayName( "W" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_W { get; set; }
		[Category( "Key Folders" ), DisplayName( "X" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_X { get; set; }
		[Category( "Key Folders" ), DisplayName( "Y" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_Y { get; set; }
		[Category( "Key Folders" ), DisplayName( "Z" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_Z { get; set; }
		[Category( "Key Folders" ), DisplayName( "0" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_0 { get; set; }
		[Category( "Key Folders" ), DisplayName( "1" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_1 { get; set; }
		[Category( "Key Folders" ), DisplayName( "2" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_2 { get; set; }
		[Category( "Key Folders" ), DisplayName( "3" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_3 { get; set; }
		[Category( "Key Folders" ), DisplayName( "4" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_4 { get; set; }
		[Category( "Key Folders" ), DisplayName( "5" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_5 { get; set; }
		[Category( "Key Folders" ), DisplayName( "6" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_6 { get; set; }
		[Category( "Key Folders" ), DisplayName( "7" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_7 { get; set; }
		[Category( "Key Folders" ), DisplayName( "8" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_8 { get; set; }
		[Category( "Key Folders" ), DisplayName( "9" ), LocalizedDescription( "Sets the destination folder where images go when this key is pressing. This can be a subfolder relative to the current base target folder, or a complete path. Clear to use default. See readme file for a more detailed explanation of the various options." )]
		[Editor( typeof( FolderNameEditor2 ), typeof( UITypeEditor ) )]
		public string KeyFolder_9 { get; set; }

		// Cache settings
		[Category("Cache"), LocalizedDisplayName("Cache Ahead (earlier)"), LocalizedDescription("Pre-reading x pictures earlier than the current image in image pool.")]
		public int CacheAhead { get; set; }
		[Category("Cache"), LocalizedDisplayName("Cache Behind (later)"), LocalizedDescription("Pre-reading x pictures later than the current image in image pool.")]
		public int CacheBehind { get; set; }

		// FileType settings
		[Category("File Type"), LocalizedDisplayName("Image exts"), LocalizedDescription("File extensions allowed to be added to the pool. Will be ignored if the Check MIME is on.")]
		public string[] allowsPicExts { get; set; }
		[Category("File Type"), LocalizedDisplayName("Video exts"), LocalizedDescription("File extensions allowed to be added to the pool. Will be ignored if the Check MIME is on.")]
		public string[] allowsVidExts { get; set; }
		[Category("File Type"), LocalizedDisplayName("Check MIME"), LocalizedDescription("Check the MIME type of the file instead of checking the extensions.")]
		public FeatureSwitch FileMIMEChecker { get; set; }
		[Category("File Type"), LocalizedDisplayName("Allowed MIME"), LocalizedDescription("File MIME types allowed to be added to the pool. Work only while the Check MIME option be turn on. Semicolon separated. Spaces on edge are ignored. Default: 'image/;video/;audio/'.")]
		public string allowsMIME { get; set; }
		[Category("File Type"), LocalizedDisplayName("Expand folder shortcuts"), LocalizedDescription("When adding files, the program will only parse one folder shortcut (if it is the first) to avoid flooding. If enabled, it attempts to recursively resolve all folder shortcuts.")]
		public bool expandFolderLnks { get; set; }

		// Misc settings
		[Category("Misc"), LocalizedDisplayName("Copy action"), LocalizedDescription("Sets the action type when you press Ctrl+C or click the \"Copy to clipboard\" menu in this software.")]
		public CopytoClipboardOptions CopyActionType { get; set; }
		[Category("Misc"), LocalizedDisplayName("Save relative paths"), LocalizedDescription("Save the path relative to the location of the program, for paths such as the target folder.")]
		public bool SaveRelativePaths { get; set; }

		// Settings located on the GUI menus (not visible in the property grid)
		[Browsable( false )]
		public bool AddInRandomOrder { get; set; }
		[Browsable(false)]
		public bool ResetViewModeOnPictureChange { get; set; }
		[Browsable(false)]
		public bool MoveIncludingCurrent { get; set; }


		// Hidden / Non-user Settings
		[Browsable( false )]
		public Rectangle FormRect_Main { get; set; }
		[Browsable( false )]
		public Rectangle FormRect_Settings { get; set; }
		[Browsable(false)]
		public System.Windows.Forms.FormWindowState WindowState { get; set; }

		[Browsable( false )]
		public string LastFolder_AddFolder { get; set; }
		[Browsable( false )]
		public string LastFolder_AddFiles { get; set; }

		[Browsable( false )]
		public bool FirstTimeUsing { get; set; }

		[Browsable( false )]
		public int FullscreenCursorAutoHideTime { get; set; }

		[Browsable( false )]
		public DateTime Stats_FirstLaunchDate { get; set; }
		[Browsable( false )]
		public int Stats_StartupCount { get; set; }
		[Browsable( false )]
		public int Stats_LoadedPics { get; set; }
		[Browsable( false )]
		public int Stats_RenamedPics { get; set; }
		[Browsable( false )]
		public int Stats_MovedPics { get; set; }
		[Browsable( false )]
		public int Stats_CopiedPics { get; set; }
		[Browsable( false )]
		public int Stats_DeletedPics { get; set; }

		[XmlIgnore]
		public Dictionary<string, object> defaultSettings = new Dictionary<string, object>();
		public AppSettings()
		{
			// File Operations Group
			defaultSettings.Add("FileMode", FileOperations.Move);
			defaultSettings.Add("ExistingFiles", ExistingFileOptions.AppendNumber);
			defaultSettings.Add("DeleteMode", DeleteOptions.RecycleBin);
			defaultSettings.Add("TargetFolderPath", "%PhotoSift%");

			// Appearance Group
#if RLVISION
			defaultSettings.Add("ColorBackground", GrayColors.Col7);
#else
			defaultSettings.Add("ColorBackground", Color.Black);
#endif
			defaultSettings.Add("ColorGradientBackgroundMode", LineGradientMode.Off);
			defaultSettings.Add("ColorGradientBackgroundOne", Color.Gray);
			defaultSettings.Add("ColorGradientBackgroundTwo", Color.Black);
			defaultSettings.Add("ColorGradientBackgroundGammaCorrection", false);
			defaultSettings.Add("ColorLabelFront", Color.FromArgb(192, 64, 0));
			defaultSettings.Add("ColorLabelBack", Color.Black);
			defaultSettings.Add("ColorTransparentLabels", true);
			defaultSettings.Add("LabelFont", new Font("Arial", 10, FontStyle.Regular));
			defaultSettings.Add("CustomMenuColors", true);
			defaultSettings.Add("CustomMenuColorBackground", Color.FromArgb(255, 45, 45, 45));
			defaultSettings.Add("CustomMenuColorText", Color.FromArgb(255, 255, 255, 255));
			defaultSettings.Add("CustomMenuColorBorder", Color.FromArgb(255, 128, 128, 128));
			defaultSettings.Add("CustomMenuColorHightlight", Color.FromArgb(255, 65, 65, 65));
			defaultSettings.Add("TargetPathInTitlebar", true);

			// Controls Group
			defaultSettings.Add("HoldKeyInterval", 300);
			defaultSettings.Add("RewindOnEnd", false);
			defaultSettings.Add("LoopInPool", false);
			defaultSettings.Add("MediumJump", 10);
			defaultSettings.Add("LargeJump", 25);
			defaultSettings.Add("WarnThresholdOnClearQueue", 0);
			defaultSettings.Add("CloseOnEscape", false);
			defaultSettings.Add("OnDeleteStepForward", true);
			defaultSettings.Add("AutoAdvanceInterval", 4.5);
			defaultSettings.Add("ActualSizeAutoScroll", true);
			defaultSettings.Add("ActualSizeAutoScrollDistance", 100);
			defaultSettings.Add("ActualSizeAutoScrollNoLimitInsideForm", false);
			defaultSettings.Add("LinearScale", false);
			defaultSettings.Add("FreeZoomSnap", 0);
			defaultSettings.Add("ZoomSteps", "5,10,25,50,75,100,125,150,175,200");
			defaultSettings.Add("ZoomLimitMaxToWindowSize", false);
			defaultSettings.Add("VideoPlayerHookKeysControl", VideoPlayerHookKeysOptions.Basic);
			defaultSettings.Add("SkipVideoBeginSeconds", 0);

			// Display Group
			defaultSettings.Add("ShowInfoLabel", ShowModes.FullscreenOnly);
			defaultSettings.Add("ShowModeLabel", ShowModes.AlwaysShow);
			defaultSettings.Add("InfoLabelFormat", "(%c / %t) %f");
			defaultSettings.Add("InfoLabelFormatVideov2", "(%c / %t) %f  %curpos / %duration  %wh");
			
			defaultSettings.Add("FullscreenHideCursor", true);
			defaultSettings.Add("EnlargeSmallImages", false);
#if RLVISION
			defaultSettings.Add("AutoMoveToScreen", false);
#endif
			// System Group
			defaultSettings.Add("PreventSleep", false);

			// Cache settings
			defaultSettings.Add("CacheAhead", 2);
			defaultSettings.Add("CacheBehind", 1);

			// File Type
			defaultSettings.Add("allowsPicExts", Util.Def_allowsPicExts);
			defaultSettings.Add("allowsVidExts", Util.Def_allowsVideoExts);
			defaultSettings.Add("FileMIMEChecker", FeatureSwitch.Disabled);
			defaultSettings.Add("allowsMIME", "image/);video/);audio/");
			defaultSettings.Add("expandFolderLnks", false);
			
			// Misc
			defaultSettings.Add("SaveRelativePaths", true);
			defaultSettings.Add("CopyActionType", CopytoClipboardOptions.Bitmap);

			// GUI settings
			defaultSettings.Add("AddInRandomOrder", false);
			defaultSettings.Add("ResetViewModeOnPictureChange", true);
			defaultSettings.Add("MoveIncludingCurrent", false);

			// Hidden settings
			defaultSettings.Add("FormRect_Main", new Rectangle());
			defaultSettings.Add("FormRect_Settings", new Rectangle());
			defaultSettings.Add("FirstTimeUsing", true);
			defaultSettings.Add("FullscreenCursorAutoHideTime", 3000);
			defaultSettings.Add("Stats_FirstLaunchDate", DateTime.Now);
			defaultSettings.Add("Stats_StartupCount", 0);
			defaultSettings.Add("Stats_LoadedPics", 0);
			defaultSettings.Add("Stats_RenamedPics", 0);
			defaultSettings.Add("Stats_MovedPics", 0);
			defaultSettings.Add("Stats_CopiedPics", 0);
			defaultSettings.Add("Stats_DeletedPics", 0);

			foreach( System.Reflection.PropertyInfo Prop in typeof( AppSettings ).GetProperties() )
			{
				if (Prop.Name.StartsWith("KeyFolder_"))
					defaultSettings.Add(Prop.Name, "");

				if (defaultSettings.TryGetValue(Prop.Name, out object value))
					Prop.SetValue(this, value); // apply default settings
			}
		}

	}

	// Setting values

	public enum ShowModes
	{
		[LocalizedDescription( "Always Show" )]
		AlwaysShow,
		[LocalizedDescription( "Always Hide" )]
		AlwaysHide,
		[LocalizedDescription( "Fullscreen Only" )]
		FullscreenOnly,
		[LocalizedDescription( "Windowed Only" )]
		WindowedOnly,
	}

	public enum YesNo
	{
		[LocalizedDescription( "Yes" )]
		Yes = 1,
		[LocalizedDescription( "No" )]
		No = 0,
	}

	public enum DeleteOptions
	{
		[LocalizedDescription( "Delete File" )]
		Delete,
		[LocalizedDescription( "Delete to Recycle Bin" )]
		RecycleBin,
		[LocalizedDescription( "Only Remove from Image Pool" )]
		RemoveFromList,
	}

	public enum FileOperations 
	{
		[LocalizedDescription( "Copy" )]
		Copy,
		[LocalizedDescription( "Move" )]
		Move,
	}

	public enum ExistingFileOptions
	{
		[LocalizedDescription( "Overwrite" )]
		Overwrite,
		[LocalizedDescription( "Append Number" )]
		AppendNumber,
		[LocalizedDescription( "Skip" )]
		Skip,
	}

	public enum CopytoClipboardOptions
	{
		[LocalizedDescription("Bitmap")]
		Bitmap,
		[LocalizedDescription("File")]
		File,
		[LocalizedDescription("File Path")]
		FilePath,
	}
	public enum VideoPlayerHookKeysOptions
	{
		[LocalizedDescription("Disabled")]
		Disabled = 0,
		[LocalizedDescription("Basic")]
		Basic = 1,
		//[LocalizedDescription("Enhance")]
		//Enhance = 2,
	}
	
	public enum FeatureSwitch
	{
		[LocalizedDescription("Disabled")]
		Disabled = 0,
		[LocalizedDescription("Enabled")]
		Enabled = 1,
		[LocalizedDescription("Unavailable")] // Deprecated
		Unavailable = -1,
	}
	public enum LineGradientMode
	{
		Off = -1,
		Horizontal = 0,
		Vertical = 1,
		ForwardDiagonal = 2,
		BackwardDiagonal = 3
	}

#if RLVISION
	public enum GrayColors
	{
		[Description( "White" )]
		Col1 = 255,
		[Description( "Lighter Gray (30)" )]
		Col2 = 191,
		[Description( "Light Gray (40)" )]
		Col3 = 98,
		[Description( "Medium Gray (51)" )]
		Col4 = 51,
		[Description( "Dark Gray (98)" )]
		Col5 = 40,
		[Description( "Darker Gray (191)" )]
		Col6 = 30,
		[Description( "Black" )]
		Col7 = 0,
	}
#endif

}


