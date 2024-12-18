using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FacesheetParser
{
    public partial class Import : Form
    {
        public string cxnString = ConfigurationManager.ConnectionStrings["FacesheetParser.Properties.Settings.dmdtestConnectionString"].ConnectionString;
        public string INFO = "INFO";
        public string ERROR = "ERROR";
        public string WARNING = "WARNING";
        public string txt = @"..\..\txt";
        public Import()
        {
            InitializeComponent();
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            Program.Mlog("Beginning to import file(s).");
            try
            {
                List<string> pdfPaths = new List<string>();
                var f = openFileDialog1;
                f.Multiselect = true;
                f.Filter = "All files (*.*)|*.*";
                f.FileName = string.Empty;
                if (f.ShowDialog() == DialogResult.OK)
                {
                    string tempFolder = Path.GetTempPath();

                    foreach (var fileName in f.FileNames)
                    {
                        pdfPaths.Add(fileName);
                    }
                }
                Cursor = Cursors.WaitCursor;
                foreach (var pdfPath in pdfPaths)
                {
                    var p = pdfPath.Split('\\').ToList();
                    var fileName = p[(p.Count - 1)];
                    fileName = fileName.Replace(".pdf", ".txt");
                    var txt_path = string.Format(@"..\..\txt\{0}", fileName);
                    List<List<string>> txt_list = new List<List<string>>();
                    using (var pdf = PdfDocument.Open(pdfPath))
                    {
                        foreach (var page in pdf.GetPages())
                        {
                            var text = ContentOrderTextExtractor.GetText(page);
                            text = text.Replace("\r\n", "~");
                            txt_list.Add(text.Split('~').ToList());
                        }
                    }

                    int counter = 1;
                    foreach (List<string> t in txt_list)
                    {
                        if (counter == 1)
                        {
                            File.WriteAllLines(txt_path, t);
                        }
                        else
                        {
                            File.AppendAllLines(txt_path, t);
                        }

                        counter++;

                    }
                }
                Program.Mlog("Import process complete.");
                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
        }
    }
}
