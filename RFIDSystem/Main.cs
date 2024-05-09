﻿using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.VariantTypes;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using ExcelDataReader;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using Color = System.Drawing.Color;

namespace RFIDSystem
{
    public partial class Main : Form
    {
        static SerialPort RFIDPort;
        DataTableCollection datacollection;
        bool IsPort = false;
        public static int subscription = 0;
        public static string subscriptiondate = "";

        public static List<string> AditionalActiveValues = new List<string>();

        //PostgreSQL
        public static string connectionString = "";
        public static NpgsqlConnection conn = new NpgsqlConnection(connectionString);
        public static NpgsqlDataAdapter da = new NpgsqlDataAdapter();
        public static DataSet ds;
        public static DataTable refreshDtReg = new DataTable();
        public static DataTable refreshDtAct = new DataTable();
        public static DataTable newUser = new DataTable();

        public Main()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            bool Do = false;

            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Excel Workbook |*xlsx|Excel workbook |*xls";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var stream = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read);
                using (IExcelDataReader ExReader = ExcelReaderFactory.CreateReader(stream))
                {
                    DataSet result = ExReader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    });
                    datacollection = result.Tables;
                    Do = true;
                }
                
            }
            if(Do)
            {
                DataTable Act = datacollection["Aktivní"];
                dataActive.DataSource = Act;

                DataTable Reg = datacollection["Registrovaní"];
                dataRegistered.DataSource = Reg;
            }

            
        }

        private void Main_Load(object sender, EventArgs e)
        {
            try
            {
                if (Properties.Settings.Default["PgIp"].ToString() == "")
                {
                    Setup f = new Setup();
                    f.ShowDialog();
                    this.Close();
                }
                else
                {
                    if (Properties.Settings.Default["ActiveTableAditional"] != null)
                    {
                        if (!Properties.Settings.Default["ActiveTableAditional"].ToString().Contains(';'))
                        {
                            AditionalActiveValues.Add(Properties.Settings.Default["ActiveTableAditional"].ToString());
                        }
                        else
                        {
                            AditionalActiveValues = Properties.Settings.Default["ActiveTableAditional"].ToString().Split(';').ToList();
                        }

                    }
                    connectionString = "Server=" + Properties.Settings.Default["PgIp"] + ";Port=" + Properties.Settings.Default["PgPort"] + ";Database=" + Properties.Settings.Default["PgDatabase"] + ";User Id=" + Properties.Settings.Default["PgUsername"] + ";Password=" + Properties.Settings.Default["PgPassword"] + ";";
                    Tick.Start();
                    conn.Open();
                    Refresh(ref dataRegistered, "PgTableRegistered");
                    Refresh(ref dataActive, "PgTableActive");
                    foreach (DataGridViewColumn d in dataActive.Columns)
                    {
                        if (d.HeaderText != "Aktivní")
                        {
                            d.ReadOnly = true;
                        }
                        else
                        {
                            d.ReadOnly = false;
                        }
                    }
                    timerrefreshDB.Start();
                    txtRFIDOut.Text = Properties.Settings.Default["PgTableActive"] + " " + Properties.Settings.Default["PgTableRegistered"];

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"RFIDystem Login to database", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            
        }

        public static String Translate(String word, string toLang, string fromLang)
        {
            var toLanguage = toLang.ToLower();
            var fromLanguage = fromLang.ToLower();
            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";
            var webClient = new WebClient
            {
                Encoding = System.Text.Encoding.UTF8
            };
            var result = webClient.DownloadString(url);
            try
            {
                result = result.Substring(4, result.IndexOf("\"", 4, StringComparison.Ordinal) - 4);
                return result;
            }
            catch
            {
                return "Error";

            }
        }
        public void Refresh(ref DataGridView d, string settTable)
        {
            d.DataSource = GetData("SELECT * FROM " + Properties.Settings.Default[settTable]);
                
        }
        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            RFIDPort = new SerialPort();
            RFIDPort.PortName = comboBox1.Text;//Set your board COM
            RFIDPort.BaudRate = 9600;
            RFIDPort.Open();
            comboBox1.Enabled = false;
            IsPort = true;
            
        }

        private void Tick_Tick(object sender, EventArgs e)
        {
            int RowIndex = 0;
            string name = null;
            string email = null;
            /*if (IsPort)
            {
                string a = RFIDPort.ReadExisting();
                if(a != "")
                {
                    txtRFIDOut.Text = txtRFIDOut.Text + a + "" +
                        "";
                    txtRFIDOut.SelectionStart = txtRFIDOut.Text.Length;
                    txtRFIDOut.ScrollToCaret();

                    dataRegistered.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                    lblTag.Text = a;

                    bool valueResult = false;
                    foreach (DataGridViewRow row in dataRegistered.Rows)
                    {
                        if (row.Cells[0].Value != null)
                        {
                            RowIndex++;
                            txtRFIDOut.Text = txtRFIDOut.Text + " 2 ";
                            DataGridViewCell cell = row.Cells[0];
                            string val = cell.Value.ToString();
                            lblChecking.Text = val;
                            if (lblTag.Text.Contains(lblChecking.Text))
                            {
                                if (row.Cells[1].Value != null)
                                {
                                    DataGridViewCell cell1 = row.Cells[1];
                                    string val1 = cell1.Value.ToString();
                                    name = val1;

                                }
                                if (row.Cells[2].Value != null)
                                {
                                    DataGridViewCell cell2 = row.Cells[2];
                                    string val2 = cell2.Value.ToString();
                                    email = val2;

                                }
                                valueResult = true;
                                break;
                            }
                        }


                    }
                    if (!valueResult)
                    {
                        MessageBox.Show("Nelze najít " + a , "Nenalezeno");
                        return;
                    }
                    else
                    {
                        Form form = new User(true,lblChecking.Text,name,email);
                        form.Show();
                    }
                }

                
            }*/

        }


        public void AddUser(String ID, String Jmeno,String Prijmeni, String Email, String TelCislo, String Datum)
        {
            string comm = "INSERT INTO " + Properties.Settings.Default["PgTableRegistered"] + " VALUES('" + ID + "','" + Jmeno + "','" + Prijmeni + "','" + Email + "','" + TelCislo + "','" + Datum + "')";
            MessageBox.Show(comm);
            try
            {
                NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(comm, conn))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex) { txtRFIDOut.Text = ex.Message; }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + Translate(ex.Message.ToString(), "cs", "en"), "RFIDSystem");
            }
            
        }

        /*public void AddActiveUser(int ID, string Val1, string Val2 = null, string Val3 = null)
        {
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using (NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO " + Properties.Settings.Default["PgTableRegistered"] + " VALUES ('" + ID + "','" + Jmeno + "','" + Prijmeni + "','" + Email + "','" + TelCislo + "','" + Datum + "','" + Heslo + "')", conn))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) { txtRFIDOut.Text = ex.Message; }
            }
        */

        private void button1_Click(object sender, EventArgs e)
        {
            Form f = new Setup();
            f.ShowDialog();
        }
        public DataTable GetData(string selectSql)
        {
            try
            {
                ds = new DataSet();
                da = new NpgsqlDataAdapter(selectSql, conn);
                da.Fill(ds);
                return ds.Tables[0];
            }
            finally
            {
                conn.Close();
            }
        }

        private void btnAddNewuser_Click(object sender, EventArgs e)
        {
            User u = new User(false,null,null,null);
            u.Show();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            Refresh(ref dataRegistered, "PgTableRegistered");
            Refresh(ref dataActive, "PgTableActive");
        }


        private void dataRegistered_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            MessageBox.Show("Error appeared in Data Grid View. More info : "+ e.Exception.Message.ToString() + "\n" + Translate(e.Exception.Message.ToString(), "cs", "en"), "RFIDSystem - DataGridView", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool AreTablesTheSame(DataTable tbl1, DataTable tbl2)
        {
            if (tbl1.Rows.Count != tbl2.Rows.Count || tbl1.Columns.Count != tbl2.Columns.Count)
                return false;


            for (int i = 0; i < tbl1.Rows.Count; i++)
            {
                for (int c = 0; c < tbl1.Columns.Count; c++)
                {
                    if (!Equals(tbl1.Rows[i][c], tbl2.Rows[i][c]))
                        return false;
                }
            }
            return true;
        }


        public static List<string>Values = new List<string>();
        private void timerrefreshDB_Tick(object sender, EventArgs e)
        {
            if(refreshDtReg != GetData("SELECT * FROM " + Properties.Settings.Default["PgTableRegistered"]))
            {
                Refresh(ref dataRegistered, "PgTableRegistered");
                refreshDtReg = GetData("SELECT * FROM " + Properties.Settings.Default["PgTableRegistered"]);
            }

            if (refreshDtAct != GetData("SELECT * FROM " + Properties.Settings.Default["PgTableActive"]))
            {
                Refresh(ref dataActive, "PgTableActive");
                refreshDtAct = GetData("SELECT * FROM " + Properties.Settings.Default["PgTableActive"]);
            }

            if (newUser != GetData("SELECT * FROM readertemp"))
            {
                newUser = GetData("SELECT * FROM readertemp");
                if(newUser.Rows.Count != 0)
                {
                    string comm = "DELETE FROM readertemp WHERE \"Id\"='" + newUser.Rows[0][0].ToString() + "'";
                    try
                    {
                        NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                        conn.Open();
                        using (NpgsqlCommand cmd = new NpgsqlCommand(comm, conn))
                        {
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex) { txtRFIDOut.Text = ex.Message; }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + "\n" + Translate(ex.Message.ToString(), "cs", "en"), "RFIDSystem");
                    }

                    bool Exists = false;
                    int RowNum = 0;
                    for (int i = 0; i < dataRegistered.Rows.Count - 1; i++)
                    {
                        if(dataRegistered.Rows[i].Cells["Id"].Value.ToString() == newUser.Rows[0][0].ToString())
                        {
                            RowNum = i;
                            Exists = true;
                            break;
                        }
                    }

                    if (!Exists)
                    {
                        User user = new User(true, newUser.Rows[0][0].ToString());
                        user.Show();
                    }
                    else
                    {
                        for (int i = 0;i< AditionalActiveValues.Count; i++)
                        {
                            Value v = new Value(AditionalActiveValues[i]);
                            v.Show();
                            while(v.Val == "")
                            {
                                wait(500);
                            }
                        }

                        string command = "INSERT INTO " + Properties.Settings.Default["PgTableActive"] + " VALUES('" + dataRegistered.Rows[RowNum].Cells["Id"].Value.ToString() + "','" + dataRegistered.Rows[RowNum].Cells["Jméno"].Value.ToString() + "','" + dataRegistered.Rows[RowNum].Cells["Příjmení"].Value.ToString() + "','" + dataRegistered.Rows[RowNum].Cells["Email"].Value.ToString() + "','" + dataRegistered.Rows[RowNum].Cells["Telefonní číslo"].Value.ToString() + "','" + dataRegistered.Rows[RowNum].Cells["Datum narození"].Value.ToString() + "', 'true'";
                        if (AditionalActiveValues.Count == 0)
                        {

                        }
                        else
                        {
                            for (int i = 0; i < AditionalActiveValues.Count(); i++)
                            {
                                if (i == 0)
                                {
                                    command = command + ", ";
                                }
                                if (i == AditionalActiveValues.Count() - 1)
                                {
                                    command = command += "'" +Values[i] + "')";
                                }
                                else
                                {
                                    command = command += "'" + Values[i] + "',";
                                }
                            }
                        }
                        Values = new List<string>();
                        MessageBox.Show(command );
                        try
                        {
                            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
                            conn.Open();
                            using (NpgsqlCommand cmd = new NpgsqlCommand(command, conn))
                            {
                                try
                                {
                                    cmd.ExecuteNonQuery();
                                }
                                catch (Exception ex) { MessageBox.Show(ex.Message + "\n" + Translate(ex.Message.ToString(), "cs", "en"), "RFIDSystem"); }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message + "\n" + Translate(ex.Message.ToString(), "cs", "en"), "RFIDSystem");
                        }
                    }
                }
            }

        }

        private void dataRegistered_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                MenuItem cut = new MenuItem("Cut");
                MenuItem copy = new MenuItem("Copy");
                MenuItem paste = new MenuItem("Paste");
                MenuItem delete = new MenuItem("Delete Row");

                ContextMenu m = new ContextMenu();
                m.MenuItems.Add(cut);
                m.MenuItems.Add(copy);
                m.MenuItems.Add(paste);
                m.MenuItems.Add(delete);
                m.Show(dataRegistered, new Point(e.X, e.Y));

            }
        }

        private void dataActive_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            timerrefreshDB.Stop();
            string comm = "UPDATE " + Properties.Settings.Default["PgTableActive"] + " SET \"" + dataActive.Columns[e.ColumnIndex].HeaderText + "\"='" + dataActive.Rows[e.RowIndex].Cells[e.ColumnIndex].Value + "' WHERE \"Jméno\"='" + dataActive.Rows[e.RowIndex].Cells["Jméno"].Value + "' AND \"Příjmení\"='" + dataActive.Rows[e.RowIndex].Cells["Příjmení"].Value + "'";
            NpgsqlConnection conn = new NpgsqlConnection(connectionString);
            conn.Open();
            DataGridViewRow row = dataRegistered.Rows[dataRegistered.Rows.Count - 1];
            MessageBox.Show(comm);
            using (NpgsqlCommand cmd = new NpgsqlCommand(comm, conn))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex) { txtRFIDOut.Text = ex.Message; }
            }
            timerrefreshDB.Start();
        }

        public void wait(int milliseconds)
        {
            var timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0) return;

            // Console.WriteLine("start wait timer");
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();

            timer1.Tick += (s, e) =>
            {
                timer1.Enabled = false;
                timer1.Stop();
                // Console.WriteLine("stop wait timer");
            };

            while (timer1.Enabled)
            {
                Application.DoEvents();
            }
        }

    }
}