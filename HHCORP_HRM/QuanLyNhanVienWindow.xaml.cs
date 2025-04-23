using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace HHCORP_HRM
{
    public class UserInfo
    {
        public int MaNV { get; set; }
        public string HoTen { get; set; }
        public string VaiTro { get; set; }

        public UserInfo(int maNV, string hoTen, string vaiTro)
        {
            MaNV = maNV;
            HoTen = hoTen;
            VaiTro = vaiTro;
        }
    }

    public partial class QuanLyNhanVienWindow : Window
    {
        private readonly string connectionString;
        public UserInfo CurrentUser { get; set; }

        public QuanLyNhanVienWindow()
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

            Loaded += QuanLyNhanVienWindow_Loaded;
        }

        private void QuanLyNhanVienWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentUser == null)
            {
                MessageBox.Show("Không có thông tin người dùng! Vui lòng đăng nhập lại.");
                Close();
                return;
            }

            // Hiển thị thông tin người dùng
            Title = $"Quản Lý Nhân Viên - Xin chào, {CurrentUser.HoTen} ({CurrentUser.VaiTro})";

            // Phân quyền dựa trên VaiTro
            ApplyPermissions();

            // Tải dữ liệu sau khi phân quyền
            LoadNhanVien();
        }

        private void ApplyPermissions()
        {
            switch (CurrentUser.VaiTro)
            {
                case "Tổng Giám đốc":
                    // Có đầy đủ quyền, không cần vô hiệu hóa
                    break;

                case "Kế toán":
                    // Chỉ có quyền xem và làm mới
                    btnThem.IsEnabled = false;
                    btnSua.IsEnabled = false;
                    btnXoa.IsEnabled = false;
                    txtHoTen.IsEnabled = false;
                    cbVaiTro.IsEnabled = false;
                    txtEmail.IsEnabled = false;
                    txtSoDienThoai.IsEnabled = false;
                    txtMatKhau.IsEnabled = false;
                    txtPersonID.IsEnabled = false;
                    break;

                case "Nhân viên":
                    // Chỉ có quyền xem thông tin của mình
                    btnThem.IsEnabled = false;
                    btnSua.IsEnabled = false;
                    btnXoa.IsEnabled = false;
                    btnLamMoi.IsEnabled = false;
                    txtHoTen.IsEnabled = false;
                    cbVaiTro.IsEnabled = false;
                    txtEmail.IsEnabled = false;
                    txtSoDienThoai.IsEnabled = false;
                    txtMatKhau.IsEnabled = false;
                    txtPersonID.IsEnabled = false;
                    break;

                default:
                    MessageBox.Show("Vai trò không hợp lệ!");
                    Close();
                    break;
            }
        }

        private void btnDangXuat_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Window.GetWindow(this).Close();
        }

        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void LoadNhanVien()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;
                    if (CurrentUser.VaiTro == "Nhân viên")
                    {
                        // Chỉ hiển thị thông tin của nhân viên đang đăng nhập
                        query = "SELECT MaNV, HoTen, VaiTro, Email, SoDienThoai FROM NhanVien WHERE MaNV = @MaNV";
                    }
                    else
                    {
                        // Kế toán và Tổng Giám đốc thấy toàn bộ danh sách
                        query = "SELECT MaNV, HoTen, VaiTro, Email, SoDienThoai FROM NhanVien";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (CurrentUser.VaiTro == "Nhân viên")
                    {
                        cmd.Parameters.AddWithValue("@MaNV", CurrentUser.MaNV);
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show("Không có dữ liệu trong bảng NhanVien!");
                    }
                    dgvNhanVien.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message);
            }
        }

        private void btnThem_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) ||
                string.IsNullOrWhiteSpace(txtSoDienThoai.Text) ||
                string.IsNullOrWhiteSpace(txtMatKhau.Text))
            {
                MessageBox.Show("Họ Tên, Số Điện Thoại và Mật Khẩu không được để trống!");
                return;
            }

            string vaiTro = (cbVaiTro.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(vaiTro) || !IsValidVaiTro(vaiTro))
            {
                MessageBox.Show("Vui lòng chọn một Vai Trò hợp lệ từ danh sách!");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO NhanVien (HoTen, VaiTro, Email, SoDienThoai, MatKhau, PersonID) " +
                                   "VALUES (@HoTen, @VaiTro, @Email, @SoDienThoai, @MatKhau, @PersonID)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.Add("@HoTen", SqlDbType.NVarChar).Value = txtHoTen.Text;
                    cmd.Parameters.Add("@VaiTro", SqlDbType.NVarChar).Value = vaiTro;
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text;
                    cmd.Parameters.Add("@SoDienThoai", SqlDbType.NVarChar).Value = txtSoDienThoai.Text;
                    cmd.Parameters.Add("@MatKhau", SqlDbType.NVarChar).Value = txtMatKhau.Text;
                    cmd.Parameters.Add("@PersonID", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(txtPersonID.Text) ? (object)DBNull.Value : txtPersonID.Text;
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Thêm nhân viên thành công!");
                    LoadNhanVien();
                    ClearInputs();
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // Lỗi trùng lặp (UNIQUE constraint)
            {
                MessageBox.Show("Email đã tồn tại! Vui lòng sử dụng email khác.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNhanVien.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn nhân viên để sửa!");
                return;
            }

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(txtHoTen.Text) ||
                string.IsNullOrWhiteSpace(txtSoDienThoai.Text) ||
                string.IsNullOrWhiteSpace(txtMatKhau.Text))
            {
                MessageBox.Show("Họ Tên, Số Điện Thoại và Mật Khẩu không được để trống!");
                return;
            }

            string vaiTro = (cbVaiTro.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(vaiTro) || !IsValidVaiTro(vaiTro))
            {
                MessageBox.Show("Vui lòng chọn một Vai Trò hợp lệ từ danh sách!");
                return;
            }

            try
            {
                DataRowView row = dgvNhanVien.SelectedItem as DataRowView;
                int maNV = Convert.ToInt32(row["MaNV"]);
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE NhanVien SET HoTen = @HoTen, VaiTro = @VaiTro, Email = @Email, " +
                                   "SoDienThoai = @SoDienThoai, MatKhau = @MatKhau, PersonID = @PersonID WHERE MaNV = @MaNV";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.Add("@MaNV", SqlDbType.Int).Value = maNV;
                    cmd.Parameters.Add("@HoTen", SqlDbType.NVarChar).Value = txtHoTen.Text;
                    cmd.Parameters.Add("@VaiTro", SqlDbType.NVarChar).Value = vaiTro;
                    cmd.Parameters.Add("@Email", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text;
                    cmd.Parameters.Add("@SoDienThoai", SqlDbType.NVarChar).Value = txtSoDienThoai.Text;
                    cmd.Parameters.Add("@MatKhau", SqlDbType.NVarChar).Value = txtMatKhau.Text;
                    cmd.Parameters.Add("@PersonID", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(txtPersonID.Text) ? (object)DBNull.Value : txtPersonID.Text;
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Sửa nhân viên thành công!");
                    LoadNhanVien();
                    ClearInputs();
                }
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601) // Lỗi trùng lặp (UNIQUE constraint)
            {
                MessageBox.Show("Email đã tồn tại! Vui lòng sử dụng email khác.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dgvNhanVien.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn nhân viên để xóa!");
                return;
            }

            if (MessageBox.Show("Bạn có chắc muốn xóa nhân viên này?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                DataRowView row = dgvNhanVien.SelectedItem as DataRowView;
                int maNV = Convert.ToInt32(row["MaNV"]);
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "DELETE FROM NhanVien WHERE MaNV = @MaNV";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@MaNV", maNV);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Xóa nhân viên thành công!");
                    LoadNhanVien();
                    ClearInputs();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message);
            }
        }

        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            ClearInputs();
            LoadNhanVien();
        }

        private void ClearInputs()
        {
            txtHoTen.Text = "";
            cbVaiTro.SelectedIndex = -1;
            txtEmail.Text = "";
            txtSoDienThoai.Text = "";
            txtMatKhau.Text = "";
            txtPersonID.Text = "";
        }

        private void dgvNhanVien_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvNhanVien.SelectedItem == null)
            {
                return;
            }

            DataRowView row = dgvNhanVien.SelectedItem as DataRowView;
            txtHoTen.Text = row["HoTen"]?.ToString() ?? "";
            string vaiTro = row["VaiTro"]?.ToString();
            if (!string.IsNullOrEmpty(vaiTro))
            {
                foreach (ComboBoxItem item in cbVaiTro.Items)
                {
                    if (item.Content.ToString() == vaiTro)
                    {
                        cbVaiTro.SelectedItem = item;
                        break;
                    }
                }
            }
            else
            {
                cbVaiTro.SelectedIndex = -1;
            }
            txtEmail.Text = row["Email"]?.ToString() ?? "";
            txtSoDienThoai.Text = row["SoDienThoai"]?.ToString() ?? "";
            // Không hiển thị MatKhau và PersonID trong giao diện, nên không cần gán
        }

        private bool IsValidVaiTro(string vaiTro)
        {
            string[] validVaiTro = { "Tổng Giám đốc", "Kế toán", "Nhân viên" };
            return Array.IndexOf(validVaiTro, vaiTro) >= 0;
        }
    }
}