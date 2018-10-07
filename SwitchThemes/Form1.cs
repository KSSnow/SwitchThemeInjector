﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SARCExt;
using SwitchThemes.Common;
using SwitchThemes.Common.Bntxx;
using Syroot.BinaryData;

namespace SwitchThemes
{
	public partial class Form1 : MaterialSkin.Controls.MaterialForm
	{
		PatchTemplate targetPatch;
		SarcData CommonSzs = null;
		
		readonly string PatchLabelText = "";
		readonly string LoadFileText = "";

		List<PatchTemplate> Templates = new List<PatchTemplate>();

		public Form1()
		{
			InitializeComponent();
			PatchLabelText = materialLabel3.Text;

			//PatchTemplate.BuildTemplateFile();
			Templates.AddRange(DefaultTemplates.templates);
			if (File.Exists("ExtraTemplates.json"))
				Templates.AddRange(PatchTemplate.LoadTemplates());

			LoadFileText = SwitchThemesCommon.GeneratePatchListString(Templates);
			tbPatches.Text = LoadFileText;

			materialTabSelector1.Enabled = false;
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			MaterialSkin.MaterialSkinManager.Instance.Theme = MaterialSkin.MaterialSkinManager.Themes.DARK;
			materialLabel1.ForeColor = Color.White;
			lblDetected.ForeColor = Color.White;			
		}

		//private void ExtractBntxButton(object sender, EventArgs e)
		//{
		//	if (!materialTabSelector1.Enabled || CommonSzs == null)
		//	{
		//		MessageBox.Show("Open a theme file first");
		//		return;
		//	}
		//	if (targetPatch == null)
		//	{
		//		MessageBox.Show("Version unsupported");
		//		return;
		//	}
		//	SaveFileDialog sav = new SaveFileDialog()
		//	{
		//		Filter = "bntx file|*.bntx",
		//		Title = "Save theme resources",
		//		FileName = "__Combined.bntx"
		//	};
		//	if (sav.ShowDialog() != DialogResult.OK)
		//		return;
		//	File.WriteAllBytes(sav.FileName, CommonSzs.Files[@"timg/__Combined.bntx"]);
		//	MessageBox.Show("Done");
		//}
		
		private void DragForm_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}


		private void OpenSzsButton(object sender, EventArgs e)
		{
			OpenFileDialog opn = new OpenFileDialog()
			{
				Title = "open szs",
				Filter = "szs file|*.szs|all files|*.*",
			};
			if (opn.ShowDialog() != DialogResult.OK)
				return;
			if (!File.Exists(opn.FileName))
			{
				MessageBox.Show("Could not open file");
				return;
			}

			targetPatch = null;

			CommonSzs = SARCExt.SARC.UnpackRamN(ManagedYaz0.Decompress(File.ReadAllBytes(opn.FileName)));

#if DEBUG
			#region Check snippets
			//diff file lists :
			//if (opn.ShowDialog() != DialogResult.OK)
			//	return;
			//var szs2 = SARCExt.SARC.UnpackRamN(YAZ0.Decompress(File.ReadAllBytes(opn.FileName)));
			//foreach (var f in szs2.Files.Keys)
			//{
			//	if (CommonSzs.Files.ContainsKey(f))
			//		CommonSzs.Files.Remove(f);
			//	else
			//		Console.WriteLine($"{f} missing");
			//}
			//if (CommonSzs.Files.Count != 0)
			//{
			//	Console.WriteLine($"Files left : \r\n {string.Join("\r\n", CommonSzs.Files.Keys.ToArray())}");
			//}
			//find a string or a panel by name
			//foreach (string k in CommonSzs.Files.Keys)
			//{
			//	if (UTF8Encoding.Default.GetString(CommonSzs.Files[k]).Contains("NavBg_03^d"))
			//	{
			//		Console.WriteLine(k);
			//	}
			//if (k.EndsWith(".bflyt"))
			//{
			//	var l = BflytFromSzs(k);
			//	foreach (var p in l.Panels)
			//	{
			//		string pName = BflytFile.TryGetPanelName(p);
			//		if (pName == null) continue;
			//		if (pName.ToLower().Contains("bg")) Console.WriteLine($"{k} : {p.name} {pName}");
			//	}
			//}
			//}
			#endregion
#endif

			targetPatch = SwitchThemesCommon.DetectSarc(CommonSzs, Templates);

			if (targetPatch == null)
			{
				MessageBox.Show("This is not a valid theme file, if it's from a newer firmware it's not compatible with this tool yet");
				CommonSzs = null;
				targetPatch = null;
				lblDetected.Text = "";
				return;
			}
			
			materialLabel3.Text = string.Format(PatchLabelText, targetPatch.szsName, targetPatch.TitleId);
			lblDetected.Text = "Detected " + targetPatch.TemplateName + " " + targetPatch.FirmName;
			
			materialTabSelector1.Enabled = true;
		}

		private void materialFlatButton1_Click(object sender, EventArgs e)
		{
			OpenFileDialog opn = new OpenFileDialog()
			{
				Title = "open dds picture",
				Filter = "*.dds|*.dds|all files|*.*",
			};
			if (opn.ShowDialog() != DialogResult.OK)
				return;
			if (opn.FileName != "")
				tbBntxFile.Text = opn.FileName;
		}
		

