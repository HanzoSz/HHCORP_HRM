using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows;
using System.Data;

namespace HHCORP_HRM
{
    public partial class XinNghiPhepWindow : Window
    {
        private readonly string connectionString;
        private int MaNV;
        private string HoTen;
        private string VaiTro;

        public XinNghiPhepWindow(int maNV, string hoTen, string vaiTro)
        {
            InitializeComponent();

            // Đọc chuỗi kết nối từ App.config
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["QuanLyNhanSu"];
            if (connectionStringSettings == null || string.IsNullOrEmpty(connectionStringSettings.ConnectionString))
            {
                MessageBox.Show("Chuỗi kết nối 'QuanLyNhanSu' không được tìm thấy trong App.config! Vui lòng kiểm tra file cấu hình.");
                Close();
            }
            connectionString = connectionStringSettings.ConnectionString;

            MaNV = maNV;
            HoTen = hoTen;
            VaiTro = vaiTro;

            // Hiển thị thông tin nhân viên đăng nhập
            txtMaNV.Text = MaNV.ToString();
            txtHoTen.Text = HoTen;

            Loaded += XinNghiPhepWindow_Loaded;
        }

        private void XinNghiPhepWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Phân quyền dựa trên VaiTro
            ApplyPermissions();
        }

        private void ApplyPermissions()
        {
            switch (VaiTro)
            {
                case "Tổng Giám đốc":
                    // Có quyền gửi đơn cho bất kỳ nhân viên nào
                    txtMaNV.IsEnabled = true;
                    txtHoTen.IsEnabled = true;
                    LoadMaNhanVien(); // Tải danh sách MaNV vào ComboBox
                    break;

                case "Kế toán":
                case "Nhân viên":
                    // Chỉ gửi đơn cho chính mình
                    txtMaNV.IsEnabled = false;
                    txtHoTen.IsEnabled = false;
                    break;

                default:
                    MessageBox.Show("Vai trò không hợp lệ!");
                    Close();
                    break;
            }
        }

        private void LoadMaNhanVien()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaNV, HoTen FROM NhanVien";
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Không có dữ liệu trong bảng NhanVien!");
                        return;
                    }
                    cbMaNV.ItemsSource = dt.DefaultView;
                    cbMaNV.DisplayMemberPath = "MaNV";
                    cbMaNV.SelectedValuePath = "MaNV";
                    cbMaNV.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải mã nhân viên: " + ex.Message);
            }
        }

        private void cbMaNV_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cbMaNV.SelectedItem == null)
            {
                txtHoTen.Text = string.Empty;
                return;
            }

            try
            {
                DataRowView row = cbMaNV.SelectedItem as DataRowView;
                txtMaNV.Text = row["MaNV"].ToString();
                txtHoTen.Text = row["HoTen"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lấy thông tin nhân viên: " + ex.Message);
            }
        }

        private void btnGuiDon_Click(object sender, RoutedEventArgs e)
        {
            if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null || string.IsNullOrWhiteSpace(txtLyDo.Text))
            {
                MessageBox.Show("Vui lòng điền đầy đủ các trường bắt buộc: Ngày Bắt Đầu, Ngày Kết Thúc, Lý Do!");
                return;
            }

            try
            {
                int selectedMaNV = VaiTro == "Tổng Giám đốc" && cbMaNV.SelectedValue != null ? Convert.ToInt32(cbMaNV.SelectedValue) : MaNV;

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO NghiPhep (MaNV, NgayBatDau, NgayKetThuc, LyDo, TrangThai) VALUES (@MaNV, @NgayBatDau, @NgayKetThuc, @LyDo, @TrangThai)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNV", selectedMaNV);
                    cmd.Parameters.AddWithValue("@NgayBatDau", dpNgayBatDau.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@NgayKetThuc", dpNgayKetThuc.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@LyDo", txtLyDo.Text);
                    cmd.Parameters.AddWithValue("@TrangThai", "Chờ duyệt");
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Gửi đơn xin nghỉ phép thành công!");
                    ClearInputs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi gửi đơn: " + ex.Message);
            }
        }

        private void btnHuy_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClearInputs()
        {
            if (VaiTro == "Tổng Giám đốc")
            {
                cbMaNV.SelectedIndex = -1;
            }
            dpNgayBatDau.SelectedDate = null;
            dpNgayKetThuc.SelectedDate = null;
            txtLyDo.Text = "";
        }
    }
}