using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Configuration;
using System.Threading;

namespace HHCORP_HRM
{
    public partial class ChamCongWindow : Window
    {
        private static readonly string subscriptionKey = "8CAX9hNg30n4m7LdSWoi02zSZSuEEHTOb08Yw3eNxgXFIKj8RzSRJQQJ99BCACqBBLyXJ3w3AAAKACOGwe0y";
        private static readonly string endpoint = "https://face-api-hrm.cognitiveservices.azure.com/";
        private static IFaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey))
        {
            Endpoint = endpoint
        };
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private VideoCaptureDevice videoDevice;
        private List<ChamCong> chamCongRecords;
        private readonly string connectionString;
        private int MaNV; // Mã nhân viên đăng nhập
        private string VaiTro; // Vai trò của người dùng

        public ChamCongWindow(int maNV, string vaiTro)
        {
            InitializeComponent();

            // Đọc chuỗi kết nối từ App.config
            var connectionStringSettings = ConfigurationManager.ConnectionStrings["QuanLyNhanSu"];
            if (connectionStringSettings == null || string.IsNullOrEmpty(connectionStringSettings.ConnectionString))
            {
                MessageBox.Show("Chuỗi kết nối 'QuanLyNhanSu' không được tìm thấy trong App.config! Vui lòng kiểm tra file cấu hình.");
                return;
            }
            connectionString = connectionStringSettings.ConnectionString;

            MaNV = maNV;
            VaiTro = vaiTro;
            chamCongRecords = new List<ChamCong>();
            dgvChamCong.ItemsSource = chamCongRecords;

            Loaded += ChamCongWindow_Loaded;
            Closing += Window_Closing;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
                videoSource = null;
            }
        }

        private void ChamCongWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Phân quyền dựa trên VaiTro
            ApplyPermissions();

            LoadMaNhanVien();
            LoadChamCong();
            InitializeCamera();
        }

        private void ApplyPermissions()
        {
            switch (VaiTro)
            {
                case "Tổng Giám đốc":
                    // Có đầy đủ quyền, không cần vô hiệu hóa
                    break;

                case "Kế toán":
                    // Chỉ có quyền xem và làm mới
                    btnThem.IsEnabled = false;
                    btnSua.IsEnabled = false;
                    btnXoa.IsEnabled = false;
                    btnNhanDien.IsEnabled = false;
                    btnRegisterFaces.IsEnabled = false;
                    cbMaNV.IsEnabled = false; // Không cho phép chọn nhân viên khác
                    dpThoiGianVao.IsEnabled = false;
                    dpThoiGianRa.IsEnabled = false;
                    cbTrangThai.IsEnabled = false;
                    txtGhiChu.IsEnabled = false;
                    break;

                case "Nhân viên":
                    // Chỉ có quyền chấm công bằng nhận diện và xem thông tin của mình
                    btnThem.IsEnabled = false;
                    btnSua.IsEnabled = false;
                    btnXoa.IsEnabled = false;
                    btnLamMoi.IsEnabled = false;
                    //btnRegisterFaces.IsEnabled = false;
                    cbMaNV.IsEnabled = false; // Không cho phép chọn nhân viên khác
                    dpThoiGianVao.IsEnabled = false;
                    dpThoiGianRa.IsEnabled = false;
                    cbTrangThai.IsEnabled = false;
                    txtGhiChu.IsEnabled = false;
                    break;

                default:
                    MessageBox.Show("Vai trò không hợp lệ!");
                    Close();
                    break;
            }
        }

        private void InitializeCamera()
        {
            try
            {
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy thiết bị camera!");
                    return;
                }

                string deviceList = "Danh sách thiết bị camera:\n";
                foreach (FilterInfo device in videoDevices)
                {
                    deviceList += device.Name + "\n";
                    if (device.Name.Contains("Iriun"))
                    {
                        videoSource = new VideoCaptureDevice(device.MonikerString);
                    }
                }
                MessageBox.Show(deviceList);

                if (videoSource == null && videoDevices.Count > 0)
                {
                    videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                    MessageBox.Show($"Không tìm thấy Iriun, chọn thiết bị mặc định: {videoDevices[0].Name}");
                }

                if (videoSource != null)
                {
                    videoSource.NewFrame += VideoSource_NewFrame;
                    videoSource.Start();
                    MessageBox.Show("Camera đã khởi động: " + videoSource.Source);
                }
                else
                {
                    MessageBox.Show("Không thể khởi động camera!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi khởi động camera: " + ex.Message);
            }
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    Dispatcher.Invoke(() =>
                    {
                        WebcamFeed.Source = BitmapToImageSource(bitmap);
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xử lý khung hình: " + ex.Message);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                return bitmapImage;
            }
        }

        private void LoadMaNhanVien()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;
                    if (VaiTro == "Nhân viên")
                    {
                        // Chỉ hiển thị MaNV của nhân viên hiện tại
                        query = "SELECT MaNV FROM NhanVien WHERE MaNV = @MaNV";
                    }
                    else
                    {
                        // Kế toán và Tổng Giám đốc thấy tất cả MaNV
                        query = "SELECT DISTINCT MaNV FROM NhanVien";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (VaiTro == "Nhân viên")
                    {
                        cmd.Parameters.AddWithValue("@MaNV", MaNV);
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
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
                    cbMaNV.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải mã nhân viên: " + ex.Message);
            }
        }

        private void LoadChamCong()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query;
                    if (VaiTro == "Nhân viên")
                    {
                        // Chỉ hiển thị bản ghi chấm công của nhân viên hiện tại
                        query = @"
                            SELECT cc.MaChamCong, cc.MaNV, nv.HoTen, cc.NgayChamCong, cc.ThoiGianVao, cc.ThoiGianRa, cc.TrangThai, cc.GhiChu
                            FROM ChamCong cc
                            LEFT JOIN NhanVien nv ON cc.MaNV = nv.MaNV
                            WHERE cc.MaNV = @MaNV";
                    }
                    else
                    {
                        // Kế toán và Tổng Giám đốc thấy tất cả bản ghi
                        query = @"
                            SELECT cc.MaChamCong, cc.MaNV, nv.HoTen, cc.NgayChamCong, cc.ThoiGianVao, cc.ThoiGianRa, cc.TrangThai, cc.GhiChu
                            FROM ChamCong cc
                            LEFT JOIN NhanVien nv ON cc.MaNV = nv.MaNV";
                    }

                    SqlCommand cmd = new SqlCommand(query, conn);
                    if (VaiTro == "Nhân viên")
                    {
                        cmd.Parameters.AddWithValue("@MaNV", MaNV);
                    }

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    chamCongRecords.Clear();
                    foreach (DataRow row in dt.Rows)
                    {
                        chamCongRecords.Add(new ChamCong
                        {
                            MaChamCong = row["MaChamCong"].ToString(),
                            MaNV = row["MaNV"].ToString(),
                            HoTen = row["HoTen"]?.ToString() ?? "Không tìm thấy",
                            NgayChamCong = Convert.ToDateTime(row["NgayChamCong"]),
                            ThoiGianVao = Convert.ToDateTime(row["ThoiGianVao"]),
                            ThoiGianRa = row["ThoiGianRa"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["ThoiGianRa"]),
                            TrangThai = row["TrangThai"].ToString(),
                            GhiChu = row["GhiChu"].ToString()
                        });
                    }
                    dgvChamCong.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu chấm công: " + ex.Message);
            }
        }

        private async void btnNhanDien_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Bitmap capturedImage = CaptureImageFromWebcam();
                if (capturedImage == null)
                {
                    MessageBox.Show("Không thể tiến hành nhận diện vì không chụp được ảnh!");
                    return;
                }
                await RecognizeAndLogAttendance(capturedImage);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhận diện khuôn mặt: {ex.Message}");
            }
        }

        private Bitmap CaptureImageFromWebcam()
        {
            Bitmap bitmap = null;
            if (videoSource == null || !videoSource.IsRunning)
            {
                MessageBox.Show("Camera chưa được khởi động hoặc đã dừng!");
                return null;
            }

            ManualResetEvent frameEvent = new ManualResetEvent(false);
            NewFrameEventHandler frameHandler = (sender, eventArgs) =>
            {
                bitmap = (Bitmap)eventArgs.Frame.Clone();
                frameEvent.Set();
            };

            videoSource.NewFrame += frameHandler;
            if (!frameEvent.WaitOne(5000))
            {
                MessageBox.Show("Không thể chụp ảnh từ camera! Kiểm tra xem camera có hoạt động không.");
                videoSource.NewFrame -= frameHandler;
                return null;
            }
            videoSource.NewFrame -= frameHandler;

            return bitmap;
        }

        private async Task RecognizeAndLogAttendance(Bitmap image)
        {
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, image.RawFormat);
                memoryStream.Position = 0;

                var faces = await faceClient.Face.DetectWithStreamAsync(memoryStream);
                if (faces.Count > 0)
                {
                    foreach (var face in faces)
                    {
                        var faceId = face.FaceId.Value;
                        var registeredFaces = await faceClient.Face.IdentifyAsync(new List<Guid> { faceId }, "hhcorp-employees");
                        if (registeredFaces.Count > 0 && registeredFaces[0].Candidates.Count > 0)
                        {
                            var personId = registeredFaces[0].Candidates[0].PersonId;
                            var person = await faceClient.PersonGroupPerson.GetAsync("hhcorp-employees", personId);

                            // Kiểm tra xem nhân viên nhận diện có phải là người đang đăng nhập (đối với vai trò Nhân viên)
                            if (VaiTro == "Nhân viên" && person.Name != MaNV.ToString())
                            {
                                MessageBox.Show("Bạn chỉ có thể chấm công cho chính mình!");
                                return;
                            }

                            using (var connection = new SqlConnection(connectionString))
                            {
                                await connection.OpenAsync();
                                string query = "INSERT INTO ChamCong (MaNV, NgayChamCong, ThoiGianVao, TrangThai, GhiChu) VALUES (@MaNV, @NgayChamCong, @ThoiGianVao, @TrangThai, @GhiChu)";
                                using (var command = new SqlCommand(query, connection))
                                {
                                    command.Parameters.AddWithValue("@MaNV", person.Name);
                                    command.Parameters.AddWithValue("@NgayChamCong", DateTime.Today);
                                    command.Parameters.AddWithValue("@ThoiGianVao", DateTime.Now);
                                    command.Parameters.AddWithValue("@TrangThai", "Thành công");
                                    command.Parameters.AddWithValue("@GhiChu", "Chấm công tự động bằng nhận diện khuôn mặt");
                                    await command.ExecuteNonQueryAsync();
                                }
                            }

                            chamCongRecords.Add(new ChamCong
                            {
                                MaChamCong = Guid.NewGuid().ToString(),
                                MaNV = person.Name,
                                HoTen = person.UserData,
                                NgayChamCong = DateTime.Today,
                                ThoiGianVao = DateTime.Now,
                                TrangThai = "Thành công",
                                GhiChu = "Chấm công tự động bằng nhận diện khuôn mặt"
                            });
                            dgvChamCong.Items.Refresh();
                            MessageBox.Show($"Chấm công thành công cho {person.UserData} vào lúc {DateTime.Now}");
                        }
                        else
                        {
                            MessageBox.Show("Không tìm thấy khuôn mặt trong danh sách đăng ký.");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Không phát hiện khuôn mặt.");
                }
            }
        }

        private void btnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            videoDevice?.Stop();
            this.Close();
        }

        private void btnThem_Click(object sender, RoutedEventArgs e)
        {
            if (cbMaNV.SelectedItem == null || dpThoiGianVao.SelectedDate == null || cbTrangThai.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng điền đầy đủ các trường bắt buộc!");
                return;
            }

            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "INSERT INTO ChamCong (MaNV, NgayChamCong, ThoiGianVao, ThoiGianRa, TrangThai, GhiChu) VALUES (@MaNV, @NgayChamCong, @ThoiGianVao, @ThoiGianRa, @TrangThai, @GhiChu)";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaNV", cbMaNV.SelectedValue.ToString());
                        command.Parameters.AddWithValue("@NgayChamCong", dpThoiGianVao.SelectedDate.Value);
                        command.Parameters.AddWithValue("@ThoiGianVao", dpThoiGianVao.SelectedDate.Value);
                        command.Parameters.AddWithValue("@ThoiGianRa", dpThoiGianRa.SelectedDate.HasValue ? dpThoiGianRa.SelectedDate.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TrangThai", cbTrangThai.SelectedItem.ToString());
                        command.Parameters.AddWithValue("@GhiChu", txtGhiChu.Text);
                        command.ExecuteNonQuery();
                    }
                }

                chamCongRecords.Add(new ChamCong
                {
                    MaChamCong = Guid.NewGuid().ToString(),
                    MaNV = cbMaNV.SelectedValue.ToString(),
                    NgayChamCong = dpThoiGianVao.SelectedDate.Value,
                    ThoiGianVao = dpThoiGianVao.SelectedDate.Value,
                    ThoiGianRa = dpThoiGianRa.SelectedDate,
                    TrangThai = cbTrangThai.SelectedItem.ToString(),
                    GhiChu = txtGhiChu.Text
                });
                dgvChamCong.Items.Refresh();
                MessageBox.Show("Thêm chấm công thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm chấm công: " + ex.Message);
            }
        }

        private void btnSua_Click(object sender, RoutedEventArgs e)
        {
            if (dgvChamCong.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bản ghi để sửa!");
                return;
            }

            try
            {
                var selectedRecord = (ChamCong)dgvChamCong.SelectedItem;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE ChamCong SET MaNV = @MaNV, NgayChamCong = @NgayChamCong, ThoiGianVao = @ThoiGianVao, ThoiGianRa = @ThoiGianRa, TrangThai = @TrangThai, GhiChu = @GhiChu WHERE MaChamCong = @MaChamCong";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaChamCong", selectedRecord.MaChamCong);
                        command.Parameters.AddWithValue("@MaNV", cbMaNV.SelectedValue.ToString());
                        command.Parameters.AddWithValue("@NgayChamCong", dpThoiGianVao.SelectedDate.Value);
                        command.Parameters.AddWithValue("@ThoiGianVao", dpThoiGianVao.SelectedDate.Value);
                        command.Parameters.AddWithValue("@ThoiGianRa", dpThoiGianRa.SelectedDate.HasValue ? dpThoiGianRa.SelectedDate.Value : (object)DBNull.Value);
                        command.Parameters.AddWithValue("@TrangThai", cbTrangThai.SelectedItem.ToString());
                        command.Parameters.AddWithValue("@GhiChu", txtGhiChu.Text);
                        command.ExecuteNonQuery();
                    }
                }

                selectedRecord.MaNV = cbMaNV.SelectedValue.ToString();
                selectedRecord.NgayChamCong = dpThoiGianVao.SelectedDate.Value;
                selectedRecord.ThoiGianVao = dpThoiGianVao.SelectedDate.Value;
                selectedRecord.ThoiGianRa = dpThoiGianRa.SelectedDate;
                selectedRecord.TrangThai = cbTrangThai.SelectedItem.ToString();
                selectedRecord.GhiChu = txtGhiChu.Text;
                dgvChamCong.Items.Refresh();
                MessageBox.Show("Sửa chấm công thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi sửa chấm công: " + ex.Message);
            }
        }

        private void btnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (dgvChamCong.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một bản ghi để xóa!");
                return;
            }

            try
            {
                var selectedRecord = (ChamCong)dgvChamCong.SelectedItem;
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM ChamCong WHERE MaChamCong = @MaChamCong";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@MaChamCong", selectedRecord.MaChamCong);
                        command.ExecuteNonQuery();
                    }
                }

                chamCongRecords.Remove(selectedRecord);
                dgvChamCong.Items.Refresh();
                MessageBox.Show("Xóa chấm công thành công!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa chấm công: " + ex.Message);
            }
        }

        private void btnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            cbMaNV.SelectedIndex = -1;
            dpThoiGianVao.SelectedDate = null;
            dpThoiGianRa.SelectedDate = null;
            cbTrangThai.SelectedIndex = -1;
            txtGhiChu.Text = string.Empty;
            LoadChamCong();
        }

        private void dgvChamCong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgvChamCong.SelectedItem != null)
            {
                var selectedRecord = (ChamCong)dgvChamCong.SelectedItem;
                cbMaNV.SelectedItem = selectedRecord.MaNV;
                dpThoiGianVao.SelectedDate = selectedRecord.NgayChamCong;
                dpThoiGianRa.SelectedDate = selectedRecord.ThoiGianRa;
                cbTrangThai.SelectedItem = selectedRecord.TrangThai;
                txtGhiChu.Text = selectedRecord.GhiChu;
            }
        }

        private async void btnRegisterFaces_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await FaceRegistration.RegisterEmployees();
                MessageBox.Show("Đăng ký khuôn mặt hoàn tất!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đăng ký: {ex.Message}");
            }
        }

        private void cbMaNV_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbMaNV.SelectedItem == null)
            {
                txtHoTen.Text = string.Empty;
                return;
            }

            try
            {
                if (!int.TryParse(cbMaNV.SelectedValue?.ToString(), out int selectedMaNV))
                {
                    txtHoTen.Text = "Không xác định";
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT HoTen FROM NhanVien WHERE MaNV = @MaNV";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNV", selectedMaNV);
                        object result = cmd.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show($"Không tìm thấy nhân viên với MaNV = {selectedMaNV} trong bảng NhanVien!");
                            txtHoTen.Text = "Không tìm thấy nhân viên";
                        }
                        else
                        {
                            txtHoTen.Text = result.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lấy họ tên nhân viên: " + ex.Message);
                txtHoTen.Text = "Lỗi";
            }
        }
    }

    public class ChamCong
    {
        public string MaChamCong { get; set; }
        public string MaNV { get; set; }
        public string HoTen { get; set; }
        public DateTime NgayChamCong { get; set; }
        public DateTime ThoiGianVao { get; set; }
        public DateTime? ThoiGianRa { get; set; }
        public string TrangThai { get; set; }
        public string GhiChu { get; set; }
    }
}