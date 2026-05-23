using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Zen_Music
{
    public partial class SignUpPage : Window
    {
        public SignUpPage()
        {
            InitializeComponent();
            txtUsername.TextChanged += ValidateUsername;
            txtEmail.TextChanged += ValidateEmail;
            txtPassword.PasswordChanged += ValidatePassword;

        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void btnSignUp_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password;

            bool hasError = false;

            
            if (string.IsNullOrWhiteSpace(username))
            {
                txtUsernameError.Text = "Username is required.";
                txtUsernameError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else if (username.Length < 3)
            {
                txtUsernameError.Text = "Username must be at least 3 characters.";
                txtUsernameError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else
            {
                txtUsernameError.Visibility = Visibility.Collapsed;
            }

           
            bool validEmail = System.Text.RegularExpressions.Regex.IsMatch(
                email,
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
            );

            if (string.IsNullOrWhiteSpace(email))
            {
                txtEmailError.Text = "Email is required.";
                txtEmailError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else if (!validEmail)
            {
                txtEmailError.Text = "Enter a valid email address.";
                txtEmailError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else
            {
                txtEmailError.Visibility = Visibility.Collapsed;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                txtPasswordError.Text = "Password is required.";
                txtPasswordError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else if (password.Length < 6)
            {
                txtPasswordError.Text = "Password must be at least 6 characters.";
                txtPasswordError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else if (!password.Any(char.IsUpper))
            {
                txtPasswordError.Text = "Password must contain 1 uppercase letter.";
                txtPasswordError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else if (!password.Any(char.IsDigit))
            {
                txtPasswordError.Text = "Password must contain 1 number.";
                txtPasswordError.Visibility = Visibility.Visible;
                hasError = true;
            }
            else
            {
                txtPasswordError.Visibility = Visibility.Collapsed;
            }

            
            if (hasError)
                return;

            try
            {
                string salt = PasswordHelper.GenerateSalt();
                string hash = PasswordHelper.HashPassword(password, salt);

                string cs = ConfigurationManager.ConnectionStrings["MusicDb"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(cs))
                {
                    conn.Open();

                    string query = @"
                INSERT INTO Users
                (Username, Email, Password_Hash, Password_Salt, Role_ID)
                VALUES
                (@Username, @Email, @Hash, @Salt, 2)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Hash", hash);
                        cmd.Parameters.AddWithValue("@Salt", salt);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    "Account created successfully!",
                    "Welcome",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var signIn = new SignInPage();
                signIn.Show();
                this.Close();
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                MessageBox.Show(
                    "Username or email already exists!",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error: " + ex.Message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void btnAlreadyMember_Click(object sender, RoutedEventArgs e)
        {
            var signIn = new SignInPage();
            signIn.Show();
            this.Close();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var adminLogin = new AdminLoginPage();
            adminLogin.ShowDialog();
        }

        private void ValidateUsername(object sender, TextChangedEventArgs e)
        {
            if (txtUsername.Text.Trim().Length < 3)
            {
                txtUsernameError.Text = "Username must be at least 3 characters.";
                txtUsernameError.Visibility = Visibility.Visible;
            }
            else
            {
                txtUsernameError.Visibility = Visibility.Collapsed;
            }
        }

        private void ValidateEmail(object sender, TextChangedEventArgs e)
        {
            string email = txtEmail.Text.Trim();

            if (!email.Contains("@") || !email.Contains("."))
            {
                txtEmailError.Text = "Invalid email address.";
                txtEmailError.Visibility = Visibility.Visible;
            }
            else
            {
                txtEmailError.Visibility = Visibility.Collapsed;
            }
        }

        private void ValidatePassword(object sender, RoutedEventArgs e)
        {
            string password = txtPassword.Password;

            if (password.Length < 6)
            {
                txtPasswordError.Text = "Password must be at least 6 characters.";
                txtPasswordError.Visibility = Visibility.Visible;
            }
            else
            {
                txtPasswordError.Visibility = Visibility.Collapsed;
            }
        }

    
    }
}
