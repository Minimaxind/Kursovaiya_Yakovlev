    using System;
    using System.Linq;
using System.Text.Json;
using System.Windows;
    using System.Windows.Controls;
    using Microsoft.EntityFrameworkCore;

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;
using Word = DocumentFormat.OpenXml.Wordprocessing;

namespace Kursovaiya_Yakovlev
{
    public partial class ContractWindow : Window
    {
        private Transactions _selectedTransaction;
        public ContractWindow()
        {
            InitializeComponent();
            LoadTransactions();
            LoadFilterData();
            ServiceComboBox.SelectionChanged += ApplyFilters;
            ObjectComboBox.SelectionChanged += ApplyFilters;
            ExecutorComboBox.SelectionChanged += ApplyFilters;
            CustomerComboBox.SelectionChanged += ApplyFilters;
            StatusComboBox.SelectionChanged += ApplyFilters;
            PriceFromTextBox.TextChanged += ApplyFilters;
            PriceToTextBox.TextChanged += ApplyFilters;
        }

        private void LoadTransactions()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                var transactions = context.Transactions

                    .Include(t => t.Property)
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Staff)
                            .ThenInclude(st => st.Users)
                    .Include(t => t.Owner)
                    .Include(t => t.Client)
                    .Include(t => t.Status)
                    .AsEnumerable()
                    .Select(t => new
                    {
                        t.Id,
                        PropertyTitle = t.Property?.Title ?? "Нет данных",
                        Address = FormatAddress(t.Property?.Address),
                        ServiceTitle = t.Service?.Title ?? "Нет данных",
                        OwnerFullName = t.Owner?.FullName ?? "Нет данных",
                        ClientFullName = t.Client?.FullName ?? "Нет данных",
                        RieltorId = t.Service.Staff.Users.Id,
                        RieltorFullName = t.Service?.Staff?.Users?.FullName ?? "Не назначен",
                        t.TransactionDate,
                        t.Amount,
                        StatusTitle = t.Status?.Title ?? "Нет данных"
                    })
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                DataGrid.ItemsSource = transactions;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}\n\n{ex.InnerException?.Message}",
                              "Ошибка",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
        private void ApplyFilters(object sender, RoutedEventArgs e)
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                var filtered = context.Transactions
                    .Include(t => t.Property)
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Staff)
                            .ThenInclude(st => st.Users)
                    .Include(t => t.Owner)
                    .Include(t => t.Client)
                    .Include(t => t.Status)
                    .AsQueryable();

                // Фильтрация по услуге
                if (ServiceComboBox.SelectedValue != null && int.TryParse(ServiceComboBox.SelectedValue.ToString(), out int serviceId))
                {
                    filtered = filtered.Where(t => t.ServiceId == serviceId);
                }

                // Фильтрация по объекту
                if (ObjectComboBox.SelectedValue != null && int.TryParse(ObjectComboBox.SelectedValue.ToString(), out int propertyId))
                {
                    filtered = filtered.Where(t => t.PropertyId == propertyId);
                }

                // Фильтрация по риелтору (исправлено)
                if (ExecutorComboBox.SelectedValue != null && int.TryParse(ExecutorComboBox.SelectedValue.ToString(), out int rieltorId))
                {
                    filtered = filtered.Where(t => t.Service.Staff.Users.Id == rieltorId);
                }

                // Фильтрация по клиенту
                if (CustomerComboBox.SelectedValue != null && int.TryParse(CustomerComboBox.SelectedValue.ToString(), out int clientId))
                {
                    filtered = filtered.Where(t => t.ClientId == clientId);
                }

                // Фильтрация по статусу
                if (StatusComboBox.SelectedValue != null && int.TryParse(StatusComboBox.SelectedValue.ToString(), out int statusId))
                {
                    filtered = filtered.Where(t => t.StatusId == statusId);
                }

                // Фильтрация по цене "от"
                if (decimal.TryParse(PriceFromTextBox.Text, out decimal minPrice))
                {
                    filtered = filtered.Where(t => t.Amount >= minPrice);
                }

                // Фильтрация по цене "до"
                if (decimal.TryParse(PriceToTextBox.Text, out decimal maxPrice))
                {
                    filtered = filtered.Where(t => t.Amount <= maxPrice);
                }

                // Проецируем результаты в анонимный тип для отображения
                var result = filtered.AsEnumerable()
                    .Select(t => new
                    {
                        t.Id,
                        PropertyTitle = t.Property?.Title ?? "Нет данных",
                        Address = FormatAddress(t.Property?.Address),
                        ServiceTitle = t.Service?.Title ?? "Нет данных",
                        OwnerFullName = t.Owner?.FullName ?? "Нет данных",
                        ClientFullName = t.Client?.FullName ?? "Нет данных",
                        RieltorId = t.Service?.Staff?.Users?.Id,
                        RieltorFullName = t.Service?.Staff?.Users?.FullName ?? "Не назначен",
                        t.TransactionDate,
                        t.Amount,
                        StatusTitle = t.Status?.Title ?? "Нет данных"
                    })
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                DataGrid.ItemsSource = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для форматирования адреса из JSON
        private string FormatAddress(string jsonAddress)
        {
            if (string.IsNullOrEmpty(jsonAddress))
                return "Адрес не указан";

            try
            {
                var address = JsonSerializer.Deserialize<AddressInfo>(jsonAddress);
                return $"{address.city}, {address.street} {address.house_number}, кв. {address.apartment_number}";
            }
            catch
            {
                return jsonAddress; // Возвращаем исходный JSON, если не удалось десериализовать
            }
        }

        private void LoadFilterData()
        {
            try
            {
                using var context = DatabaseContext.GetContext();

                // Загрузка данных для фильтров
                var services = context.Service.Select(s => new { s.Id, s.Title }).ToList();
                ServiceComboBox.ItemsSource = services;
                ServiceComboBox.DisplayMemberPath = "Title";
                ServiceComboBox.SelectedValuePath = "Id";

                var properties = context.HouseData.Select(p => new { p.Id, p.Title }).ToList();
                ObjectComboBox.ItemsSource = properties;
                ObjectComboBox.DisplayMemberPath = "Title";
                ObjectComboBox.SelectedValuePath = "Id";

                var rieltors = context.Staff
                    .Include(s => s.Users)
                    .Select(s => new { s.Users.Id, s.Users.FullName })
                    .ToList();
                ExecutorComboBox.ItemsSource = rieltors;
                ExecutorComboBox.DisplayMemberPath = "FullName";
                ExecutorComboBox.SelectedValuePath = "Id";

                var clients = context.Users.Select(c => new { c.Id, c.FullName }).ToList();
                CustomerComboBox.ItemsSource = clients;
                CustomerComboBox.DisplayMemberPath = "FullName";
                CustomerComboBox.SelectedValuePath = "Id";

                var statuses = context.Status.Select(s => new { s.Id, s.Title }).ToList();
                StatusComboBox.ItemsSource = statuses;
                StatusComboBox.DisplayMemberPath = "Title";
                StatusComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGrid.SelectedItem == null)
            {
                _selectedTransaction = null;
                return;
            }

            try
            {
                var selectedItem = DataGrid.SelectedItem;
                var idProperty = selectedItem.GetType().GetProperty("Id");
                int transactionId = (int)idProperty.GetValue(selectedItem);

                using var context = DatabaseContext.GetContext();

                _selectedTransaction = context.Transactions
                    .Include(t => t.Property)
                    .Include(t => t.Service)
                        .ThenInclude(s => s.Staff)
                            .ThenInclude(st => st.Users)
                    .Include(t => t.Owner)
                    .Include(t => t.Client)
                    .Include(t => t.Status)
                    .FirstOrDefault(t => t.Id == transactionId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе контракта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _selectedTransaction = null;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем новое окно для добавления контракта
                var addWindow = new AddEditContractWindow();
                if (addWindow.ShowDialog() == true)
                {
                    // После успешного добавления обновляем список
                    LoadTransactions();
                    MessageBox.Show("Контракт успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении контракта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransaction == null)
            {
                MessageBox.Show("Пожалуйста, выберите контракт для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Создаем копию объекта для редактирования
                var transactionToEdit = new Transactions
                {
                    Id = _selectedTransaction.Id,
                    PropertyId = _selectedTransaction.PropertyId,
                    ServiceId = _selectedTransaction.ServiceId,
                    OwnerId = _selectedTransaction.OwnerId,
                    ClientId = _selectedTransaction.ClientId,
                    Amount = _selectedTransaction.Amount,
                    TransactionDate = _selectedTransaction.TransactionDate,
                    StatusId = _selectedTransaction.StatusId
                };

                var editWindow = new AddEditContractWindow(transactionToEdit);
                if (editWindow.ShowDialog() == true)
                {
                    // Обновляем данные в базе
                    using var context = DatabaseContext.GetContext();
                    context.Entry(_selectedTransaction).CurrentValues.SetValues(transactionToEdit);
                    context.SaveChanges();

                    LoadTransactions();
                    MessageBox.Show("Контракт успешно изменен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании контракта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransaction == null)
            {
                MessageBox.Show("Пожалуйста, выберите контракт для удаления", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show("Вы уверены, что хотите удалить выбранный контракт?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = DatabaseContext.GetContext())
                    {
                        // Прикрепляем объект к контексту, если он не отслеживается
                        var transaction = context.Transactions.Find(_selectedTransaction.Id);
                        if (transaction != null)
                        {
                            context.Transactions.Remove(transaction);
                            context.SaveChanges();
                        }
                    }
                    _selectedTransaction = null;
                    LoadTransactions();
                    MessageBox.Show("Контракт успешно удален!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении контракта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTransaction == null)
            {
                MessageBox.Show("Пожалуйста, выберите контракт для печати", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Создаем диалог сохранения файла
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    FileName = $"Договор_{_selectedTransaction.Id}_{DateTime.Now:yyyyMMdd}.docx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Генерируем документ
                    GenerateContractDocument(_selectedTransaction, saveFileDialog.FileName);

                    // Открываем документ
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = saveFileDialog.FileName,
                        UseShellExecute = true
                    });

                    MessageBox.Show("Документ успешно создан!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании документа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

       

        private void GenerateContractDocument(Transactions contract, string filePath)
        {
            try
            {
                string rieltorName = contract.Service?.Staff?.Users?.FullName ?? "Не назначен";

                if (DataGrid.SelectedItem != null)
                {
                    var selectedItem = DataGrid.SelectedItem;
                    var rieltorNameProperty = selectedItem.GetType().GetProperty("RieltorFullName");
                    rieltorName = rieltorNameProperty?.GetValue(selectedItem)?.ToString() ?? rieltorName;
                }

                using (WordprocessingDocument wordDocument =
                    WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    Body body = mainPart.Document.AppendChild(new Body());

                    // Настройки страницы
                    SectionProperties sectionProps = new SectionProperties();
                    PageSize pageSize = new PageSize() { Width = 11906U, Height = 16838U }; // A4
                    PageMargin pageMargin = new PageMargin()
                    {
                        Top = 1701,
                        Right = 850,
                        Bottom = 1701,
                        Left = 1701,
                        Header = 851,
                        Footer = 992,
                        Gutter = 0
                    };
                    sectionProps.Append(pageSize, pageMargin);
                    body.Append(sectionProps);

                    // Шапка документа
                    Paragraph header = new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Center }),
                        new Run(
                            new RunProperties(
                                new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                new FontSize() { Val = "28" },
                                new Bold()),
                            new Text("ДОГОВОР №" + contract.Id)));
                    body.Append(header);

                    // Дата договора
                    Paragraph dateParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Right }),
                        new Run(
                            new RunProperties(
                                new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                new FontSize() { Val = "22" }),
                            new Text("г. " + contract.TransactionDate.ToString("dd.MM.yyyy"))));
                    body.Append(dateParagraph);
                    body.Append(new Paragraph(new Run(new Break()))); // Пустая строка

                    // Основной текст договора
                    string mainText = $@"
Настоящий договор заключен между:
Собственником: {contract.Owner?.FullName ?? "Не указано"}, именуемый(ая) в дальнейшем 'Собственник',
и 
Клиентом: {contract.Client?.FullName ?? "Не указано"}, именуемый(ая) в дальнейшем 'Клиент',
с одной стороны, и 
Агентством недвижимости 'Название агентства', в лице риелтора {rieltorName}, 
действующего(ей) на основании доверенности, с другой стороны,
о нижеследующем:

1. ПРЕДМЕТ ДОГОВОРА
1.1. Агентство обязуется оказать услугу: {contract.Service?.Title ?? "Не указано"}.
1.2. Объект недвижимости: {contract.Property?.Title ?? "Не указано"}.
1.3. Адрес объекта: {FormatAddress(contract.Property?.Address)}.

2. СТОИМОСТЬ УСЛУГ И ПОРЯДОК РАСЧЕТОВ
2.1. Общая стоимость услуг составляет: {contract.Amount.ToString("N2")} рублей.
2.2. Оплата производится в следующем порядке: [указать условия оплаты].

3. ОБЯЗАННОСТИ СТОРОН
3.1. Агентство обязуется:
- Оказать услуги в соответствии с условиями настоящего договора;
- Предоставить полную и достоверную информацию об объекте недвижимости.

3.2. Клиент обязуется:
- Своевременно оплачивать услуги;
- Предоставлять необходимые документы.

4. ОТВЕТСТВЕННОСТЬ СТОРОН
4.1. За неисполнение или ненадлежащее исполнение обязательств стороны несут ответственность в соответствии с законодательством РФ.

5. СРОК ДЕЙСТВИЯ ДОГОВОРА
5.1. Настоящий договор вступает в силу с момента подписания и действует до полного исполнения обязательств сторонами.

6. РЕКВИЗИТЫ И ПОДПИСИ СТОРОН

Собственник: ___________________ / {contract.Owner?.FullName ?? "Не указано"} /
Паспорт: ____________________ Выдан: ____________________

Клиент: ___________________ / {contract.Client?.FullName ?? "Не указано"} /
Паспорт: ____________________ Выдан: ____________________

Агентство недвижимости: ___________________ / {rieltorName} /
";

                    // Добавление основного текста
                    foreach (var line in mainText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        Paragraph paragraph = new Paragraph(
                            new ParagraphProperties(
                                new SpacingBetweenLines() { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }),
                            new Run(
                                new RunProperties(
                                    new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                    new FontSize() { Val = "22" }),
                                new Text(line)));
                        body.Append(paragraph);
                    }

                    // Печать агентства
                    Paragraph stampParagraph = new Paragraph(
                        new ParagraphProperties(
                            new Justification() { Val = JustificationValues.Right }),
                        new Run(
                            new RunProperties(
                                new RunFonts() { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                                new FontSize() { Val = "22" },
                                new Bold()),
                            new Text("М.П.")));
                    body.Append(stampParagraph);

                    mainPart.Document.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при создании документа: " + ex.Message);
            }
        }
    }
}
