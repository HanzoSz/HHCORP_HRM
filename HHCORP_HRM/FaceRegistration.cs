using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using HHCORP_HRM;
using System.Collections.Generic;

namespace HHCORP_HRM
{
    public static class FaceRegistration
    {
        private static readonly string subscriptionKey = "8CAX9hNg30n4m7LdSWoi02zSZSuEEHTOb08Yw3eNxgXFIKj8RzSRJQQJ99BCACqBBLyXJ3w3AAAKACOGwe0y"; // Thay bằng khóa của bạn
        private static readonly string endpoint = "https://face-api-hrm.cognitiveservices.azure.com/"; // Thay bằng endpoint của bạn
        private static IFaceClient faceClient = new FaceClient(new ApiKeyServiceClientCredentials(subscriptionKey))
        {
            Endpoint = endpoint
        };

        public static async Task CreatePersonGroupAsync()
        {
            try
            {
                string personGroupId = "hhcorp-employees";
                string personGroupName = "HHCORP Employees";
                string personGroupDescription = "Person group for HHCORP employee attendance system";

                // Kiểm tra xem Person Group đã tồn tại chưa
                try
                {
                    await faceClient.PersonGroup.GetAsync(personGroupId);
                    Console.WriteLine($"Person Group '{personGroupId}' đã tồn tại.");
                }
                catch (APIErrorException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // Nếu không tồn tại, tạo mới
                    await faceClient.PersonGroup.CreateAsync(personGroupId, personGroupName, personGroupDescription);
                    Console.WriteLine($"Person Group '{personGroupId}' đã được tạo thành công.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi tạo Person Group: {ex.Message}");
            }
        }

        public static async Task RegisterEmployeeFaceAsync(int maNV, string employeeName, List<string> imagePaths)
        {
            try
            {
                string personGroupId = "hhcorp-employees";

                // Tạo Person trong Person Group
                var person = await faceClient.PersonGroupPerson.CreateAsync(personGroupId, employeeName, userData: maNV.ToString());
                Console.WriteLine($"Đã tạo Person cho nhân viên: {employeeName} (PersonId: {person.PersonId})");

                // Lưu PersonId vào bảng NhanVien
                string connectionString = "your-connection-string";
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string query = "UPDATE NhanVien SET PersonId = @PersonId WHERE MaNV = @MaNV";
                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PersonId", person.PersonId.ToString());
                        command.Parameters.AddWithValue("@MaNV", maNV);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Thêm từng hình ảnh khuôn mặt vào Person
                foreach (var imagePath in imagePaths)
                {
                    using (var imageStream = File.OpenRead(imagePath))
                    {
                        var persistedFace = await faceClient.PersonGroupPerson.AddFaceFromStreamAsync(personGroupId, person.PersonId, imageStream);
                        Console.WriteLine($"Đã thêm khuôn mặt cho nhân viên: {employeeName}, PersistedFaceId: {persistedFace.PersistedFaceId}");

                        // Lưu PersistedFaceId vào bảng EmployeeFaces
                        using (var connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            string queryFace = "INSERT INTO EmployeeFaces (MaNV, PersistedFaceId, ImagePath) VALUES (@MaNV, @PersistedFaceId, @ImagePath)";
                            using (var command = new SqlCommand(queryFace, connection))
                            {
                                command.Parameters.AddWithValue("@MaNV", maNV);
                                command.Parameters.AddWithValue("@PersistedFaceId", persistedFace.PersistedFaceId.ToString());
                                command.Parameters.AddWithValue("@ImagePath", imagePath);
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi đăng ký khuôn mặt: {ex.Message}");
            }
        }

        public static async Task TrainPersonGroupAsync(string personGroupId)
        {
            try
            {
                await faceClient.PersonGroup.TrainAsync(personGroupId);
                Console.WriteLine("Đang huấn luyện Person Group...");

                TrainingStatus trainingStatus = null;
                do
                {
                    await Task.Delay(1000);
                    trainingStatus = await faceClient.PersonGroup.GetTrainingStatusAsync(personGroupId);
                    Console.WriteLine($"Trạng thái huấn luyện: {trainingStatus.Status}");
                } while (trainingStatus.Status == TrainingStatusType.Running);

                if (trainingStatus.Status == TrainingStatusType.Succeeded)
                {
                    Console.WriteLine("Huấn luyện Person Group thành công!");
                }
                else
                {
                    Console.WriteLine("Huấn luyện Person Group thất bại!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi huấn luyện Person Group: {ex.Message}");
            }
        }

        // Hàm để gọi từ nơi khác (ví dụ: từ form)
        public static async Task RegisterEmployees()
        {
            string personGroupId = "hhcorp-employees";

            // Tạo Person Group
            await CreatePersonGroupAsync();

            // Đăng ký khuôn mặt cho nhân viên
            // Nhân viên 3 (nv3) với 6 ảnh
            var nv3Images = new List<string>
    {
        @"C:\Users\Admin\source\repos\HHCORP_HRM\IMAGE_TRAINING\nv3_01.jpg",
        @"C:\Users\Admin\source\repos\HHCORP_HRM\IMAGE_TRAINING\nv3_02.jpg",
        @"C:\Users\Admin\source\repos\HHCORP_HRM\IMAGE_TRAINING\nv3_03.jpg",
        @"C:\Users\Admin\source\repos\HHCORP_HRM\IMAGE_TRAINING\nv3_04.jpg",
        @"C:\Users\Admin\source\repos\HHCORP_HRM\IMAGE_TRAINING\nv3_05.jpg",
        @"C:\Users\Admin\source\repos\HHCORP_HRM\IMAGE_TRAINING\nv3_06.jpg"
    };
            await RegisterEmployeeFaceAsync(3, "Ngô Đức Huy", nv3Images);

            // Huấn luyện Person Group
            await TrainPersonGroupAsync(personGroupId);
        }
    }
}