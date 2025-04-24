using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace HHCORP_HRM
{
    public partial class MainWindow : Window
    {
        private readonly string connectionString;
        public UserInfo CurrentUser { get; set; }

        public MainWindow(UserInfo currentUser)
        {
            InitializeComponent();

            // Đọc chuỗi kết nối từ App.config
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["QuanLyNhanSu"];
            if (connectionStringSettings == null || string.IsNullOrEmpty(connectionStringSettings.ConnectionString))
            {
                MessageBox.Show("Chuỗi kết nối 'QuanLyNhanSu' không được tìm thấy trong App.config! Vui lòng kiểm tra file cấu hình.");
                Close();
                return;
            }
            connectionString = connectionStringSettings.ConnectionString;

            CurrentUser = currentUser;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Không có thông tin người dùng! Vui lòng đăng nhập lại.");
                Close();
                return;
            }

            // Hiển thị tiêu đề với thông tin người dùng
            Title = $"HHCORP HRM - Xin chào, {CurrentUser.HoTen} ({CurrentUser.VaiTro})";

            // Hiển thị thông tin người dùng trên giao diện
            txtUserInfo.Text = $"Xin chào, {CurrentUser.HoTen} ({CurrentUser.VaiTro})";

            // Phân quyền giao diện
            ApplyPermissions();

            // Hiển thị thông báo cho nhân viên
            if (CurrentUser.VaiTro == "Nhân viên")
            {
                HienThiThongBao(CurrentUser.MaNV);
            }
        }

        private void ApplyPermissions()
        {
            // Ẩn tất cả các nút trước, sau đó hiển thị theo vai trò
            btnQuanLyNhanVien.Visibility = Visibility.Collapsed;
            btnXinNghiPhep.Visibility = Visibility.Collapsed;
            btnDuyetNghiPhep.Visibility = Visibility.Collapsed;
            btnChamCong.Visibility = Visibility.Collapsed;
            btnXuatBaoCao.Visibility = Visibility.Collapsed;
            btnDangXuat.Visibility = Visibility.Visible; // Nút Đăng Xuất luôn hiển thị

            switch (CurrentUser.VaiTro)
            {
                case "Tổng Giám đốc":
                    // Hiển thị tất cả các nút
                    btnQuanLyNhanVien.Visibility = Visibility.Visible;
                    btnXinNghiPhep.Visibility = Visibility.Visible;
                    btnDuyetNghiPhep.Visibility = Visibility.Visible;
                    btnChamCong.Visibility = Visibility.Visible;
                    btnXuatBaoCao.Visibility = Visibility.Visible;
                    break;

                case "Kế toán":
                    // Hiển thị các nút: Xin Nghỉ Phép, Chấm Công, Xuất Báo Cáo
                    btnXinNghiPhep.Visibility = Visibility.Visible;
                    btnChamCong.Visibility = Visibility.Visible;
                    btnXuatBaoCao.Visibility = Visibility.Visible;
                    btnQuanLyNhanVien.Visibility = Visibility.Visible;
                    break;

                case "Nhân viên":
                    // Chỉ hiển thị nút Xin Nghỉ Phép
                    btnQuanLyNhanVien.Visibility = Visibility.Visible;
                    btnXinNghiPhep.Visibility = Visibility.Visible;
                    btnChamCong.Visibility = Visibility.Visible;
                    btnXuatBaoCao.Visibility = Visibility.Visible;
                    break;

                default:
                    MessageBox.Show("Vai trò không hợp lệ!");
                    Close();
                    break;
            }
        }

        private void HienThiThongBao(int maNV)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT NoiDung FROM ThongBao WHERE MaNV = @MaNV AND DaXem = 0";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNV", maNV);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string noiDung = reader["NoiDung"].ToString();
                        MessageBox.Show(noiDung, "Thông báo mới");
                    }
                    reader.Close();

                    // Đánh dấu các thông báo đã xem
                    string updateQuery = "UPDATE ThongBao SET DaXem = 1 WHERE MaNV = @MaNV AND DaXem = 0";
                    SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@MaNV", maNV);
                    updateCmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi hiển thị thông báo: " + ex.Message);
            }
        }

        private void btnQuanLyNhanVien_Click(object sender, RoutedEventArgs e)
        {
            QuanLyNhanVienWindow quanLyNhanVienWindow = new QuanLyNhanVienWindow { CurrentUser = CurrentUser };
            quanLyNhanVienWindow.ShowDialog();
        }

        private void btnXinNghiPhep_Click(object sender, RoutedEventArgs e)
        {
            XinNghiPhepWindow xinNghiPhepWindow = new XinNghiPhepWindow(CurrentUser.MaNV, CurrentUser.HoTen, CurrentUser.VaiTro);
            xinNghiPhepWindow.ShowDialog();
        }

        private void btnDuyetNghiPhep_Click(object sender, RoutedEventArgs e)
        {
            DuyetNghiPhepWindow duyetNghiPhepWindow = new DuyetNghiPhepWindow(CurrentUser.MaNV, CurrentUser.VaiTro);
            duyetNghiPhepWindow.ShowDialog();
        }

        private void btnChamCong_Click(object sender, RoutedEventArgs e)
        {
            ChamCongWindow chamCongWindow = new ChamCongWindow(CurrentUser.MaNV, CurrentUser.VaiTro);
            chamCongWindow.ShowDialog();
        }

        private void btnXuatBaoCao_Click(object sender, RoutedEventArgs e)
        {
            XuatBaoCaoWindow xuatBaoCaoWindow = new XuatBaoCaoWindow(CurrentUser.MaNV);
            xuatBaoCaoWindow.ShowDialog();
        }

        private void btnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Window.GetWindow(this).Close();
        }
    }
}