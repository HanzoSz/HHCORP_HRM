using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace HHCORP_HRM
{
    public partial class DuyetNghiPhepWindow : Window
    {
        private readonly string connectionString;
        private int MaNV;
        private string VaiTro;
        private bool isTuChoiMode = false;

        public DuyetNghiPhepWindow(int maNV, string vaiTro)
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
            VaiTro = vaiTro;

            Loaded += DuyetNghiPhepWindow_Loaded;
        }

        private void DuyetNghiPhepWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyPermissions();
            LoadNghiPhep();
        }

        private void ApplyPermissions()
        {
            if (VaiTro != "Tổng Giám đốc")
            {
                btnDuyet.IsEnabled = false;
                btnTuChoi.IsEnabled = false;
            }
        }

        private void LoadNghiPhep()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        SELECT MaNghiPhep, np.MaNV, nv.HoTen, NgayBatDau, NgayKetThuc, LyDo, TrangThai, LyDoTuChoi
                        FROM NghiPhep np
                        LEFT JOIN NhanVien nv ON np.MaNV = nv.MaNV
                        WHERE TrangThai = N'Chờ duyệt'";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Không có đơn nghỉ phép nào chờ duyệt!");
                    }
                    dgvNghiPhep.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message);
            }
        }

        private void btnDuyet_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNghiPhep.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn đơn nghỉ phép để duyệt!");
                return;
            }

            if (isTuChoiMode)
            {
                // Nếu đang ở chế độ từ chối, hủy chế độ đó và quay lại bình thường
                isTuChoiMode = false;
                spLyDoTuChoi.Visibility = Visibility.Collapsed;
                btnDuyet.Content = "Duyệt";
                btnTuChoi.Content = "Từ Chối";
                return;
            }

            DataRowView row = dgvNghiPhep.SelectedItem as DataRowView;
            int maNghiPhep = Convert.ToInt32(row["MaNghiPhep"]);
            int maNVNopDon = Convert.ToInt32(row["MaNV"]);

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE NghiPhep SET TrangThai = @TrangThai, MaNVPheDuyet = @MaNVPheDuyet, LyDoTuChoi = @LyDoTuChoi WHERE MaNghiPhep = @MaNghiPhep";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TrangThai", "Đã duyệt");
                    cmd.Parameters.AddWithValue("@MaNVPheDuyet", MaNV);
                    cmd.Parameters.AddWithValue("@LyDoTuChoi", DBNull.Value);
                    cmd.Parameters.AddWithValue("@MaNghiPhep", maNghiPhep);
                    cmd.ExecuteNonQuery();

                    // Ghi thông báo cho nhân viên
                    GhiThongBao(maNVNopDon, $"Đơn nghỉ phép của bạn (Mã: {maNghiPhep}) đã được duyệt.");

                    MessageBox.Show("Duyệt đơn thành công!");
                    LoadNghiPhep();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi duyệt đơn: " + ex.Message);
            }
        }

        private void btnTuChoi_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNghiPhep.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn đơn nghỉ phép để từ chối!");
                return;
            }

            if (!isTuChoiMode)
            {
                // Chuyển sang chế độ nhập lý do từ chối
                isTuChoiMode = true;
                spLyDoTuChoi.Visibility = Visibility.Visible;
                btnDuyet.Content = "Hủy chế độ từ chối";
                btnTuChoi.Content = "Xác nhận từ chối";
                return;
            }

            // Xác nhận từ chối
            if (string.IsNullOrWhiteSpace(txtLyDoTuChoi.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do từ chối!");
                return;
            }

            DataRowView row = dgvNghiPhep.SelectedItem as DataRowView;
            int maNghiPhep = Convert.ToInt32(row["MaNghiPhep"]);
            int maNVNopDon = Convert.ToInt32(row["MaNV"]);

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE NghiPhep SET TrangThai = @TrangThai, MaNVPheDuyet = @MaNVPheDuyet, LyDoTuChoi = @LyDoTuChoi WHERE MaNghiPhep = @MaNghiPhep";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TrangThai", "Từ chối");
                    cmd.Parameters.AddWithValue("@MaNVPheDuyet", MaNV);
                    cmd.Parameters.AddWithValue("@LyDoTuChoi", txtLyDoTuChoi.Text);
                    cmd.Parameters.AddWithValue("@MaNghiPhep", maNghiPhep);
                    cmd.ExecuteNonQuery();

                    // Ghi thông báo cho nhân viên
                    GhiThongBao(maNVNopDon, $"Đơn nghỉ phép của bạn (Mã: {maNghiPhep}) đã bị từ chối. Lý do: {txtLyDoTuChoi.Text}");

                    MessageBox.Show("Từ chối đơn thành công!");
                    isTuChoiMode = false;
                    spLyDoTuChoi.Visibility = Visibility.Collapsed;
                    btnDuyet.Content = "Duyệt";
                    btnTuChoi.Content = "Từ Chối";
                    txtLyDoTuChoi.Text = "";
                    LoadNghiPhep();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi từ chối đơn: " + ex.Message);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void GhiThongBao(int maNV, string noiDung)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO ThongBao (MaNV, NoiDung, NgayTao, DaXem) VALUES (@MaNV, @NoiDung, @NgayTao, @DaXem)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNV", maNV);
                    cmd.Parameters.AddWithValue("@NoiDung", noiDung);
                    cmd.Parameters.AddWithValue("@NgayTao", DateTime.Now);
                    cmd.Parameters.AddWithValue("@DaXem", 0);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi ghi thông báo: " + ex.Message);
            }
        }
    }
}