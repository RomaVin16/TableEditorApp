using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TableEditor.Repository;
using TableEditor.Repository.Repository;

namespace TableEditor
{
    public partial class MainWindow
    {
        private readonly IConfiguration configuration;
        private readonly List<Column> columns = new List<Column>();
        private List<Column> editColumns = new List<Column>();
        private List<Column> newEditColumns = new List<Column>();
        private readonly DataRepository context;

        public MainWindow()
        {
            InitializeComponent();
            NewFieldsDataGrid.ItemsSource = columns;

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            configuration = builder.Build();

            context = DataRepository.Create(configuration);

            LoadTablesListAsync(context);
        }

        /// <summary>
        /// Добавить поле для новой таблицы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFieldButton_Click(object sender, RoutedEventArgs e)
        {
            columns.Add(new Column { FieldName = "Новое поле", DataType = "Целочисленный", IsPrimaryKey = false });
            NewFieldsDataGrid.Items.Refresh();
        }

        /// <summary>
        /// Создать новую таблицу
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CreateTableButton_Click(object sender, RoutedEventArgs e)
        {
            var tableName = NewTableNameTextBox.Text.Trim();
            var columns = new List<Column>();

            foreach (var item in NewFieldsDataGrid.Items)
            {
                if (item is Column column)
                {
                    columns.Add(column);
                }
            }

            try
            {

                await using(var context = DataRepository.Create(configuration))
                {
                    await context.CreateTableAsync(tableName, columns);
                    await LoadTablesListAsync(context);
                }

                MessageBox.Show("Таблица успешно создана.");
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузить список таблиц из БД
        /// </summary>
        /// <returns></returns>
        private async Task LoadTablesListAsync(DataRepository context)
        {
            try
            {
                var tableNames = await context.GetTableNamesAsync();
                TablesListBox.ItemsSource = tableNames;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке таблиц: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработка выбора таблицы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TablesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTable = TablesListBox.SelectedItem as string;
            if (selectedTable != null)
            {
                await LoadEditFieldsAsync(selectedTable);
            }
        }

        /// <summary>
        /// Загрузить поля для редактируемой таблицы
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        private async Task LoadEditFieldsAsync(string tableName)
        {
            try
            {
                using (var context = DataRepository.Create(configuration))
                {
                    editColumns = await context.GetColumnsAsync(tableName, context);
                    EditFieldsDataGrid.ItemsSource = editColumns;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке полей таблицы: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавление новых полей 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFieldToEditButton_Click(object sender, RoutedEventArgs e)
        {
            var newColumn = new Column { FieldName = "Новое поле", DataType = "Целочисленный", IsPrimaryKey = false };

            newEditColumns.Add(newColumn);
            editColumns.Add(newColumn);

            EditFieldsDataGrid.ItemsSource = editColumns;
            EditFieldsDataGrid.Items.Refresh();
        }

        /// <summary>
        /// Обновление таблицы 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UpdateTableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tableName = TablesListBox.SelectedItem as string;

                if (tableName == null)
                {
                    MessageBox.Show("Выберите таблицу для обновления.");
                    return;
                }

                var newColumns = newEditColumns.ToList();

                await using (var context = DataRepository.Create(configuration))
                {
                    foreach (var column in newColumns)
                    {
                        var columnType = column.DataType switch
                        {
                            "Целочисленный" => "INTEGER",
                            "Вещественный" => "FLOAT",
                            "Текстовый" => "VARCHAR(255)",
                            "Дата/Время" => "TIMESTAMP"
                        };

                        var sql = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{column.FieldName}\" {columnType};";

                        await context.Database.ExecuteSqlRawAsync(sql);
                    }
                }

                MessageBox.Show("Таблица успешно обновлена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении таблицы: {ex.Message}");
            }
        }

        /// <summary>
        /// Удаление таблицы из БД 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteTableButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var tableName = TablesListBox.SelectedItem as string;
                if (tableName == null)
                {
                    MessageBox.Show("Выберите таблицу для удаления.");
                    return;
                }

                await using (var context = DataRepository.Create(configuration))
                {
                    await context.DeleteTable(tableName);
                    await LoadTablesListAsync(context);
                }

                MessageBox.Show("Таблица успешно удалена.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении таблицы: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            context.Dispose();
            base.OnClosed(e);
        }
    }
}
