﻿//************************************************************************************************
// Copyright © 2020 Steven M Cohn.  All rights reserved.
//************************************************************************************************

namespace River.OneMoreAddIn.Commands
{
	using River.OneMoreAddIn.Models;
	using River.OneMoreAddIn.Styles;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Forms;
	using System.Xml.Linq;


	internal class TaggingCommand : Command
	{
		private const string BankStyleName = "omWordBank";

		private const string RibbonSymbol = "26";	// the award ribbon Tag symbol
		private const int TitleType = 99;			// custom TagDef type for tagged title		
		private const int BankType = 23;			// custom TagDef type for word bank outline


		public TaggingCommand()
		{
		}


		public override void Execute(params object[] args)
		{
			using (var one = new OneNote(out var page, out var ns))
			{
				using (var dialog = new TaggingDialog())
				{
					var content = page.GetMetaContent(Page.TaggingMetaName);
					if (!string.IsNullOrEmpty(content))
					{
						var parts = content.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => s.Trim())
							.ToList();

						dialog.Tags = parts;
					}

					if (dialog.ShowDialog(owner) != DialogResult.OK)
					{
						return;
					}

					page.SetMeta(Page.TaggingMetaName, string.Join(",", dialog.Tags));

					MakeWordBank(page, ns, dialog.Tags);

					one.Update(page);
				}
			}
		}


		private void MakeWordBank(Page page, XNamespace ns, List<string> words)
		{
			var content = string.Join(AddIn.Culture.TextInfo.ListSeparator, words);

			var quickIndex = MakeQuickStyle(page);
			var tagIndex = MakeRibbonTagDef(page);

			var outline = page.Root.Elements(ns + "Outline")
				.FirstOrDefault(e => e.Elements().Any(x =>
					x.Name.LocalName == "Meta" &&
					x.Attribute("name").Value == Page.TagBankMetaName));

			XCData cdata;

			if (outline == null)
			{
				var tag = new XElement(ns + "Tag",
					new XAttribute("index", tagIndex),
					new XAttribute("completed", "true"),
					new XAttribute("disabled", "false"));

				cdata = new XCData(content); // $"<span style='font-weight:bold'>{content}</span>");

				outline = new XElement(ns + "Outline",
					new XElement(ns + "Position",
						new XAttribute("x", "235"),
						new XAttribute("y", "43"),
						new XAttribute("z", "0")),
					new XElement(ns + "Size",
						new XAttribute("width", "400"),
						new XAttribute("height", "11"),
						new XAttribute("isSetByUser", "true")),
					new XElement(ns + "Meta",
						new XAttribute("name", Page.TagBankMetaName),
						new XAttribute("content", "1")),
					new XElement(ns + "OEChildren",
						new XElement(ns + "OE",
							new XAttribute("quickStyleIndex", quickIndex),
							tag,
							new XElement(ns + "T", cdata)
						))
					);

				page.Root.Elements(ns + "Title").First().AddAfterSelf(outline);
			}
			else
			{
				cdata = outline.Descendants(ns + "T").FirstOrDefault().GetCData();
				if (cdata != null)
				{
					cdata.Value = content; // $"<span style='font-weight:bold'>{content}</span>";
				}
			}
		}


		private int MakeQuickStyle(Page page)
		{
			var styles = page.GetQuickStyles();
			var style = styles.FirstOrDefault(s => s.Name == BankStyleName);

			if (style != null)
			{
				return style.Index;
			}

			var quick = StandardStyles.Citation.GetDefaults();
			quick.Index = styles.Max(s => s.Index) + 1;
			quick.Name = BankStyleName;
			quick.IsBold = true;

			page.AddQuickStyleDef(quick.ToElement(page.Namespace));

			return quick.Index;
		}


		private string MakeRibbonTagDef(Page page)
		{
			var index = page.GetTagDefIndex(RibbonSymbol);
			if (index == null)
			{
				index = page.AddTagDef(RibbonSymbol, "Page Tags", BankType);
			}

			return index;
		}
	}
}
/*
  <one:Outline>
    <one:Position x="236.0000152587891" y="43.0" z="0" />
    <one:Size width="400.0000610351562" height="10.98629760742187" isSetByUser="true" />
    <one:Meta name="omTagBox" content="1" />
    <one:Indents>
      <one:Indent level="0" indent="1.6536735019749E-19" />
    </one:Indents>
    <one:OEChildren>
      <one:OE alignment="left" quickStyleIndex="1">
        <one:Tag index="10" completed="true" disabled="false" />
        <one:T><![CDATA[<span style='font-weight:bold'>#Cheese, #Grape</span>]]></one:T>
      </one:OE>
    </one:OEChildren>
  </one:Outline>
*/