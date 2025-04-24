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
            cbLoaiBaoCao.SelectedIndex = 0; // Mặc định chọn "Chấm công"
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

        private void cbLoaiBaoCao_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbLoaiBaoCao.SelectedItem == null) return;

            string loaiBaoCao = (cbLoaiBaoCao.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (loaiBaoCao == "Nhân sự")
            {
                // Báo cáo nhân sự không cần khoảng thời gian
                spNgayBatDau.Visibility = Visibility.Collapsed;
                spNgayKetThuc.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Báo cáo chấm công và lương cần khoảng thời gian
                spNgayBatDau.Visibility = Visibility.Visible;
                spNgayKetThuc.Visibility = Visibility.Visible;
            }
        }

        private void btnXuatBaoCao_Click(object sender, RoutedEventArgs e)
        {
            if (cbNhanVien.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn nhân viên!");
                return;
            }

            if (cbLoaiBaoCao.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn loại báo cáo!");
                return;
            }

            string loaiBaoCao = (cbLoaiBaoCao.SelectedItem as ComboBoxItem)?.Content.ToString();
            int selectedMaNV = Convert.ToInt32(cbNhanVien.SelectedValue);

            // Kiểm tra khoảng thời gian nếu cần
            DateTime? ngayBatDau = null;
            DateTime? ngayKetThuc = null;
            if (loaiBaoCao != "Nhân sự")
            {
                if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null)
                {
                    MessageBox.Show("Vui lòng chọn ngày bắt đầu và ngày kết thúc!");
                    return;
                }

                ngayBatDau = dpNgayBatDau.SelectedDate.Value;
                ngayKetThuc = dpNgayKetThuc.SelectedDate.Value;

                if (ngayKetThuc < ngayBatDau)
                {
                    MessageBox.Show("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu!");
                    return;
                }
            }

            try
            {
                // Bước 1: Lấy dữ liệu tùy theo loại báo cáo
                DataTable dt = new DataTable();
                string query = "";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    if (loaiBaoCao == "Chấm công")
                    {
                        query = @"
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
                    else if (loaiBaoCao == "Nhân sự")
                    {
                        query = @"
                            SELECT MaNV, HoTen, NgaySinh, GioiTinh, DiaChi, SoDienThoai, Email
                            FROM NhanVien
                            WHERE MaNV = @MaNV";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@MaNV", selectedMaNV);
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(dt);
                    }
                    else if (loaiBaoCao == "Lương")
                    {
                        query = @"
                            SELECT MaNV, Thang, Nam, LuongCoBan, Thuong, TongLuong
                            FROM Luong
                            WHERE MaNV = @MaNV AND NgayTinhLuong BETWEEN @NgayBatDau AND @NgayKetThuc";
                        SqlCommand cmd = new SqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@MaNV", selectedMaNV);
                        cmd.Parameters.AddWithValue("@NgayBatDau", ngayBatDau);
                        cmd.Parameters.AddWithValue("@NgayKetThuc", ngayKetThuc);
                        SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                        adapter.Fill(dt);
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show($"Không có dữ liệu {loaiBaoCao.ToLower()} trong khoảng thời gian đã chọn!");
                    return;
                }

                // Bước 2: Hiển thị dialog để chọn đường dẫn lưu file
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = "csv",
                    FileName = $"BaoCao_{loaiBaoCao}_MaNV_{selectedMaNV}_{DateTime.Now:yyyyMMdd}.csv"
                };

                if (saveFileDialog.ShowDialog() != true)
                {
                    return; // Người dùng hủy việc chọn file
                }

                string filePath = saveFileDialog.FileName;

                // Bước 3: Lưu thông tin báo cáo vào bảng BaoCao
                int maBaoCao;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string insertQuery = @"
                        INSERT INTO BaoCao (LoaiBaoCao, NgayTao, DuongDan, MaNVTao)
                        OUTPUT INSERTED.MaBaoCao
                        VALUES (@LoaiBaoCao, @NgayTao, @DuongDan, @MaNVTao)";
                    SqlCommand insertCmd = new SqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@LoaiBaoCao", loaiBaoCao);
                    insertCmd.Parameters.AddWithValue("@NgayTao", DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@DuongDan", filePath);
                    insertCmd.Parameters.AddWithValue("@MaNVTao", MaNV);
                    maBaoCao = (int)insertCmd.ExecuteScalar();
                }

                // Bước 4: Xuất dữ liệu ra file CSV
                StringBuilder sb = new StringBuilder();
                // Thêm tiêu đề cột
                string[] columnNames = dt.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToArray();
                sb.AppendLine(string.Join(",", columnNames));

                // Thêm dữ liệu
                foreach (DataRow row in dt.Rows)
                {
                    string[] fields = row.ItemArray.Select(field => $"\"{field.ToString().Replace("\"", "\"\"")}\"").ToArray();
                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(filePath, sb.ToString());
                MessageBox.Show($"Xuất báo cáo thành công! Báo cáo đã được lưu vào bảng BaoCao với MaBaoCao = {maBaoCao}", "Thông báo");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xuất báo cáo: " + ex.Message);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}