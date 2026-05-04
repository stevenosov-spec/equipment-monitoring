using System;
using System.Windows;

namespace EquipmentMonitoring.App.Views
{
    public partial class OeeDateRangeDialog : Window
    {
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        public OeeDateRangeDialog()
        {
            InitializeComponent();
            // Устанавливаем начальные значения (последний час)
            StartDatePicker.SelectedDate = DateTime.Now.AddHours(-1);
            EndDatePicker.SelectedDate = DateTime.Now;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите обе даты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartDate = StartDatePicker.SelectedDate.Value;
            EndDate = EndDatePicker.SelectedDate.Value;

            if (StartDate > EndDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}