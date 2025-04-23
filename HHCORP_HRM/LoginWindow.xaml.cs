using System;
using System.Data.SqlClient;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace HHCORP_HRM
{
    public partial class LoginWindow : Window
    {
        private readonly string connectionString;

        public LoginWindow()
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

            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            txtEmail.Focus();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ email và mật khẩu!");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT MaNV, HoTen, VaiTro, MatKhau FROM NhanVien WHERE Email = @Email";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Email", email);
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        string storedPassword = reader["MatKhau"].ToString();
                        // Giả sử mật khẩu được lưu dạng plain text (không mã hóa)
                        if (storedPassword == password)
                        {
                            int maNV = Convert.ToInt32(reader["MaNV"]);
                            string hoTen = reader["HoTen"].ToString();
                            string vaiTro = reader["VaiTro"].ToString();

                            MainWindow mainWindow = new MainWindow(new UserInfo(maNV, hoTen, vaiTro));
                            mainWindow.Show();
                            Window.GetWindow(this).Close();
                        }
                        else
                        {
                            MessageBox.Show("Mật khẩu không đúng!");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Email không tồn tại!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng nhập: " + ex.Message);
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}