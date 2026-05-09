using Microsoft.Win32;
using ProGlassAutomation.ViewModels;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ProGlassAutomation.Models
{
    public class ProfileViewModel : INotifyPropertyChanged
    {
        private string _fullName = "John Smith";
        private string _email = "john@email.com";
        private string _phone = "+1234567890";
        private string _role = "Engineer";
        private string _memberSince = "January 2024";
        private string _avatarInitials = "JS";
        private string _avatarImagePath = "";
        private bool _hasAvatar = false;

        private string _firstName = "John";
        private string _lastName = "Smith";
        private string _company = "ProGlass";
        private string _department = "Engineering";
        private string _address = "123 Glass Street";

        private string _currentPassword = "";
        private string _newPassword = "";
        private string _confirmPassword = "";
        private int _passwordStrength = 0;

        private bool _emailNotifications = true;
        private bool _smsNotifications = false;
        private string _selectedLanguage = "English";

        private string _statusMessage = "";

        public ProfileViewModel()
        {
            SaveProfileCommand = new RelayCommand(SaveProfile);
            ChangePasswordCommand = new RelayCommand(ChangePassword);
            DiscardCommand = new RelayCommand(Discard);
            UploadAvatarCommand = new RelayCommand(UploadAvatar);
        }

        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        public string MemberSince
        {
            get => _memberSince;
            set { _memberSince = value; OnPropertyChanged(); }
        }

        public string AvatarInitials
        {
            get => _avatarInitials;
            set { _avatarInitials = value; OnPropertyChanged(); }
        }

        public string AvatarImagePath
        {
            get => _avatarImagePath;
            set
            {
                _avatarImagePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvatarSource));
            }
        }

        public bool HasAvatar
        {
            get => _hasAvatar;
            set { _hasAvatar = value; OnPropertyChanged(); }
        }

        public BitmapImage AvatarSource
        {
            get
            {
                if (!string.IsNullOrEmpty(AvatarImagePath) && File.Exists(AvatarImagePath))
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(AvatarImagePath);
                        bitmap.EndInit();
                        bitmap.Freeze();
                        return bitmap;
                    }
                    catch
                    {
                        return null;
                    }
                }
                return null;
            }
        }

        public string FirstName
        {
            get => _firstName;
            set { _firstName = value; OnPropertyChanged(); UpdateFullName(); }
        }

        public string LastName
        {
            get => _lastName;
            set { _lastName = value; OnPropertyChanged(); UpdateFullName(); }
        }

        public string Company
        {
            get => _company;
            set { _company = value; OnPropertyChanged(); }
        }

        public string Department
        {
            get => _department;
            set { _department = value; OnPropertyChanged(); }
        }

        public string Address
        {
            get => _address;
            set { _address = value; OnPropertyChanged(); }
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set { _currentPassword = value; OnPropertyChanged(); }
        }

        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(); CalculatePasswordStrength(); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { _confirmPassword = value; OnPropertyChanged(); }
        }

        public int PasswordStrength
        {
            get => _passwordStrength;
            set { _passwordStrength = value; OnPropertyChanged(); }
        }

        public string PasswordStrengthText
        {
            get
            {
                return PasswordStrength switch
                {
                    0 => "",
                    1 => "Weak",
                    2 => "Medium",
                    3 => "Strong",
                    4 => "Very Strong",
                    _ => ""
                };
            }
        }

        public string PasswordStrengthColor
        {
            get
            {
                return PasswordStrength switch
                {
                    1 => "#EF4444",
                    2 => "#F59E0B",
                    3 => "#10B981",
                    4 => "#059669",
                    _ => "#94A3B8"
                };
            }
        }

        public bool EmailNotifications
        {
            get => _emailNotifications;
            set { _emailNotifications = value; OnPropertyChanged(); }
        }

        public bool SmsNotifications
        {
            get => _smsNotifications;
            set { _smsNotifications = value; OnPropertyChanged(); }
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set { _selectedLanguage = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string[] Languages { get; } = { "English", "Arabic", "Urdu", "Spanish", "French" };

        public ICommand SaveProfileCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand DiscardCommand { get; }
        public ICommand UploadAvatarCommand { get; }

        private void UpdateFullName()
        {
            FullName = $"{FirstName} {LastName}".Trim();
            if (string.IsNullOrWhiteSpace(FullName)) FullName = "User";
            AvatarInitials = $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}{(LastName.Length > 0 ? LastName[0] : ' ')}".ToUpper().Trim();
            if (string.IsNullOrWhiteSpace(AvatarInitials)) AvatarInitials = "U";
        }

        private void CalculatePasswordStrength()
        {
            int strength = 0;
            string pwd = NewPassword ?? "";

            if (pwd.Length >= 8) strength++;
            if (pwd.Length >= 12) strength++;
            if (pwd.Any(char.IsUpper) && pwd.Any(char.IsLower)) strength++;
            if (pwd.Any(char.IsDigit)) strength++;
            if (pwd.Any(c => !char.IsLetterOrDigit(c))) strength++;

            PasswordStrength = Math.Min(strength, 4);
            OnPropertyChanged(nameof(PasswordStrengthText));
            OnPropertyChanged(nameof(PasswordStrengthColor));
        }

        private void SaveProfile(object parameter)
        {
            StatusMessage = "✓ Profile updated successfully!";
            ClearStatusAfterDelay();
        }

        private void ChangePassword(object parameter)
        {
            if (string.IsNullOrEmpty(CurrentPassword))
            {
                StatusMessage = "⚠ Please enter current password";
                ClearStatusAfterDelay();
                return;
            }

            if (string.IsNullOrEmpty(NewPassword))
            {
                StatusMessage = "⚠ Please enter new password";
                ClearStatusAfterDelay();
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                StatusMessage = "⚠ Passwords do not match";
                ClearStatusAfterDelay();
                return;
            }

            if (PasswordStrength < 2)
            {
                StatusMessage = "⚠ Password is too weak";
                ClearStatusAfterDelay();
                return;
            }

            CurrentPassword = "";
            NewPassword = "";
            ConfirmPassword = "";
            PasswordStrength = 0;
            StatusMessage = "✓ Password changed successfully!";
            ClearStatusAfterDelay();
        }

        private void Discard(object parameter)
        {
            FirstName = "John";
            LastName = "Smith";
            Email = "john@email.com";
            Phone = "+1234567890";
            Company = "ProGlass";
            Department = "Engineering";
            Address = "123 Glass Street";
            StatusMessage = "↩ Changes discarded";
            ClearStatusAfterDelay();
        }

        private void UploadAvatar(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg;*.gif;*.bmp)|*.png;*.jpeg;*.jpg;*.gif;*.bmp|All files (*.*)|*.*",
                Title = "Select Profile Picture"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AvatarImagePath = openFileDialog.FileName;
                HasAvatar = true;
                StatusMessage = "✓ Profile picture updated!";
                ClearStatusAfterDelay();
            }
        }

        private async void ClearStatusAfterDelay()
        {
            await System.Threading.Tasks.Task.Delay(3000);
            StatusMessage = "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}