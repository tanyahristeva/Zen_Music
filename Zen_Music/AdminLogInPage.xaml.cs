using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;

namespace Zen_Music
{
    public partial class AdminLoginPage : Window
    {
        public AdminLoginPage()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void btnLogIn_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please fill in all fields!", "Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();
                    string query = @"SELECT ID, Password_Hash, Password_Salt, Role_ID 
                             FROM Users WHERE Username = @Username";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("Invalid username or password!", "Error",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            string hash = reader["Password_Hash"].ToString();
                            string salt = reader["Password_Salt"].ToString();
                            int roleId = Convert.ToInt32(reader["Role_ID"]);

                            if (!PasswordHelper.VerifyPassword(password, salt, hash))
                            {
                                MessageBox.Show("Invalid username or password!", "Error",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            if (roleId != 1)
                            {
                                MessageBox.Show("Access denied! Admin only.", "Unauthorized",
                                                MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            SessionManager.UserId = Convert.ToInt32(reader["ID"]);
                            SessionManager.Username = username;
                        }
                    }
                }

                var adminPage = new MainWindow();
                adminPage.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
