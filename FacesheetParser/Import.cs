using CsvHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FacesheetParser
{
    public class Patient
    {
        public string FileName { get; set; }
        public string Name { get; set; }
        public string Room { get; set; }
        public string SSN { get; set; }
        public string DOB { get; set; }
        public string Sex { get; set; }
        public string MedicaidNo { get; set; }
        public bool HasDementiaIndicator { get; set; }
        public List<Contact> Contacts { get; set; }
    }
    public class Contact
    {
        public string Relationship { get; set; }
        public string Name { get; set; }
        public string Responsibilities { get; set; }
        public string CallOrder { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
    }
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

        private void Import_Load(object sender, EventArgs e)
        {
            Program.Mlog("Running Import_Load tasks.");
            try
            {
                Setup();
                Program.Mlog("Import_Load tasks complete.");
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
        }

        private void Setup()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(txt);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
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
                    WriteToDb();
                }
                Program.Mlog("Import process complete.");
                Cursor = Cursors.Default;
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
        }

        private void WriteToDb()
        {
            Program.Mlog("Beginning to write to the database.");
            try
            {
                List<List<Patient>> records = new List<List<Patient>>();
                foreach (string file in Directory.EnumerateFiles(txt, "*.txt"))
                {
                    var content = File.ReadAllLines(file).ToList();
                    var data = ParseData(content, file);
                    records.Add(data);
                }
                foreach (var record in records)
                {
                        using (var writer = new StreamWriter(@"..\..\patients.csv"))
                        {
                            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                            {
                                csv.WriteRecords(record);
                            }
                        }

                    //string sql = string.Empty;
                    //using (SqlConnection conn = new SqlConnection(cxnString))
                    //{
                    //    conn.Open();
                    //    using (SqlCommand command = new SqlCommand())
                    //    {
                    //        command.Connection = conn;
                    //        command.CommandType = CommandType.Text;
                    //        sql = string.Empty;
                    //        sql = "INSERT INTO EOB_stage_DentaQuestFFSS (file_name, physician, total_submitted_amount, total_patient_pay, total_writeoff, total_plan_pay, claim_count) ";
                    //        sql += "VALUES (@file_name, @physician, @total_submitted_amount, @total_patient_pay, @total_writeoff, @total_plan_pay, @claim_count)";
                    //        command.CommandText = sql;
                    //        command.Parameters.AddWithValue("@file_name", record["file_name"]);
                    //        command.Parameters.AddWithValue("@physician", record["physician"]);
                    //        command.Parameters.AddWithValue("@total_submitted_amount", record["total_submitted_amount"]);
                    //        command.Parameters.AddWithValue("@total_patient_pay", record["total_patient_pay"]);
                    //        command.Parameters.AddWithValue("@total_writeoff", record["total_writeoff"]);
                    //        command.Parameters.AddWithValue("@total_plan_pay", record["total_plan_pay"]);
                    //        command.Parameters.AddWithValue("@claim_count", record["claim_count"]);
                    //        command.ExecuteNonQuery();
                    //    }
                    //}
                }
                Program.Mlog("Writing to the database complete.");
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
        }

        private List<Patient> ParseData(List<string> txt, string file)
        {
            Program.Mlog("Beginning to parse data.");
            List<Patient> patients = new List<Patient>();
            try
            {
                string file_name = string.Empty;

                // file_name logic
                var p = file.Split('\\').ToList();
                var fileName = p[(p.Count - 1)];
                file_name = fileName.Replace(".txt", string.Empty);

                int currPage = 0;
                int maxPage = -1;
                bool processed = false;
                Patient patient = new Patient();

                foreach (var t in txt)
                {
                    if (t.Contains("Page ") && t.Contains(" of "))
                    {
                        string s = t.Substring(t.IndexOf("Page "), 11);
                        string cp = s.Substring(5, 1);
                        string mp = s.Substring(10, 1);
                        currPage = int.Parse(cp);
                        maxPage = int.Parse(mp);
                    }
                    if (currPage == 1 && !processed)
                    {
                        patient = new Patient();
                        patient = ParsePatient(txt, file_name);
                        if (patient.Name != string.Empty && !patients.Contains(patient))
                        {
                            patients.Add(patient);
                        }
                        processed = true;
                    }
                    if (currPage == maxPage)
                    {
                        int txtCount = txt.Count;
                        int start = txt.IndexOf(t) + 1;
                        int end = txtCount - start;

                        if (end > 0)
                        {
                            txt = txt.GetRange(start, end);

                            currPage = 1;
                            maxPage = -1;
                            processed = false;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Program.Mlog("Parsing complete.");
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }

            return patients;
        }

        private Patient ParsePatient(List<string> txt, string fileName)
        {
            Patient patient = new Patient();
            try
            {
                patient.FileName = fileName;

                var name = txt[0];
                name = name.Replace("Resident Face Sheet:  ", string.Empty);
                name = name.Substring(0, name.IndexOf(" (")).Trim();
                patient.Name = name;

                var room = txt[txt.IndexOf("Unit: ") + 1];
                patient.Room = room;

                var ssn = txt[txt.IndexOf("Primary Payer: ") - 1];
                ssn = ssn.Replace("SSN:  ", string.Empty);
                patient.SSN = ssn;

                var dob = txt[txt.IndexOf("Birth Date: ") + 2];
                patient.DOB = dob;

                var sex = txt[txt.IndexOf("Birth Date: ") + 1];
                patient.Sex = sex;

                var medicaidno = txt[txt.IndexOf("Mother's Maiden Name:") + 1];
                patient.MedicaidNo = medicaidno;

                int diagnosesStart = txt.IndexOf("Intolerances:") + 1;
                int diagnosesEnd = txt.IndexOf("Alerts: ");
                int range = diagnosesEnd - diagnosesStart;

                var diagnoses = txt.GetRange(diagnosesStart, range);
                foreach (var d in diagnoses)
                {
                    if (d.ToUpper().Contains("DEMENTIA"))
                    {
                        patient.HasDementiaIndicator = true;
                        break;
                    }
                }

                //patient.Contacts = ParseContacts();
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
            return patient;
        }

        private List<Contact> ParseContacts(List<string> contactsText)
        {
            List<Contact> contacts = new List<Contact>();
            try
            {
                Contact contact = new Contact();
                contacts.Add(contact);
            }
            catch (Exception ex)
            {
                Program.Mlog(ex.ToString(), ERROR);
            }
            return contacts;
        }
    }
}
