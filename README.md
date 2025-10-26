# PhotoSift Ex
This is a fork of a fork of Photosift, with this fork adding HEIC support through WIC/Openize.

This a fork with audio/video playback, batch operations, more options and other optimizations. It also provides localized interface.

View the release notes and downloads at https://github.com/yfdyh000/PhotoSift/releases.

This program requires .NET Framework 4.8, the Windows Media Player Library, etc. Available on Microsoft Windows.

## Development
I have a big roadmap (list of ideas, not written in English), but probably don't have enough energy to complete all. If you are interested in the development of this project, open an issue or contact me.

## Localization
1. Open the [PhotoSift.po](https://github.com/yfdyh000/PhotoSift/blob/master/PhotoSift/locale/zh-CN/LC_MESSAGES/PhotoSift.po) file with [Poedit](https://poedit.net/).
2. Execute the "Extract from source" for cloned repo. (optional)
3. Change necessary properties like target language. Clear all and make your translation.
4. Move to the correct language code directory, and update the UILanguages definition. (optional)
5. If needed, generate the mo file and place it in \<this software\>\locale\en-US\LC_MESSAGES\PhotoSift.po directory to testing your translation.

# PhotoSift

The original: https://github.com/rlv-dan/PhotoSift, http://rlvision.com/photosift.

PhotoSift is a free utility helping you to quickly organize unsorted image libraries. The basic idea is to load the program with images, then show and inspect each image and press a key on the keyboard. The image will then be moved or copied to a folder of your choice corresponding to that key, and the next images is displayed. This allows you to rapidly go through and organize your large amounts of pictures.

Download the application at http://rlvision.com/photosift

* Developed with C#, targeting .Net 2.0
* Multithreaded cache-ahead image loading
* Various inspection tools (zoom, flip, rotate)
* Support fullscreen and multiple monitors
* Highly configurable using a propertygrid
* Can rename and delete files if needed
* Portable, settings are saved as XML in application folder

<img src="http://rlvision.com/photosift/screenshot.png">
