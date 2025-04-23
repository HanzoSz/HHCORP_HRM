using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Linq;

namespace HHCORP_HRM
{
    public partial class XuatBaoCaoWindow : Window
    {
        private readonly string connectionString;
        private readonly int MaNV;

        public XuatBaoCaoWindow(int maNV)
        {
            InitializeComponent();

            var connectionStringSettings = ConfigurationManager.ConnectionStrings["QuanLyNhanSu"];
            if (connectionStringSettings == null || string.IsNullOrEmpty(connectionStringSettings.ConnectionString))
            {
                MessageBox.Show("Chuỗi kết nối 'QuanLyNhanSu' không được tìm thấy trong App.config! Vui lòng kiểm tra file cấu hình.");
                Close();
                return;
            }
            connectionString = connectionStringSettings.ConnectionString;

            MaNV = maNV;
            Loaded += XuatBaoCaoWindow_Loaded;
        }

        private void XuatBaoCaoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadNhanVien();
        }

        private void LoadNhanVien()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaNV, HoTen FROM NhanVien";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    cbNhanVien.ItemsSource = dt.DefaultView;
                    cbNhanVien.DisplayMemberPath = "HoTen";
                    cbNhanVien.SelectedValuePath = "MaNV";
                    cbNhanVien.SelectedIndex = dt.Rows.Cast<DataRow>().ToList().FindIndex(row => Convert.ToInt32(row["MaNV"]) == MaNV);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách nhân viên: " + ex.Message);
            }
        }

        private void btnXuatBaoCao_Click(object sender, RoutedEventArgs e)
        {
            if (cbNhanVien.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn nhân viên!");
                return;
            }

            if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null)
            {
                MessageBox.Show("Vui lòng chọn ngày bắt đầu và ngày kết thúc!");
                return;
            }

            DateTime ngayBatDau = dpNgayBatDau.SelectedDate.Value;
            DateTime ngayKetThuc = dpNgayKetThuc.SelectedDate.Value;
            int selectedMaNV = Convert.ToInt32(cbNhanVien.SelectedValue);

            if (ngayKetThuc < ngayBatDau)
            {
                MessageBox.Show("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu!");
                return;
            }

            try
            {
                DataTable dt = new DataTable();
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT MaChamCong, MaNV, NgayChamCong, GioVao, GioRa, GhiChu
                        FROM ChamCong
                        WHERE MaNV = @MaNV AND NgayChamCong BETWEEN @NgayBatDau AND @NgayKetThuc";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNV", selectedMaNV);
                    cmd.Parameters.AddWithValue("@NgayBatDau", ngayBatDau);
                    cmd.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu chấm công trong khoảng thời gian đã chọn!");
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"BaoCaoChamCong_MaNV_{selectedMaNV}_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    StringBuilder sb = new StringBuilder();
                    // Thêm tiêu đề cột
                    string[] columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
                    sb.AppendLine(string.Join(",", columnNames));

                    // Thêm dữ liệu
                    foreach (DataRow row in dt.Rows)
                    {
                        string[] fields = row.ItemArray.Select(field => field.ToString()).ToArray();
                        sb.AppendLine(string.Join(",", fields));
                    }

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString());
                    MessageBox.Show("Xuất báo cáo thành công!", "Thông báo");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất báo cáo: " + ex.Message);
            }
        }
    }
}