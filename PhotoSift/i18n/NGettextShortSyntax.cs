using System;
using System.Diagnostics;
using System.Globalization;
using NGettext;

//
// Usage:
//		using static Example.NGettextShortSyntax;
//		
//		_("Hello, World!"); // GetString
//		_n("You have {0} apple.", "You have {0} apples.", count, count); // GetPluralString
//		_p("Context", "Hello, World!"); // GetParticularString
//		_pn("Context", "You have {0} apple.", "You have {0} apples.", count, count); // GetParticularPluralString
//
namespace PhotoSift
{
	internal static class NGettextShortSyntax
	{
		private static ICatalog _Catalog = new Catalog("PhotoSift", "locale");
		//private static readonly ICatalog _Catalog = new Catalog("PhotoSift", "locale", new CultureInfo("zh-CN"));

		public static void setUICultureInfo(CultureInfo locale)
		{
			_Catalog = new Catalog("PhotoSift", "locale", locale);
		}
		public static void setUICultureLCID(int LCID)
		{
			if (LCID > 0)
            {
				var locale = new CultureInfo(LCID);
				_Catalog = new Catalog("PhotoSift", "locale", locale);
			}
			else
            {
				_Catalog = new Catalog("PhotoSift", "locale");
			}
		}

		public static string _(string text)
		{
			return _Catalog.GetString(text);
		}

		public static string _(string text, params object[] args)
		{
			return _Catalog.GetString(text, args);
		}

		public static string _n(string text, string pluralText, long n)
		{
			return _Catalog.GetPluralString(text, pluralText, n);
		}

		public static string _n(string text, string pluralText, long n, params object[] args)
		{
			return _Catalog.GetPluralString(text, pluralText, n, args);
		}

		public static string _p(string context, string text)
		{
			return _Catalog.GetParticularString(context, text);
		}

		public static string _p(string context, string text, params object[] args)
		{
			return _Catalog.GetParticularString(context, text, args);
		}

		public static string _pn(string context, string text, string pluralText, long n)
		{
			return _Catalog.GetParticularPluralString(context, text, pluralText, n);
		}

		public static string _pn(string context, string text, string pluralText, long n, params object[] args)
		{
			return _Catalog.GetParticularPluralString(context, text, pluralText, n, args);
		}
	}
	public class GlNGettext {
		private static ICatalog _Catalog = new Catalog("PhotoSift", "locale");

		public GlNGettext(CultureInfo locale) {
			_Catalog = new Catalog("PhotoSift", "locale", locale);
		}
	}
}