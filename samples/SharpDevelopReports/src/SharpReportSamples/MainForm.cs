﻿/*
 * Erstellt mit SharpDevelop.
 * Benutzer: Peter Forstmeier
 * Datum: 03.01.2010
 * Zeit: 17:43
 * 
 * Sie können diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader ändern.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using ICSharpCode.Reports.Core;

namespace SharpReportSamples
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		
		private TreeNode formNode;
		private TreeNode pullNode;
		private TreeNode iListNode;
		private TreeNode providerIndependent;
		private TreeNode customized;
		
		
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			InitTree();
			UpdateStatusbar (Application.StartupPath);
			this.previewControl1.Messages = new ReportViewerMessagesProvider();
			
			/*
			EventLog ev = new EventLog();
			ev.Log = "System";
			ev.MachineName = ".";  // Lokale Maschine
			ArrayList ar = new ArrayList();
			ar.AddRange(ev.Entries);
			 */
		}
		
		
		private void InitTree ()
		{
			string formSheetDir = @"\FormSheet\JCA.srd";
			
			string startupPath = Application.StartupPath;
			string samplesDir = @"SharpDevelopReports\";
			int y = startupPath.IndexOf(samplesDir);
			string startPath = startupPath.Substring(0,y + samplesDir.Length) + @"SampleReports\";
			
			//D:\Reporting3.0_branches\SharpDevelop\samples\SharpDevelopReports\SampleReports
			
			string pathToFormSheet = startPath + formSheetDir;
			
			this.formNode = this.treeView1.Nodes[0].Nodes[0];
			this.pullNode =  this.treeView1.Nodes[0].Nodes[1];
			this.iListNode = this.treeView1.Nodes[0].Nodes[2];
			this.providerIndependent = this.treeView1.Nodes[0].Nodes[3];
			this.customized = this.treeView1.Nodes[0].Nodes[4];
			
			AddNodesToTree (this.formNode,startPath + @"FormSheet\" );
			AddNodesToTree (this.pullNode,startPath + @"PullModel\" );
			AddNodesToTree (this.iListNode,startPath + @"IList\" );
			AddNodesToTree (this.providerIndependent,startPath + @"ProviderIndependent\" );
			AddNodesToTree (this.customized,startPath + @"Customized\" );
			
		}
		
		private void AddNodesToTree (TreeNode parent,string path)
		{
			if (!Directory.Exists(path)) {
				return;
			}
			string[] filePaths = Directory.GetFiles(path, "*.srd");
			TreeNode reportNode = null;
			foreach (string fullPath in filePaths){
				string fileName = Path.GetFileNameWithoutExtension(fullPath);
				reportNode = new TreeNode(fileName);
				reportNode.Tag = fullPath;
				parent.Nodes.Add(reportNode);
			}
		}
		
		
		
		private void UpdateStatusbar (string text)
		{
			this.label1.Text = text;
		}
		
		
		private void RunStandardReport(string reportName)
		{
			string s = Path.GetFileNameWithoutExtension(reportName);
			if (s == "ContributorsList" ) {
				this.RunContributors(reportName);
			} else if (s == "NoConnectionReport") {
				this.RunProviderIndependent(reportName);
			} else if (s =="EventLog")
				this.RunEventlogger(reportName);
			else {
				
				ReportParameters parameters =  ReportEngine.LoadParameters(reportName);
				
				if ((parameters != null)&& (parameters.SqlParameters.Count > 0)){
					parameters.SqlParameters[0].ParameterValue = "I'm the Parameter";
				}
				this.previewControl1.PreviewLayoutChanged += delegate (object sender, EventArgs e)
				{
					this.RunStandardReport(reportName);
				};
				this.previewControl1.RunReport(reportName,parameters);
			}
		}
		
		
		#region ProviderIndependent
		private void RunProviderIndependent (string reportName)
		{
			string conOleDbString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=D:\SharpReport_TestReports\TestReports\Nordwind.mdb;Persist Security Info=False";
			ReportParameters parameters =  ReportEngine.LoadParameters(reportName);
			ConnectionObject con = ConnectionObject.CreateInstance(conOleDbString,
			                                                       System.Data.Common.DbProviderFactories.GetFactory("System.Data.OleDb") );
			
			parameters.ConnectionObject = con;
			parameters.SqlParameters[0].ParameterValue = "Provider Independent";
			this.previewControl1.PreviewLayoutChanged += delegate (object sender, EventArgs e)
			{
				this.RunProviderIndependent(reportName);
			};
			this.previewControl1.RunReport(reportName,parameters);
		}
		
		
		#endregion
		
		#region Contributors
		//
		/// <summary>
		/// Some values in the Datastructure are not set (means they are null), you can handle this values by setting
		/// the NullValue in the properties of this Item, or, you can use the SectionRenderingEvent as shown
		/// below
		/// </summary>
		/// <param name="fileName"></param>
		private void RunContributors (string fileName)
		{
			ReportModel model = ReportEngine.LoadReportModel(fileName);
			
			// sorting is done here, but, be carefull, misspelled fieldnames will cause an exception
			
			//ReportSettings settings = model.ReportSettings;
			//settings.SortColumnCollection.Add(new SortColumn("First",System.ComponentModel.ListSortDirection.Ascending));
		
			// Both variable declarations  are valid
			
			ContributorCollection contributorCollection = ContributorsReportData.CreateContributorsList();
			IDataManager dataManager = DataManager.CreateInstance(contributorCollection,model.ReportSettings);
			
//			List<Contributor> list = ContributorsReportData.CreateContributorsList();
//			IDataManager dm = DataManager.CreateInstance(list,model.ReportSettings);
			
			
			this.previewControl1.PreviewLayoutChanged += delegate (object sender, EventArgs e)
			{
				this.previewControl1.RunReport(model,dataManager);
			};
			
			this.previewControl1.RunReport(model,dataManager);
		}
		
		
		
		ImageList imageList;
		
		private void RunEventlogger (string fileName)
		{
			EventLogger eLog = new EventLogger(fileName);
			this.imageList = eLog.Images;
			
			ReportModel model = ReportEngine.LoadReportModel(fileName);
			IDataManager dataManager = DataManager.CreateInstance(eLog.EventLog,model.ReportSettings);
			
			this.previewControl1.SectionRendering += PushPrinting;
			
			this.previewControl1.PreviewLayoutChanged += delegate (object sender, EventArgs e)
		
			{
				this.previewControl1.RunReport(model,dataManager);
			};
			this.previewControl1.RunReport(model,dataManager);
		}
		
		
		//Handles  SectionRenderEvent
		
		private void PushPrinting (object sender, SectionRenderEventArgs e ) 
		{
			string sectionName = e.Section.Name;
			
			if (sectionName == ReportSectionNames.ReportHeader) {
				Console.WriteLine("xx  " + ReportSectionNames.ReportHeader);
			} 
			
			else if (sectionName == ReportSectionNames.ReportPageHeader) {
				Console.WriteLine("xx " +ReportSectionNames .ReportPageHeader);
			} 
			
			else if (sectionName == ReportSectionNames.ReportDetail){
				
				BaseDataItem item = e.Section.FindItem("EntryType") as BaseDataItem;
				if (item != null) {
					string s = item.DBValue;
					Image im = null;
					if (s == "Information") {
						im = this.imageList.Images[1];
					} else if (s == "Warning") {
						im = this.imageList.Images[2];
					} else if (s == "Error")
					{
						im = this.imageList.Images[0];
					}
					
					if (im != null)
					{
						BaseImageItem bi = e.Section.FindItem("BaseImageItem1") as BaseImageItem;
						if (bi != null) {
							bi.Image = im;
						}
						
					}
				}
			}
			
			else if (sectionName == ReportSectionNames.ReportPageFooter){
				Console.WriteLine("xx " + ReportSectionNames.ReportPageFooter);
			}
			
			else if (sectionName == ReportSectionNames.ReportFooter){
				Console.WriteLine("xx " + ReportSectionNames.ReportFooter);
			}
			
			else{
				throw new WrongSectionException(sectionName);
			}
		}
		
		
		#endregion
		

		
		void TreeView1MouseDoubleClick(object sender, MouseEventArgs e)
		{
			TreeNode selectedNode = this.treeView1.SelectedNode;
			if (selectedNode != null) {
				RunStandardReport(selectedNode.Tag.ToString());
			}
		}
		
		/*
		void Button2Click(object sender, EventArgs e)
		{
			// get Filename to save *.pdf
			string saveTo = this.SelectFilename();
			
			// Create connectionobject
			parameters =  ReportEngine.LoadParameters(reportName);
			ConnectionObject con = ConnectionObject.CreateInstance(this.conOleDbString,
			                                                       System.Data.Common.DbProviderFactories.GetFactory("System.Data.OleDb") );
			
			parameters.ConnectionObject = con;
			
			
			// create a Pagebuilder
			pageBuilder = ReportEngine.CreatePageBuilder(reportName,parameters);
			pageBuilder.BuildExportList();
		
			using (PdfRenderer pdfRenderer = PdfRenderer.CreateInstance(pageBuilder,saveTo,true)){
				pdfRenderer.Start();
				pdfRenderer.RenderOutput();
				pdfRenderer.End();
			}
		}
		
		
		private string SelectFilename()
		{
			using (SaveFileDialog saveDialog = new SaveFileDialog()){

				saveDialog.FileName = "_pdf";
				saveDialog.DefaultExt = "PDF";
				saveDialog.ValidateNames = true;
				if(saveDialog.ShowDialog() == DialogResult.OK){
					return saveDialog.FileName;
				} else {
					return String.Empty;
				}
			}
		}
		 */
	}
}