		private void PatchButtonClick(object sender, EventArgs e)
		{
			if (CommonSzs == null || targetPatch == null)
			{
				MessageBox.Show("Open a theme first !");
				return;
			}
			if (tbBntxFile.Text.Trim() == "")
			{
				if (MessageBox.Show("Are you sure you want to continue without selecting a bntx ? Unless the szs already contains a custom one the theme will most likely crash", "", MessageBoxButtons.YesNo) == DialogResult.No)
					return;
			}
			else if (!File.Exists(tbBntxFile.Text))
			{
				MessageBox.Show($"{tbBntxFile.Text} not found !");
				return;
			}

			SaveFileDialog sav = new SaveFileDialog()
			{
				Filter = "szs file|*.szs",
				FileName = targetPatch.szsName
			};
			if (sav.ShowDialog() != DialogResult.OK) return;

			if (tbBntxFile.Text.Trim() != "")
			{
				if (SwitchThemesCommon.PatchBntx(CommonSzs, File.ReadAllBytes(tbBntxFile.Text), targetPatch) == BflytFile.PatchResult.Fail)
				{
					MessageBox.Show(
							"Can't build this theme: the szs you opened doesn't contain some information needed to patch the bntx," +
							"without this information it is not possible to rebuild the bntx." +
							"You should use an original or at least working szs", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}

			var res = SwitchThemesCommon.PatchLayouts(CommonSzs, targetPatch);

			if (res == BflytFile.PatchResult.Fail)
			{
				MessageBox.Show("Couldn't patch this file, it might have been already modified or it's from an unsupported system version.");
				return;
			}
			else if (res == BflytFile.PatchResult.CorruptedFile)
			{
				MessageBox.Show("This file has been already patched with another tool and is not compatible, you should get an unmodified layout.");
				return;
			}

			var sarc = SARC.PackN(CommonSzs);
			
			File.WriteAllBytes(sav.FileName, ManagedYaz0.Compress(sarc.Item2, 3, (int)sarc.Item1));
			GC.Collect();

			if (res == BflytFile.PatchResult.AlreadyPatched)
				MessageBox.Show("Done, This file has already been patched, only the bntx was replaced.\r\nIf you have issues try with an unmodified file");
			else
				MessageBox.Show("Done");
		}

		int eggCounter = 0;
		private void label1_Click(object sender, EventArgs e)
		{
			if (eggCounter++ == 5)
				MessageBox.Show("---ALL YOUR THEMES ARE BELONG TO US---");
			else
				MessageBox.Show("Switch theme injector V 3.0\r\nby exelix\r\n\r\nTeam Qcean:\r\nCreatable, einso, GRAnimated, Traiver, Cellenseres, Vorphixx, SimonMKWii, Exelix\r\n\r\nDiscord invite code : GrKPJZt");
		}

		

		// old implementation
		//BflytFile.PatchResult PatchBntx(byte[] Bntx)
		//{
		//	var ImportBntxReader = new BinaryDataReader(new MemoryStream(Bntx));
		//	ImportBntxReader.ByteOrder = ByteOrder.LittleEndian;
		//	ImportBntxReader.BaseStream.Position = 0x18;
		//	ImportBntxReader.BaseStream.Position = ImportBntxReader.ReadUInt32();
		//	int fileRltLen = (int)(ImportBntxReader.BaseStream.Length - ImportBntxReader.BaseStream.Position);
		//	if (fileRltLen == 0x80) //this file has an original rlt
		//	{
		//		ImportBntxReader.Dispose();
		//		CommonSzs.Files[@"timg/__Combined.bntx"] = Bntx;
		//		return BflytFile.PatchResult.OK;
		//	}
		//	else //the rlt has to be fixed
		//	{
		//		var reader = new BinaryDataReader(new MemoryStream(CommonSzs.Files[@"timg/__Combined.bntx"]));
		//		reader.ByteOrder = ByteOrder.LittleEndian;
		//		reader.BaseStream.Position = 0x18;
		//		reader.BaseStream.Position = reader.ReadUInt32();
		//		if (reader.BaseStream.Length - reader.BaseStream.Position > 0x80) //the rlt in the theme is corrupted
		//		{
		//			MessageBox.Show(
		//					"Can't build this theme: the szs you opened doesn't contain some information needed to patch the bntx," +
		//					"without this information it is not possible to rebuild the bntx." +
		//					"You should use an original or at least working szs", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		//			return BflytFile.PatchResult.Fail;
		//		}
		//		reader.BaseStream.Position += 8;
		//		var OriginalRlt = reader.ReadBytes(0x80 - 8);
		//		reader.Dispose();
		//		MemoryStream mem = new MemoryStream();
		//		var writer = new BinaryDataWriter(mem);
		//		writer.ByteOrder = ByteOrder.LittleEndian;
		//		ImportBntxReader.BaseStream.Position = 0;
		//		writer.Write(ImportBntxReader.ReadBytes(0x18));
		//		int rltOffset = ImportBntxReader.ReadInt32();
		//		int fileLen = rltOffset + OriginalRlt.Length + 8;
		//		writer.Write(rltOffset);
		//		writer.Write(fileLen);
		//		ImportBntxReader.ReadInt32(); //skip file size
		//		writer.Write(ImportBntxReader.ReadBytes(rltOffset - 0x20));
		//		writer.Write("_RLT", BinaryStringFormat.NoPrefixOrTermination);
		//		writer.Write(rltOffset);
		//		writer.Write(OriginalRlt);
		//		CommonSzs.Files[@"timg/__Combined.bntx"] = mem.ToArray();
		//		writer.Dispose();
		//		mem.Dispose();
		//		return BflytFile.PatchResult.OK;
		//	}
		//}
	}
}